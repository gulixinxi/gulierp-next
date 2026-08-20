using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace GuliERP.Identity.IntegrationTests;

/// <summary>
/// ID-GEN-001 focused proof for PostgreSQL sequence-backed EF/Npgsql HiLo.
/// These tests require the Operator PostgreSQL target because HiLo obtains
/// sequence ranges from the database before INSERT.
/// </summary>
public sealed class IdGenerationHiLoFacts : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ExpectedOperatorDatabase = "gulierp_g2_003_test";

    private readonly WebApplicationFactory<Program> _factory;

    public IdGenerationHiLoFacts(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HiLo_PreGenerates_Id_Before_SaveChanges()
    {
        using var factory = BuildRealPostgreSqlFactory();
        using var scope = factory.Services.CreateScope();
        var db = await GetGuardedMigratedDbAsync(scope.ServiceProvider);
        var beforeMax = await GetMaxExistingTechnicalIdAsync(db);

        var tenant = NewTenant("PRESAVE");
        db.Tenants.Add(tenant);

        Assert.NotEqual(0L, tenant.Id);
        Assert.True(tenant.Id > beforeMax);
        Assert.Equal(EntityState.Added, db.Entry(tenant).State);
        Assert.False(await db.Tenants.AsNoTracking().AnyAsync(t => t.Id == tenant.Id));
    }

    [Fact]
    public async Task HiLo_Persists_Long_Id()
    {
        using var factory = BuildRealPostgreSqlFactory();
        using var scope = factory.Services.CreateScope();
        var db = await GetGuardedMigratedDbAsync(scope.ServiceProvider);
        var tenant = NewTenant("PERSIST");
        db.Tenants.Add(tenant);
        var generatedId = tenant.Id;

        await db.SaveChangesAsync();

        try
        {
            using var freshScope = factory.Services.CreateScope();
            var freshDb = await GetGuardedMigratedDbAsync(freshScope.ServiceProvider);
            var reloaded = await freshDb.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == generatedId);

            Assert.NotNull(reloaded);
            Assert.Equal(generatedId, reloaded!.Id);
            Assert.IsType<long>(reloaded.Id);
        }
        finally
        {
            await CleanupTenantAsync(factory, generatedId);
        }
    }

    [Fact]
    public async Task HiLo_FreshScopes_Produce_Distinct_Ids()
    {
        using var factory = BuildRealPostgreSqlFactory();

        using var firstScope = factory.Services.CreateScope();
        var firstDb = await GetGuardedMigratedDbAsync(firstScope.ServiceProvider);
        var first = NewTenant("SCOPEA");
        firstDb.Tenants.Add(first);

        using var secondScope = factory.Services.CreateScope();
        var secondDb = await GetGuardedMigratedDbAsync(secondScope.ServiceProvider);
        var second = NewTenant("SCOPEB");
        secondDb.Tenants.Add(second);

        Assert.NotEqual(0L, first.Id);
        Assert.NotEqual(0L, second.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task HiLo_Does_Not_Collide_With_Existing_Id_Range()
    {
        using var factory = BuildRealPostgreSqlFactory();
        using var scope = factory.Services.CreateScope();
        var db = await GetGuardedMigratedDbAsync(scope.ServiceProvider);
        var beforeMax = await GetMaxExistingTechnicalIdAsync(db);

        var tenant = NewTenant("RANGE");
        db.Tenants.Add(tenant);

        Assert.True(
            tenant.Id > beforeMax,
            $"Generated HiLo Id {tenant.Id} must be greater than existing max technical Id {beforeMax}.");
    }

    [Fact]
    public void Snowflake_Not_Registered_In_Production()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting(
                "ConnectionStrings:GuliERP",
                "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2;Command Timeout=2");
        });

        var snowflakeType = Type.GetType(
            "GuliERP.Foundation.Kernel.SnowflakeIdGenerator, GuliERP.Foundation",
            throwOnError: false);

        Assert.Null(snowflakeType);
    }

    [Fact]
    public void Existing_Foundation_Identity_Long_Keys_Remain_Compatible()
    {
        using var factory = BuildMetadataOnlyFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var entityTypes = new[]
        {
            typeof(Tenant),
            typeof(Company),
            typeof(Plant),
            typeof(OrganizationUnit),
            typeof(GuliErpUser),
            typeof(GuliErpRole),
            typeof(UserCompanyMembership),
            typeof(UserOrganizationMembership),
            typeof(UserRoleAssignment),
        };

        foreach (var entityType in entityTypes)
        {
            var id = db.Model.FindEntityType(entityType)?.FindProperty("Id");
            Assert.NotNull(id);
            Assert.Equal(typeof(long), id!.ClrType);
            Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);
            Assert.Equal(IdentityDbContext.HiLoSequenceName, id.GetHiLoSequenceName());
        }
    }

    private WebApplicationFactory<Program> BuildRealPostgreSqlFactory()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    private WebApplicationFactory<Program> BuildMetadataOnlyFactory()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:GuliERP",
                "Host=127.0.0.1;Port=1;Database=metadata_only;Username=none;Password=none;Timeout=2;Command Timeout=2");
        });
    }

    private static async Task<IdentityDbContext> GetGuardedMigratedDbAsync(IServiceProvider sp)
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var connectionString = cfg.GetConnectionString("GuliERP");
        AssertOperatorTarget(connectionString);

        var db = sp.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync();
        return db;
    }

    private static void AssertOperatorTarget(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ID-GEN-001 HiLo evidence requires ConnectionStrings__GuliERP.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.Database, ExpectedOperatorDatabase, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Wrong DB target for ID-GEN-001. Expected {ExpectedOperatorDatabase}; actual {builder.Database}.");
        }
    }

    private static Tenant NewTenant(string marker)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;
        return new Tenant
        {
            Code = $"IDGEN-{marker}-{suffix}",
            Name = $"ID-GEN-001 {marker} Tenant",
            Status = TenantStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
    }

    private static async Task<long> GetMaxExistingTechnicalIdAsync(IdentityDbContext db)
    {
        var values = new[]
        {
            await db.Tenants.AsNoTracking().Select(t => (long?)t.Id).MaxAsync() ?? 0,
            await db.Companies.AsNoTracking().Select(c => (long?)c.Id).MaxAsync() ?? 0,
            await db.Plants.AsNoTracking().Select(p => (long?)p.Id).MaxAsync() ?? 0,
            await db.OrganizationUnits.AsNoTracking().Select(o => (long?)o.Id).MaxAsync() ?? 0,
            await db.Users.AsNoTracking().Select(u => (long?)u.Id).MaxAsync() ?? 0,
            await db.Roles.AsNoTracking().Select(r => (long?)r.Id).MaxAsync() ?? 0,
            await db.UserCompanyMemberships.AsNoTracking().Select(m => (long?)m.Id).MaxAsync() ?? 0,
            await db.UserOrganizationMemberships.AsNoTracking().Select(m => (long?)m.Id).MaxAsync() ?? 0,
            await db.UserRoleAssignments.AsNoTracking().Select(a => (long?)a.Id).MaxAsync() ?? 0,
        };

        return values.Max();
    }

    private static async Task CleanupTenantAsync(WebApplicationFactory<Program> factory, long tenantId)
    {
        try
        {
            using var scope = factory.Services.CreateScope();
            var db = await GetGuardedMigratedDbAsync(scope.ServiceProvider);
            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM identity.gulierp_tenant WHERE \"Id\" = {0}",
                tenantId);
        }
        catch
        {
            // Best-effort cleanup. IDs and codes are unique per run.
        }
    }
}
