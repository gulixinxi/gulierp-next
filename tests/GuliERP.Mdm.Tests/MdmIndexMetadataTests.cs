using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// MDM-001R3 — Structural index metadata regression tests.
///
/// <para>
/// These tests load the migration
/// <c>MdmDbContextModelSnapshot</c> directly via
/// <see cref="IMigrationsAssembly"/> and assert that the snapshot
/// model is structurally valid AND consistent with the live
/// runtime model. They do NOT require a real PostgreSQL
/// connection — they only need an <c>MdmDbContext</c> with
/// <c>UseNpgsql</c> so the relational + Npgsql providers are
/// wired.
/// </para>
///
/// <para>
/// The original bug (R3): <c>Designer.cs</c> + <c>Snapshot.cs</c>
/// used <c>b.HasIndex("Code", "ux_gulierp_uom_code")</c>. The
/// 2-arg <c>HasIndex(string, string)</c> overload was removed in
/// EF Core 7+. Under EF Core 10 the call resolves to
/// <c>HasIndex(params string[])</c>, EF tries to add
/// <c>ux_gulierp_uom_code</c> as a shadow property on
/// <see cref="Uom"/>, and throws
/// <c>InvalidOperationException: The property
/// 'ux_gulierp_uom_code' cannot be added to the type 'Uom
/// (Dictionary&lt;string, object&gt;)' ...</c>.
/// </para>
///
/// <para>
/// These tests are the regression guard. If anyone re-introduces
/// the 2-arg <c>HasIndex</c> form (or any other EF-Core-7+
/// breaking-change form) the model build will throw, and the
/// <c>Uom_Code_Index_…</c> tests will fail because the snapshot
/// cannot be loaded.
/// </para>
/// </summary>
public sealed class MdmIndexMetadataTests
{
    private static MdmDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MdmDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=mdm_metadata_test;Username=test;Password=test;Pooling=false",
                npg => npg.MigrationsHistoryTable("__ef_migrations_history", MdmDbContext.DefaultSchema))
            .Options;
        return new MdmDbContext(options);
    }

    private static IModel LoadSnapshotModel(MdmDbContext ctx)
    {
        var migrationsAssembly = ctx.GetService<IMigrationsAssembly>();
        var snapshot = migrationsAssembly.ModelSnapshot
            ?? throw new InvalidOperationException(
                "No ModelSnapshot found in the Migrations assembly.");
        // The bug surface: this property getter calls
        // ModelSnapshot.BuildModel() under the hood. If the
        // Snapshot has the 2-arg HasIndex bug, BuildModel throws
        // BEFORE we get an IModel back.
        return snapshot.Model;
    }

    // Direct BuildModel probe. Bypasses the IMigrationsAssembly
    // cache that some EF Core 10 builds use, and directly invokes
    // the source-defined MdmDbContextModelSnapshot class. This
    // is the EXACT code path that the operator's
    // `dotnet ef database update` exercised when it failed.
    private static IModel BuildSnapshotModelDirectly()
    {
        var asm = typeof(GuliERP.Mdm.Infrastructure.Persistence.MdmDbContext).Assembly;
        var snapshotType = asm.GetType(
            "GuliERP.Mdm.Infrastructure.Migrations.MdmDbContextModelSnapshot",
            throwOnError: true)!;
        var instance = (Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot)
            Activator.CreateInstance(snapshotType)!;
        return instance.Model;
    }

    [Fact]
    public void MdmDbContextModelSnapshot_Builds_Without_HasIndex_Overload_Regression()
    {
        // The single most important regression test: the
        // Snapshot must be loadable. If this throws, the
        // mdm-001R3 bug has re-appeared.
        using var ctx = CreateContext();
        var model = LoadSnapshotModel(ctx);
        Assert.NotNull(model);
    }

    [Fact]
    public void MdmDbContextModelSnapshot_Builds_Directly_Without_HasIndex_Overload_Regression()
    {
        // Direct BuildModel probe (bypasses IMigrationsAssembly
        // caching). This is the EXACT code path that
        // `dotnet ef database update` exercises. The original
        // mdm-001R3 failure occurred here.
        var model = BuildSnapshotModelDirectly();
        Assert.NotNull(model);
    }

    [Fact]
    public void MdmDbContextModelSnapshot_Has_No_Shadow_Property_Named_Like_Index()
    {
        // The original bug surfaced as EF trying to add
        // `ux_gulierp_uom_code` as a shadow property. The fix
        // resolves the call to a real index declaration. This
        // test asserts that NO entity in the snapshot model has
        // a shadow property whose name starts with `ux_` or
        // `ix_` — i.e. a DB index name was never accidentally
        // registered as a property.
        using var ctx = CreateContext();
        var model = LoadSnapshotModel(ctx);
        foreach (var entity in model.GetEntityTypes())
        {
            foreach (var prop in entity.GetProperties())
            {
                Assert.False(
                    prop.Name.StartsWith("ux_", StringComparison.Ordinal)
                        || prop.Name.StartsWith("ix_", StringComparison.Ordinal),
                    $"Entity {entity.Name} has a shadow property named like a DB index: '{prop.Name}'.");
            }
        }
    }

    [Fact]
    public void MdmDbContextModelSnapshot_Uom_Code_Index_Exists_With_Correct_Name()
    {
        // The unique index on Uom.Code must exist on the
        // snapshot model with DB name `ux_gulierp_uom_code` and
        // a single property `Code`.
        using var ctx = CreateContext();
        var model = LoadSnapshotModel(ctx);
        var uom = model.FindEntityType(typeof(Uom))
            ?? throw new InvalidOperationException("Uom entity not found in snapshot model.");
        var index = uom.GetIndexes()
            .FirstOrDefault(i => i.IsUnique
                && i.Properties.Count == 1
                && i.Properties[0].Name == "Code")
            ?? throw new InvalidOperationException("Uom unique index on Code not found in snapshot model.");
        Assert.Equal("ux_gulierp_uom_code", index.GetDatabaseName());
    }

    [Fact]
    public void MdmDbContextModelSnapshot_ItemCategory_Composite_Index_Exists_With_Correct_Name()
    {
        // The unique composite index on (TenantId, Code) for
        // ItemCategory must exist on the snapshot model with DB
        // name `ux_gulierp_item_category_tenant_code`.
        using var ctx = CreateContext();
        var model = LoadSnapshotModel(ctx);
        var entity = model.FindEntityType(typeof(ItemCategory))
            ?? throw new InvalidOperationException("ItemCategory entity not found in snapshot model.");
        var index = entity.GetIndexes()
            .FirstOrDefault(i => i.IsUnique
                && i.Properties.Count == 2
                && i.Properties.Any(p => p.Name == "TenantId")
                && i.Properties.Any(p => p.Name == "Code"))
            ?? throw new InvalidOperationException("ItemCategory unique composite (TenantId, Code) index not found.");
        Assert.Equal("ux_gulierp_item_category_tenant_code", index.GetDatabaseName());
    }

    [Fact]
    public void MdmDbContextModelSnapshot_Item_Composite_Index_Exists_With_Correct_Name()
    {
        // The unique composite index on (TenantId, Code) for
        // Item must exist on the snapshot model with DB name
        // `ux_gulierp_item_tenant_code`.
        using var ctx = CreateContext();
        var model = LoadSnapshotModel(ctx);
        var entity = model.FindEntityType(typeof(Item))
            ?? throw new InvalidOperationException("Item entity not found in snapshot model.");
        var index = entity.GetIndexes()
            .FirstOrDefault(i => i.IsUnique
                && i.Properties.Count == 2
                && i.Properties.Any(p => p.Name == "TenantId")
                && i.Properties.Any(p => p.Name == "Code"))
            ?? throw new InvalidOperationException("Item unique composite (TenantId, Code) index not found.");
        Assert.Equal("ux_gulierp_item_tenant_code", index.GetDatabaseName());
    }

    [Fact]
    public void MdmDbContext_Snapshot_Matches_Runtime_Model_For_Uom_Code_Index()
    {
        // The snapshot must agree with the current Uom
        // configuration. This is the strongest single-entity
        // drift check.
        using var ctx = CreateContext();
        var runtime = ctx.Model.FindEntityType(typeof(Uom))!;
        var snapshotModel = LoadSnapshotModel(ctx);
        var snapshot = snapshotModel.FindEntityType(typeof(Uom))!;

        var runtimeIdx = runtime.GetIndexes()
            .Single(i => i.IsUnique
                && i.Properties.Count == 1
                && i.Properties[0].Name == "Code");
        var snapshotIdx = snapshot.GetIndexes()
            .Single(i => i.IsUnique
                && i.Properties.Count == 1
                && i.Properties[0].Name == "Code");

        Assert.Equal(runtimeIdx.GetDatabaseName(), snapshotIdx.GetDatabaseName());
    }
}
