using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Mdm;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Mdm.Tests;

public sealed class MasterDataCodeServiceFacts
{
    [Fact]
    public async Task AutoEditable_Empty_Generates_Next_Code()
    {
        await using var fixture = MasterDataCodeFixture.Create();
        await fixture.SeedRuleAsync("BusinessPartner", tenantId: 1, prefix: "BP", sequenceLength: 6);

        var code = await fixture.Service.GenerateNextAsync(new MasterDataCodeRequest(
            EntityType: "BusinessPartner",
            TenantId: 1,
            CompanyId: null,
            WarehouseId: null,
            ExplicitCode: null));

        Assert.Equal("BP_000001", code.Code);
        Assert.False(code.WasExplicit);
    }

    [Fact]
    public async Task Explicit_Code_Is_Canonicalized_And_Does_Not_Consume_Sequence()
    {
        await using var fixture = MasterDataCodeFixture.Create();
        var rule = await fixture.SeedRuleAsync("BusinessPartner", tenantId: 1, prefix: "BP", sequenceLength: 6);

        var explicitCode = await fixture.Service.GenerateNextAsync(new MasterDataCodeRequest(
            EntityType: "BusinessPartner",
            TenantId: 1,
            CompanyId: null,
            WarehouseId: null,
            ExplicitCode: " bp_test_001 "));

        Assert.Equal("BP_TEST_001", explicitCode.Code);
        Assert.True(explicitCode.WasExplicit);

        var state = await fixture.Db.MasterDataCodeSequenceStates.SingleAsync(x => x.RuleId == rule.Id);
        Assert.Equal(0, state.CurrentValue);
    }

    [Fact]
    public async Task Same_Tenant_Concurrent_Generation_Is_Unique()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using (var seed = MasterDataCodeFixture.Create(databaseName))
        {
            await seed.SeedRuleAsync("BusinessPartner", tenantId: 1, prefix: "BP", sequenceLength: 6);
        }

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                await using var fixture = MasterDataCodeFixture.Create(databaseName);
                var generated = await fixture.Service.GenerateNextAsync(new MasterDataCodeRequest(
                    EntityType: "BusinessPartner",
                    TenantId: 1,
                    CompanyId: null,
                    WarehouseId: null,
                    ExplicitCode: null));
                return generated.Code;
            }))
            .ToArray();

        var codes = await Task.WhenAll(tasks);

        Assert.Equal(20, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("BP_000001", codes);
        Assert.Contains("BP_000020", codes);
    }

    [Fact]
    public async Task Tenant_And_Company_Scopes_Are_Isolated()
    {
        await using var fixture = MasterDataCodeFixture.Create();
        await fixture.SeedRuleAsync("BusinessPartner", tenantId: 1, prefix: "BP", sequenceLength: 6);
        await fixture.SeedRuleAsync("BusinessPartner", tenantId: 2, prefix: "BP", sequenceLength: 6);
        await fixture.SeedRuleAsync("Warehouse", tenantId: 1, companyId: 1, prefix: "WH", sequenceLength: 3);
        await fixture.SeedRuleAsync("Warehouse", tenantId: 1, companyId: 2, prefix: "WH", sequenceLength: 3);

        Assert.Equal("BP_000001", (await fixture.GenerateAsync("BusinessPartner", 1)).Code);
        Assert.Equal("BP_000001", (await fixture.GenerateAsync("BusinessPartner", 2)).Code);
        Assert.Equal("WH_001", (await fixture.GenerateAsync("Warehouse", 1, companyId: 1)).Code);
        Assert.Equal("WH_001", (await fixture.GenerateAsync("Warehouse", 1, companyId: 2)).Code);
    }

    [Fact]
    public async Task Missing_Inactive_And_Overflow_Rules_Are_Rejected()
    {
        await using var fixture = MasterDataCodeFixture.Create();

        var missing = await Assert.ThrowsAsync<MdmValidationException>(() =>
            fixture.GenerateAsync("BusinessPartner", 1));
        Assert.Equal(MdmErrorCodes.CodeRuleNotFound, missing.Code);

        await fixture.SeedRuleAsync("BusinessPartner", tenantId: 1, prefix: "BP", sequenceLength: 1, isActive: false);
        var inactive = await Assert.ThrowsAsync<MdmValidationException>(() =>
            fixture.GenerateAsync("BusinessPartner", 1));
        Assert.Equal(MdmErrorCodes.CodeRuleInactive, inactive.Code);

        await fixture.SeedRuleAsync("Item", tenantId: 1, prefix: "IT", sequenceLength: 1, currentValue: 9);
        var overflow = await Assert.ThrowsAsync<MdmValidationException>(() =>
            fixture.GenerateAsync("Item", 1));
        Assert.Equal(MdmErrorCodes.CodeSequenceExhausted, overflow.Code);
    }

    [Fact]
    public async Task BusinessPartner_Create_Empty_Code_Uses_AutoEditable_Rule()
    {
        await using var fixture = BusinessPartnerCodeRuleFixture.Create();
        await fixture.SeedRuleAsync();

        var created = await fixture.BusinessPartners.CreateAsync(new CreateBusinessPartnerRequest(
            Code: " ",
            Name: "Generated Partner",
            ShortName: "GP",
            Role: BusinessPartnerRole.Customer,
            ContactPerson: "Alice",
            Phone: "13900000000",
            Email: "alice@example.com",
            AddressLine1: "Road 1",
            AddressLine2: null,
            City: "Shanghai",
            Region: "Shanghai",
            PostalCode: "200000",
            CountryCode: "CN",
            TaxNumber: null,
            MnemonicCode: null,
            AdministrativeRegionId: null,
            Description: null));

        Assert.Equal("BP_000001", created.Code);
    }

    private sealed class MasterDataCodeFixture : IAsyncDisposable
    {
        private MasterDataCodeFixture(
            MdmDbContext db,
            MasterDataCodeService service)
        {
            Db = db;
            Service = service;
        }

        public MdmDbContext Db { get; }
        public MasterDataCodeService Service { get; }

        public static MasterDataCodeFixture Create(string? databaseName = null)
        {
            // TEST_CORRECTION: the previous implementation created a
            // fresh InMemoryDatabaseRoot here, which made concurrent
            // tasks calling Create(sameDatabaseName) end up with
            // DIFFERENT roots — so the seed phase wrote to one
            // store while the concurrent tasks read from another.
            // The fix is to always route through SharedRoot so the
            // concurrent test can actually observe the seeded rule.
            var name = databaseName ?? Guid.NewGuid().ToString("N");
            var options = new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(name, SharedRoot.For(name))
                .Options;
            var db = new MdmDbContext(options);
            var service = new MasterDataCodeService(
                db,
                NullLogger<MasterDataCodeService>.Instance);
            return new MasterDataCodeFixture(db, service);
        }

        // Kept for explicit root injection (not used by the focused
        // tests; only present so callers that want a fully-isolated
        // InMemory store can still ask for one).
        private static MasterDataCodeFixture Create(string databaseName, InMemoryDatabaseRoot root)
        {
            var options = new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(databaseName, root)
                .Options;
            var db = new MdmDbContext(options);
            var service = new MasterDataCodeService(
                db,
                NullLogger<MasterDataCodeService>.Instance);
            return new MasterDataCodeFixture(db, service);
        }

        public Task<MasterDataCodeResult> GenerateAsync(
            string entityType,
            long tenantId,
            long? companyId = null,
            long? warehouseId = null) =>
            Service.GenerateNextAsync(new MasterDataCodeRequest(
                entityType,
                tenantId,
                companyId,
                warehouseId,
                ExplicitCode: null));

        public async Task<MasterDataCodeRule> SeedRuleAsync(
            string entityType,
            long tenantId,
            long? companyId = null,
            long? warehouseId = null,
            string prefix = "BP",
            int sequenceLength = 6,
            bool isActive = true,
            long currentValue = 0)
        {
            var now = DateTimeOffset.UtcNow;
            var rule = new MasterDataCodeRule
            {
                TenantId = tenantId,
                CompanyId = companyId,
                WarehouseId = warehouseId,
                EntityType = entityType,
                Mode = MasterDataCodeMode.AutoEditable,
                Prefix = prefix,
                Separator = "_",
                SequenceLength = sequenceLength,
                StartValue = 1,
                IsActive = isActive,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            Db.MasterDataCodeRules.Add(rule);
            await Db.SaveChangesAsync();
            Db.MasterDataCodeSequenceStates.Add(new MasterDataCodeSequenceState
            {
                RuleId = rule.Id,
                TenantId = tenantId,
                CompanyId = companyId,
                WarehouseId = warehouseId,
                CurrentValue = currentValue,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            });
            await Db.SaveChangesAsync();
            return rule;
        }

        public async ValueTask DisposeAsync() => await Db.DisposeAsync();
    }

    private sealed class BusinessPartnerCodeRuleFixture : IAsyncDisposable
    {
        private BusinessPartnerCodeRuleFixture(
            MdmDbContext db,
            MdmBusinessPartnerService businessPartners,
            MasterDataCodeService codeService)
        {
            Db = db;
            BusinessPartners = businessPartners;
            CodeService = codeService;
        }

        public MdmDbContext Db { get; }
        public MdmBusinessPartnerService BusinessPartners { get; }
        public MasterDataCodeService CodeService { get; }

        public static BusinessPartnerCodeRuleFixture Create()
        {
            var db = new MdmDbContext(new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);
            var codeService = new MasterDataCodeService(
                db,
                NullLogger<MasterDataCodeService>.Instance);
            var service = new MdmBusinessPartnerService(
                db,
                new MutableCurrentTenant(1),
                new MutableCurrentUser(99),
                codeService,
                NullLogger<MdmBusinessPartnerService>.Instance);
            return new BusinessPartnerCodeRuleFixture(db, service, codeService);
        }

        public async Task SeedRuleAsync()
        {
            var now = DateTimeOffset.UtcNow;
            // GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 3
            // (2026-08-28): the InMemory test DB does NOT inherit the
            // MdmReferenceDataService seed, so the test fixture must
            // explicitly seed the CN country used by the BP create test.
            // This mirrors what MdmReferenceDataService.EnsureSeedAsync
            // does on the real PG database; the migration also leaves
            // the country table empty until the operator runs that seed
            // (which is a separate, idempotent step on a fresh DB).
            if (!await Db.Countries.AnyAsync(c => c.Code == "CN"))
            {
                Db.Countries.Add(new Country
                {
                    Code = "CN",
                    Alpha3Code = "CHN",
                    Name = "中国",
                    EnglishName = "China",
                    IsActive = true,
                    SortOrder = 1,
                    CreatedAt = now,
                    ModifiedAt = now,
                    ConcurrencyVersion = 1,
                });
                await Db.SaveChangesAsync();
            }
            var rule = new MasterDataCodeRule
            {
                TenantId = 1,
                EntityType = "BusinessPartner",
                Mode = MasterDataCodeMode.AutoEditable,
                Prefix = "BP",
                Separator = "_",
                SequenceLength = 6,
                StartValue = 1,
                IsActive = true,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            Db.MasterDataCodeRules.Add(rule);
            await Db.SaveChangesAsync();
            Db.MasterDataCodeSequenceStates.Add(new MasterDataCodeSequenceState
            {
                RuleId = rule.Id,
                TenantId = 1,
                CurrentValue = 0,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            });
            await Db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync() => await Db.DisposeAsync();
    }

    private static class SharedRoot
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<string, InMemoryDatabaseRoot> Roots = new(StringComparer.Ordinal);

        public static InMemoryDatabaseRoot For(string databaseName)
        {
            lock (Gate)
            {
                if (!Roots.TryGetValue(databaseName, out var root))
                {
                    root = new InMemoryDatabaseRoot();
                    Roots.Add(databaseName, root);
                }
                return root;
            }
        }
    }

    private sealed class MutableCurrentTenant(long id) : ICurrentTenant
    {
        public long? Id { get; private set; } = id;
        public string? Name => null;
        public bool IsAvailable => Id.HasValue;
        public IDisposable Change(long? tenantId)
        {
            var previous = Id;
            Id = tenantId;
            return new Restore(() => Id = previous);
        }
    }

    private sealed class MutableCurrentUser(long id) : ICurrentUser
    {
        public long? Id { get; private set; } = id;
        public string? UserName => "tester";
        public bool IsAuthenticated => Id.HasValue;
        public bool IsPlatformAdmin => false;
        public IDisposable Change(long? userId)
        {
            var previous = Id;
            Id = userId;
            return new Restore(() => Id = previous);
        }
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
