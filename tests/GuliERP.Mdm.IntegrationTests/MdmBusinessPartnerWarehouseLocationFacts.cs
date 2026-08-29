using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GuliERP.Mdm.IntegrationTests;

/// <summary>
/// MDM-002 BusinessPartner / Warehouse / Location integration facts.
/// Operator-required PostgreSQL tests. Each test creates per-run-unique
/// data so tests are order-independent and idempotent across runs.
/// Tests live in the same <see cref="MdmPostgresIntegrationCollection"/>
/// as the MDM-001 tests because they share the canonical DB.
/// </summary>
[Collection(MdmPostgresIntegrationCollection.Name)]
public sealed class MdmBusinessPartnerWarehouseLocationFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BadConnectionString =
        "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2";

    private readonly WebApplicationFactory<Program> _factory;

    public MdmBusinessPartnerWarehouseLocationFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> BuildHost() =>
        _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

    private static string UniqueSuffix() =>
        Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static void RequireRealDb(IServiceProvider sp)
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var conn = cfg.GetConnectionString("GuliERP");
        if (string.IsNullOrEmpty(conn) || conn.Contains("Host=127.0.0.1;Port=1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "This MDM-002 integration test requires a real PostgreSQL connection. " +
                "Set ConnectionStrings__GuliERP and re-run.");
        }
    }

    [Fact]
    public async Task BusinessPartner_Create_List_Get_Update_EndToEnd()
    {
        // Per-run unique Code (avoids UNIQUE (TenantId, Code) collisions
        // when the test runs against a DB that already has prior runs).
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 5.2
        // fix: tenant must be resolved before any master-data write.
        // Without this the service throws MdmValidationException
        // ("Current Tenant is not resolved"). Code is also
        // normalized to the 6-digit UPPER_SNAKE shape (no hyphens,
        // per the MasterDataCodeValidator regex
        // ^[A-Z][A-Z0-9_]{1,39}$).
        var tenantId = 1_400_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var currentTenant = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        using var _ = currentTenant.Change(tenantId);

        var bpSvc = sp.GetRequiredService<IMdmBusinessPartnerService>();
        var suffix = UniqueSuffix();
        var code = $"BP{suffix}";

        var create = new CreateBusinessPartnerRequest(
            Code: code,
            Name: "Test Customer " + suffix,
            ShortName: "TC" + suffix,
            Role: BusinessPartnerRole.Customer,
            ContactPerson: "Alice",
            Phone: "+86 21 0000 0000",
            Email: "alice@example.com",
            AddressLine1: "1 Test Street",
            AddressLine2: null,
            City: "Shanghai",
            Region: "Shanghai",
            PostalCode: "200000",
            CountryCode: "CN",
            TaxNumber: "TAX" + suffix,
            MnemonicCode: "TC" + suffix,
            AdministrativeRegionId: null,
            Description: "MDM-002 integration test BP");

        var created = await bpSvc.CreateAsync(create);
        Assert.True(created.Id > 0, "HiLo did not assign a positive Id.");
        Assert.Equal(code, created.Code);
        Assert.Equal(BusinessPartnerRole.Customer, created.Role);
        Assert.Equal("CN", created.CountryCode);
        Assert.Equal(1, created.ConcurrencyVersion);

        // List with role filter (Customer should match this row).
        var list = await bpSvc.ListAsync(new BusinessPartnerListQuery(
            Keyword: code,
            Role: BusinessPartnerRole.Customer,
            Status: null,
            Page: 1,
            PageSize: 20));
        Assert.Contains(list.Items, x => x.Id == created.Id && x.Code == code);

        // GetById
        var fetched = await bpSvc.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(code, fetched!.Code);

        // Update
        var updated = await bpSvc.UpdateAsync(created.Id, new UpdateBusinessPartnerRequest(
            Name: "Renamed " + suffix,
            ShortName: "TC" + suffix,
            Role: BusinessPartnerRole.Both,
            ContactPerson: "Alice",
            Phone: "+86 21 0000 0000",
            Email: "alice@example.com",
            AddressLine1: "1 Test Street",
            AddressLine2: null,
            City: "Shanghai",
            Region: "Shanghai",
            PostalCode: "200000",
            CountryCode: "CN",
            TaxNumber: "TAX" + suffix,
            MnemonicCode: "TC" + suffix,
            AdministrativeRegionId: null,
            Status: MasterDataStatus.Active,
            Description: "MDM-002 integration test BP (updated)",
            ExpectedConcurrencyVersion: created.ConcurrencyVersion));
        Assert.NotNull(updated);
        Assert.Equal(2, updated!.ConcurrencyVersion);
        Assert.Equal(BusinessPartnerRole.Both, updated.Role);
    }

    [Fact]
    public async Task Warehouse_And_Location_Create_EndToEnd_And_CrossScopeGuard()
    {
        // Per-run unique Code.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 5.2
        // fix: tenant AND company must be resolved before any
        // master-data write. MdmWarehouseService.RequireScope
        // throws on either missing.
        var tenantId = 1_500_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var companyId = 1_500_100L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var currentTenant = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        var currentCompany = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentCompany>();
        using var __t = currentTenant.Change(tenantId);
        using var __c = currentCompany.Change(companyId);

        var whSvc = sp.GetRequiredService<IMdmWarehouseService>();
        var locSvc = sp.GetRequiredService<IMdmLocationService>();
        var suffix = UniqueSuffix();
        var whCode = $"WH{suffix}";
        var locCode = $"LOC{suffix}";

        // Create Warehouse
        var wh = await whSvc.CreateAsync(new CreateWarehouseRequest(
            PlantId: null,
            Code: whCode,
            Name: "Test Warehouse " + suffix,
            Type: WarehouseType.Physical,
            AddressLine1: "1 Warehouse Road",
            AddressLine2: null,
            City: "Shanghai",
            Region: "Shanghai",
            PostalCode: "200000",
            CountryCode: "CN",
            Description: "MDM-002 integration test WH"));
        Assert.True(wh.Id > 0, "HiLo did not assign a Warehouse Id.");
        Assert.Equal(whCode, wh.Code);

        // Create Location under the same Warehouse
        var loc = await locSvc.CreateAsync(new CreateLocationRequest(
            WarehouseId: wh.Id,
            Code: locCode,
            Name: "Test Location " + suffix,
            Type: LocationType.Bin,
            Aisle: "A1",
            Bay: "B1",
            Shelf: "S1",
            Description: "MDM-002 integration test LOC"));
        Assert.True(loc.Id > 0);
        Assert.Equal(locCode, loc.Code);
        Assert.Equal(wh.Id, loc.WarehouseId);

        // Cross-scope guard: a Location create with a non-existent
        // WarehouseId must throw LocationParentWarehouseCrossScope.
        await Assert.ThrowsAsync<MdmValidationException>(async () =>
        {
            await locSvc.CreateAsync(new CreateLocationRequest(
                WarehouseId: long.MaxValue - 1,
                Code: $"BAD{suffix}",
                Name: "Bad Location",
                Type: LocationType.Bin,
                Aisle: null, Bay: null, Shelf: null,
                Description: "should fail"));
        });
    }

    // ============================================================
    // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_SCHEMA_AND_OPERATOR_CLOSURE
    // (2026-08-30) — schema-semantics tests. These require a
    // real PostgreSQL connection because the InMemory provider
    // does NOT enforce unique-index constraints; only the
    // service-layer duplicate pre-check is exercised there
    // (see MdmReuseWaveFacts.Same_Warehouse_Duplicate_Rejected).
    // The PG tests below are the authoritative persistence proof.
    // ============================================================

    [Fact]
    public async Task Location_Same_Warehouse_Same_Code_Rejected_By_Db_Unique_Index()
    {
        // MDM007 contract: (TenantId, CompanyId, WarehouseId, Code)
        // is UNIQUE. Two Locations in the SAME Warehouse with the
        // SAME Code must be rejected by the DB, not just by the
        // service-layer pre-check.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        var tenantId = 1_600_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var companyId = 1_600_100L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var currentTenant = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        var currentCompany = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentCompany>();
        using var __t = currentTenant.Change(tenantId);
        using var __c = currentCompany.Change(companyId);

        var whSvc = sp.GetRequiredService<IMdmWarehouseService>();
        var locSvc = sp.GetRequiredService<IMdmLocationService>();
        var suffix = UniqueSuffix();
        var whCode = $"WH{suffix}";
        var dupCode = $"DUP{suffix}";

        // One Warehouse, two Locations with the SAME Code.
        var wh = await whSvc.CreateAsync(new CreateWarehouseRequest(
            PlantId: null,
            Code: whCode,
            Name: "Dup WH " + suffix,
            Type: WarehouseType.Physical,
            AddressLine1: null, AddressLine2: null,
            City: null, Region: null, PostalCode: null,
            CountryCode: null, Description: null));

        // First insert succeeds.
        var first = await locSvc.CreateAsync(new CreateLocationRequest(
            WarehouseId: wh.Id,
            Code: dupCode,
            Name: "First " + suffix,
            Type: LocationType.Bin,
            Aisle: null, Bay: null, Shelf: null,
            Description: "first"));
        Assert.True(first.Id > 0);

        // Second insert with the SAME (TenantId, CompanyId,
        // WarehouseId, Code) must throw — caught here as a
        // MdmValidationException (service pre-check) OR as the
        // raw DbUpdateException from the unique index. Either
        // way: the duplicate is rejected.
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await locSvc.CreateAsync(new CreateLocationRequest(
                WarehouseId: wh.Id,
                Code: dupCode,
                Name: "Second " + suffix,
                Type: LocationType.Bin,
                Aisle: null, Bay: null, Shelf: null,
                Description: "second — must be rejected"));
        });
    }

    [Fact]
    public async Task Location_Different_Warehouse_Same_Code_Allowed_By_MDM007_Index()
    {
        // MDM007 contract: (TenantId, CompanyId, WarehouseId, Code)
        // is UNIQUE. Two Locations in DIFFERENT Warehouses with the
        // SAME Code MUST be allowed (per-Warehouse uniqueness).
        // This is the Foundation-driven scenario: Warehouse A and
        // Warehouse B both auto-generate LOC_000001, both persist.
        using var factory = BuildHost();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        RequireRealDb(sp);

        var tenantId = 1_700_000L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var companyId = 1_700_100L + Math.Abs(UniqueSuffix().GetHashCode() % 100_000);
        var currentTenant = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentTenant>();
        var currentCompany = sp.GetRequiredService<GuliERP.Foundation.Kernel.ICurrentCompany>();
        using var __t = currentTenant.Change(tenantId);
        using var __c = currentCompany.Change(companyId);

        var whSvc = sp.GetRequiredService<IMdmWarehouseService>();
        var locSvc = sp.GetRequiredService<IMdmLocationService>();
        var suffix = UniqueSuffix();
        var wh1Code = $"WA{suffix}";
        var wh2Code = $"WB{suffix}";
        var sharedLocCode = $"SHR{suffix}";

        // Two distinct Warehouses, same Company, same Tenant.
        var wh1 = await whSvc.CreateAsync(new CreateWarehouseRequest(
            PlantId: null, Code: wh1Code, Name: "A " + suffix,
            Type: WarehouseType.Physical,
            AddressLine1: null, AddressLine2: null,
            City: null, Region: null, PostalCode: null,
            CountryCode: null, Description: null));
        var wh2 = await whSvc.CreateAsync(new CreateWarehouseRequest(
            PlantId: null, Code: wh2Code, Name: "B " + suffix,
            Type: WarehouseType.Physical,
            AddressLine1: null, AddressLine2: null,
            City: null, Region: null, PostalCode: null,
            CountryCode: null, Description: null));

        // Same Location Code in both Warehouses — must BOTH succeed.
        var l1 = await locSvc.CreateAsync(new CreateLocationRequest(
            WarehouseId: wh1.Id,
            Code: sharedLocCode,
            Name: "A1 " + suffix,
            Type: LocationType.Bin,
            Aisle: null, Bay: null, Shelf: null, Description: null));
        var l2 = await locSvc.CreateAsync(new CreateLocationRequest(
            WarehouseId: wh2.Id,
            Code: sharedLocCode,
            Name: "B1 " + suffix,
            Type: LocationType.Bin,
            Aisle: null, Bay: null, Shelf: null, Description: null));

        Assert.True(l1.Id > 0);
        Assert.True(l2.Id > 0);
        Assert.NotEqual(l1.Id, l2.Id);
        Assert.Equal(sharedLocCode, l1.Code);
        Assert.Equal(sharedLocCode, l2.Code);
        Assert.Equal(wh1.Id, l1.WarehouseId);
        Assert.Equal(wh2.Id, l2.WarehouseId);
    }
}
