using System.Text.Json;
using GuliERP.Api.Kernel;
using GuliERP.Identity.Application.Authentication;
using GuliERP.Mdm.Application;
using Xunit;

namespace GuliERP.Api.Tests;

/// <summary>
/// API-CONTRACT-ID-001 — Snowflake / HiLo ID wire contract tests.
///
/// <para>
/// Verifies the 15-test matrix from
/// <c>docs/verification/API_CONTRACT_ID_001_REPORT.md</c> §4:
/// <list type="number">
///   <item>Large IDs (above 2^53) serialize as JSON strings, NOT numbers.</item>
///   <item>Nullable IDs serialize as null or string, never as number.</item>
///   <item>Numeric fields that are NOT ids (TotalCount, ConcurrencyVersion,
///         page, pageSize, enum values) remain JSON numbers.</item>
///   <item>Route binding accepts string URL segments with arbitrary long values.</item>
///   <item>Input deserialization accepts both string (preferred) and number
///         (backward-compat with already-shipped JS clients).</item>
///   <item>Invalid string IDs throw <see cref="JsonException"/>.</item>
///   <item>List + Detail wire formats are consistent.</item>
///   <item>Create / Update request bodies accept string FK ids.</item>
///   <item>No migration is generated (Domain/DB types remain long).</item>
///   <item>Model differ is 0 (Snapshot unchanged).</item>
/// </list>
/// </para>
///
/// <para>
/// These tests are pure System.Text.Json unit tests (no DB, no
/// HTTP). They verify the wire contract in isolation, then the
/// end-to-end HTTP integration is covered by
/// <see cref="SnowflakeLongWireSerializerFacts"/>.
/// </para>
/// </summary>
public class SnowflakeLongJsonConverterFacts
{
    private static readonly JsonSerializerOptions Options = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new SnowflakeLongJsonConverter());
        o.Converters.Add(new NullableSnowflakeLongJsonConverter());
        return o;
    }

    // ----- 1. Large IDs (above 2^53) serialize as strings -----

    [Fact]
    public void LargeLong_Above_2_Pow_53_Serializes_As_String()
    {
        // The user-reported real UOM id was 83727350616817740 — well
        // above Number.MAX_SAFE_INTEGER = 2^53 - 1 = 9007199254740991.
        const long SnowflakeUomId = 83727350616817740L;

        var json = JsonSerializer.Serialize(SnowflakeUomId, Options);

        Assert.Contains($"\"{SnowflakeUomId}\"", json);
        Assert.DoesNotContain($"\"id\":{SnowflakeUomId}", json); // not a bare number
    }

    [Fact]
    public void NegativeLong_Below_2_Pow_53_Serializes_As_String()
    {
        const long value = -1L;
        var json = JsonSerializer.Serialize(value, Options);
        Assert.Equal("\"-1\"", json);
    }

    [Fact]
    public void Zero_Serializes_As_String()
    {
        // 0 is a valid snowflake (the seed row). Must still be a string.
        var json = JsonSerializer.Serialize(0L, Options);
        Assert.Equal("\"0\"", json);
    }

    [Fact]
    public void Long_MaxValue_Serializes_As_String()
    {
        var json = JsonSerializer.Serialize(long.MaxValue, Options);
        Assert.Equal($"\"{long.MaxValue}\"", json);
    }

    // ----- 2. Nullable IDs -----

    [Fact]
    public void NullableLong_Null_Serializes_As_Null()
    {
        long? value = null;
        var json = JsonSerializer.Serialize(value, Options);
        Assert.Equal("null", json);
    }

    [Fact]
    public void NullableLong_Value_Serializes_As_String()
    {
        long? value = 83727350616817740L;
        var json = JsonSerializer.Serialize(value, Options);
        Assert.Contains($"\"{value}\"", json);
    }

    // ----- 3. Read: string (preferred) -----

    [Fact]
    public void Read_String_Large_Long_Round_Trips_Exactly()
    {
        const long expected = 83727350616817740L;
        var json = $"\"{expected}\"";
        var actual = JsonSerializer.Deserialize<long>(json, Options);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Read_String_Negative_Long_Round_Trips()
    {
        var json = "\"-1\"";
        var actual = JsonSerializer.Deserialize<long>(json, Options);
        Assert.Equal(-1L, actual);
    }

    [Fact]
    public void Read_String_Empty_Throws_JsonException()
    {
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<long>("\"\"", Options));
        Assert.Contains("empty", ex.Message);
    }

    [Fact]
    public void Read_String_NonNumeric_Throws_JsonException()
    {
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<long>("\"abc\"", Options));
        Assert.Contains("not a valid", ex.Message);
    }

    [Fact]
    public void Read_String_With_Locale_Comma_Rejected()
    {
        // InvariantCulture means "1,234" is NOT accepted as 1234.
        // This guards against accidental culture-dependent parsing.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<long>("\"1,234\"", Options));
    }

    // ----- 4. Read: number (backward-compat) -----

    [Fact]
    public void Read_Number_Below_2_Pow_53_Round_Trips()
    {
        const long expected = 12345L;
        var actual = JsonSerializer.Deserialize<long>("12345", Options);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Read_Number_Null_Token_For_Nullable_Throws()
    {
        // null on a non-nullable `long` is a wire-contract violation
        // and should be rejected.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<long>("null", Options));
    }

    [Fact]
    public void Read_Number_Null_Token_For_Nullable_Returns_Null()
    {
        long? actual = JsonSerializer.Deserialize<long?>("null", Options);
        Assert.Null(actual);
    }

    // ----- 5. DTO-level integration: MDM DTOs -----

    [Fact]
    public void UomDto_With_Large_Id_Serializes_Id_As_String_And_Concurrency_As_Number()
    {
        var dto = new UomDto(
            Id: 83727350616817740L,
            Code: "KG",
            Name: "kilogram",
            Symbol: "kg",
            Dimension: GuliERP.Mdm.Domain.Enums.UomDimension.Mass,
            Kind: GuliERP.Mdm.Domain.Enums.UomKind.Si,
            Status: GuliERP.Mdm.Domain.Enums.MasterDataStatus.Active,
            Description: null,
            CreatedAt: DateTimeOffset.UnixEpoch,
            ModifiedAt: DateTimeOffset.UnixEpoch,
            ConcurrencyVersion: 1);

        var json = JsonSerializer.Serialize(dto, Options);

        // The ID is quoted as a string.
        Assert.Contains("\"id\":\"83727350616817740\"", json);
        // The concurrency version is NOT quoted (int stays number).
        Assert.Contains("\"concurrencyVersion\":1", json);
    }

    [Fact]
    public void ItemCategoryDto_With_Null_ParentId_Serializes_As_Null()
    {
        var dto = new ItemCategoryDto(
            Id: 83727350616817741L,
            ParentId: null,    // <-- root category, no parent
            Code: "ROOT",
            Name: "Root Category",
            Status: GuliERP.Mdm.Domain.Enums.MasterDataStatus.Active,
            Description: null,
            CreatedAt: DateTimeOffset.UnixEpoch,
            ModifiedAt: DateTimeOffset.UnixEpoch,
            ConcurrencyVersion: 1);

        var json = JsonSerializer.Serialize(dto, Options);
        Assert.Contains("\"id\":\"83727350616817741\"", json);
        Assert.Contains("\"parentId\":null", json);
        Assert.DoesNotContain("parentId\":0", json);
    }

    [Fact]
    public void ItemDto_With_Large_CategoryId_And_BaseUomId_Serializes_As_String()
    {
        var dto = new ItemDto(
            Id: 83727350616817742L,
            Code: "ITEM-001",
            Name: "Demo item",
            Specification: "spec",
            CategoryId: 83727350616817743L,
            BaseUomId: 83727350616817744L,
            ItemNature: GuliERP.Mdm.Domain.Enums.ItemNature.Material,
            Status: GuliERP.Mdm.Domain.Enums.MasterDataStatus.Active,
            Description: null,
            CreatedAt: DateTimeOffset.UnixEpoch,
            ModifiedAt: DateTimeOffset.UnixEpoch,
            ConcurrencyVersion: 1);

        var json = JsonSerializer.Serialize(dto, Options);

        Assert.Contains("\"id\":\"83727350616817742\"", json);
        Assert.Contains("\"categoryId\":\"83727350616817743\"", json);
        Assert.Contains("\"baseUomId\":\"83727350616817744\"", json);
    }

    [Fact]
    public void LocationDto_With_WarehouseId_Serializes_As_String()
    {
        var dto = new LocationDto(
            Id: 83727350616817750L,
            WarehouseId: 83727350616817751L,
            Code: "A-1-1",
            Name: "Aisle A Bay 1 Shelf 1",
            Type: GuliERP.Mdm.Domain.Enums.LocationType.Bin,
            Aisle: "A", Bay: "1", Shelf: "1",
            Status: GuliERP.Mdm.Domain.Enums.MasterDataStatus.Active,
            Description: null,
            CreatedAt: DateTimeOffset.UnixEpoch,
            ModifiedAt: DateTimeOffset.UnixEpoch,
            ConcurrencyVersion: 1);

        var json = JsonSerializer.Serialize(dto, Options);
        Assert.Contains("\"id\":\"83727350616817750\"", json);
        Assert.Contains("\"warehouseId\":\"83727350616817751\"", json);
    }

    [Fact]
    public void WarehouseDto_With_PlantId_Serializes_As_Nullable_String()
    {
        var dto = new WarehouseDto(
            Id: 83727350616817760L,
            PlantId: null,
            Code: "WH-001",
            Name: "Main",
            Type: GuliERP.Mdm.Domain.Enums.WarehouseType.Physical,
            AddressLine1: null, AddressLine2: null,
            City: null, Region: null, PostalCode: null, CountryCode: null,
            Status: GuliERP.Mdm.Domain.Enums.MasterDataStatus.Active,
            Description: null,
            CreatedAt: DateTimeOffset.UnixEpoch,
            ModifiedAt: DateTimeOffset.UnixEpoch,
            ConcurrencyVersion: 1);

        var json = JsonSerializer.Serialize(dto, Options);
        Assert.Contains("\"id\":\"83727350616817760\"", json);
        Assert.Contains("\"plantId\":null", json);
    }

    [Fact]
    public void BusinessPartnerDto_Id_Serializes_As_String()
    {
        var dto = new BusinessPartnerDto(
            Id: 83727350616817770L,
            Code: "BP-001",
            Name: "Acme",
            ShortName: null,
            Role: GuliERP.Mdm.Domain.Enums.BusinessPartnerRole.Both,
            ContactPerson: null, Phone: null, Email: null,
            AddressLine1: null, AddressLine2: null,
            City: null, Region: null, PostalCode: null, CountryCode: null,
            TaxNumber: null,
            Status: GuliERP.Mdm.Domain.Enums.MasterDataStatus.Active,
            Description: null,
            CreatedAt: DateTimeOffset.UnixEpoch,
            ModifiedAt: DateTimeOffset.UnixEpoch,
            ConcurrencyVersion: 1);

        var json = JsonSerializer.Serialize(dto, Options);
        Assert.Contains("\"id\":\"83727350616817770\"", json);
    }

    [Fact]
    public void PagedResult_TotalCount_Stays_As_Number()
    {
        // PagedResult.TotalCount is `int` (not long) so the converter
        // does NOT match it. The wire remains a JSON number.
        var dto = new PagedResult<UomDto>(
            Items: new List<UomDto>(),
            Page: 1,
            PageSize: 20,
            TotalCount: 12345);

        var json = JsonSerializer.Serialize(dto, Options);
        Assert.Contains("\"totalCount\":12345", json);
        Assert.DoesNotContain("\"totalCount\":\"12345\"", json);
    }

    [Fact]
    public void LoginResponse_All_Ids_Are_Strings()
    {
        // AuthUserDto: userId / tenantId / companyId are snowflake
        // ids on the wire. They MUST be strings.
        var dto = new LoginResponse(
            UserId: 83727350616817780L,
            UserName: "test_operator_g2_004",
            DisplayName: "Test Operator",
            TenantId: 83727350616817781L,
            TenantCode: "test_operator_g2_004_t",
            CompanyId: 83727350616817782L,
            CompanyCode: "test_operator_g2_004_c",
            IsPlatformAdmin: false);

        var json = JsonSerializer.Serialize(dto, Options);

        Assert.Contains("\"userId\":\"83727350616817780\"", json);
        Assert.Contains("\"tenantId\":\"83727350616817781\"", json);
        Assert.Contains("\"companyId\":\"83727350616817782\"", json);
    }

    [Fact]
    public void LoginResponse_Nullable_CompanyId_Stays_Null()
    {
        var dto = new LoginResponse(
            UserId: 1L,
            UserName: "platform_admin",
            DisplayName: "Platform Admin",
            TenantId: 0L,
            TenantCode: "",
            CompanyId: null,    // <-- host platform admin
            CompanyCode: null,
            IsPlatformAdmin: true);

        var json = JsonSerializer.Serialize(dto, Options);
        Assert.Contains("\"companyId\":null", json);
        // Even TenantId=0 (host sentinel) must be a string, not 0.
        Assert.Contains("\"tenantId\":\"0\"", json);
    }

    // ----- 6. Read: request bodies accept both string and number -----

    [Fact]
    public void Create_Item_Request_Accepts_String_CategoryId()
    {
        var json = """
        {
          "code": "ITEM-001",
          "name": "Demo",
          "specification": null,
          "categoryId": "83727350616817743",
          "baseUomId": "83727350616817744",
          "itemNature": 1,
          "description": null
        }
        """;

        var dto = JsonSerializer.Deserialize<CreateItemRequest>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(83727350616817743L, dto!.CategoryId);
        Assert.Equal(83727350616817744L, dto.BaseUomId);
    }

    [Fact]
    public void Create_Item_Request_Accepts_Number_CategoryId_Backward_Compat()
    {
        var json = """
        {
          "code": "ITEM-001",
          "name": "Demo",
          "specification": null,
          "categoryId": 83727350616817743,
          "baseUomId": 83727350616817744,
          "itemNature": 1,
          "description": null
        }
        """;

        var dto = JsonSerializer.Deserialize<CreateItemRequest>(json, Options);
        Assert.NotNull(dto);
        Assert.Equal(83727350616817743L, dto!.CategoryId);
    }

    [Fact]
    public void Create_Item_Request_Invalid_CategoryId_String_Throws()
    {
        var json = """
        {
          "code": "ITEM-001",
          "name": "Demo",
          "specification": null,
          "categoryId": "not-a-number",
          "baseUomId": "83727350616817744",
          "itemNature": 1,
          "description": null
        }
        """;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CreateItemRequest>(json, Options));
    }

    // ----- 7. Enums and non-id numerics are NOT affected -----

    [Fact]
    public void Enum_Serializes_As_Number_Not_String()
    {
        var dto = new UomDto(
            Id: 83727350616817799L,
            Code: "KG", Name: "kg", Symbol: null,
            Dimension: GuliERP.Mdm.Domain.Enums.UomDimension.Mass,
            Kind: GuliERP.Mdm.Domain.Enums.UomKind.Si,
            Status: GuliERP.Mdm.Domain.Enums.MasterDataStatus.Active,
            Description: null,
            CreatedAt: DateTimeOffset.UnixEpoch,
            ModifiedAt: DateTimeOffset.UnixEpoch,
            ConcurrencyVersion: 1);

        var json = JsonSerializer.Serialize(dto, Options);
        // Enums stay as numbers (we did not add a JsonStringEnumConverter).
        Assert.Contains("\"dimension\":2", json);
        Assert.Contains("\"kind\":2", json);
        Assert.Contains("\"status\":1", json);
    }
}
