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
}
