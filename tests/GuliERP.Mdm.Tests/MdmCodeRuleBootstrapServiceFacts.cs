using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Mdm;
using GuliERP.Mdm.Infrastructure.Persistence;
using GuliERP.Mdm.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GuliERP.Mdm.Tests;

public sealed class MdmCodeRuleBootstrapServiceFacts
{
    [Fact]
    public async Task Default_Rule_Created()
    {
        await using var fx = CodeRuleBootstrapFixture.Create();
        await fx.SeedTenantAsync(1);

        var result = await fx.Bootstrap.EnsureDefaultBusinessPartnerRuleForTenantAsync(1, 99);

        Assert.True(result.RuleCreated);
        Assert.True(result.SequenceStateCreated);
        Assert.Equal("BP", result.Prefix);
        Assert.Equal(1, result.StartValue);
    }

    [Fact]
    public async Task Idempotent()
    {
        await using var fx = CodeRuleBootstrapFixture.Create();
        await fx.SeedTenantAsync(1);

        var first = await fx.Bootstrap.EnsureDefaultBusinessPartnerRuleForTenantAsync(1, 99);
        var firstRuleId = first.RuleId;

        var second = await fx.Bootstrap.EnsureDefaultBusinessPartnerRuleForTenantAsync(1, 99);
        Assert.Equal(firstRuleId, second.RuleId);
        Assert.False(second.RuleCreated);
    }

    [Fact]
    public async Task Preview_Does_Not_Consume()
    {
        await using var fx = CodeRuleBootstrapFixture.Create();
        await fx.SeedTenantAsync(1);
        await fx.Bootstrap.EnsureDefaultBusinessPartnerRuleForTenantAsync(1, 99);

        var p1 = await fx.MasterDataCode.PreviewAsync(new MasterDataCodeRequest(
            EntityType: "BusinessPartner", TenantId: 1, CompanyId: null,
            WarehouseId: null, ExplicitCode: null));
        var p2 = await fx.MasterDataCode.PreviewAsync(new MasterDataCodeRequest(
            EntityType: "BusinessPartner", TenantId: 1, CompanyId: null,
            WarehouseId: null, ExplicitCode: null));
        Assert.Equal("BP_000001", p1.Code);
        Assert.Equal("BP_000001", p2.Code);
    }

    private sealed class CodeRuleBootstrapFixture : IAsyncDisposable
    {
        private CodeRuleBootstrapFixture(
            MdmDbContext mdmDb,
            IdentityDbContext identityDb,
            MdmCodeRuleBootstrapService bootstrap,
            MasterDataCodeService codeService)
        {
            MdmDb = mdmDb;
            IdentityDb = identityDb;
            Bootstrap = bootstrap;
            MasterDataCode = codeService;
        }

        public MdmDbContext MdmDb { get; }
        public IdentityDbContext IdentityDb { get; }
        public MdmCodeRuleBootstrapService Bootstrap { get; }
        public MasterDataCodeService MasterDataCode { get; }

        public static CodeRuleBootstrapFixture Create()
        {
            var root = new InMemoryDatabaseRoot();
            var dbName = Guid.NewGuid().ToString("N");
            var mdmOptions = new DbContextOptionsBuilder<MdmDbContext>()
                .UseInMemoryDatabase(dbName, root).Options;
            var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseInMemoryDatabase(dbName, root).Options;
            var mdmDb = new MdmDbContext(mdmOptions);
            var identityDb = new IdentityDbContext(identityOptions);
            var codeService = new MasterDataCodeService(
                mdmDb, NullLogger<MasterDataCodeService>.Instance);
            var bootstrap = new MdmCodeRuleBootstrapService(
                mdmDb, identityDb, NullLogger<MdmCodeRuleBootstrapService>.Instance);
            return new CodeRuleBootstrapFixture(mdmDb, identityDb, bootstrap, codeService);
        }

        public async Task SeedTenantAsync(long tenantId)
        {
            var now = DateTimeOffset.UtcNow;
            IdentityDb.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Code = $"T{tenantId:D2}",
                Name = $"Tenant {tenantId}",
                Status = TenantStatus.Active,
                CreatedAt = now, ModifiedAt = now, ConcurrencyVersion = 1,
            });
            await IdentityDb.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await MdmDb.DisposeAsync();
            await IdentityDb.DisposeAsync();
        }
    }
}