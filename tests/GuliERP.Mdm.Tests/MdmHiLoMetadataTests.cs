using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace GuliERP.Mdm.Tests;

/// <summary>
/// MDM-001R1 — Structural HiLo metadata regression tests.
///
/// <para>
/// These tests read the EF Core model DIRECTLY (no PostgreSQL
/// connection required) and assert that the HiLo sequence is
/// bound to the canonical
/// <c>identity.gulierp_hilo_sequence</c> — NOT to
/// <c>mdm.gulierp_hilo_sequence</c> (the default-schema trap).
/// </para>
///
/// <para>
/// Per the MDM-001R1 brief §7: this is a focused structural
/// test that protects against a silent regression where the
/// module's default schema accidentally absorbs the sequence
/// schema. The same regression is what the R1 operator evidence
/// caught at runtime via
/// <c>NpgsqlSequenceHiLoValueGenerator</c> reporting
/// <c>mdm.gulierp_hilo_sequence</c>.
/// </para>
/// </summary>
public sealed class MdmHiLoMetadataTests
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

    private static (string? SequenceName, string? Schema) GetHiLoSequence(IModel model, Type entityType, string propertyName)
    {
        var entity = model.FindEntityType(entityType);
        Assert.NotNull(entity);
        var property = entity!.FindProperty(propertyName);
        Assert.NotNull(property);
        var hiLo = property!.GetHiLoSequenceName();
        var hiLoSchema = property.GetHiLoSequenceSchema();
        return (hiLo, hiLoSchema);
    }

    [Fact]
    public void Uom_Id_HiLo_Binds_To_Canonical_Identity_Sequence()
    {
        using var ctx = CreateContext();
        var (name, schema) = GetHiLoSequence(ctx.Model, typeof(Uom), nameof(Uom.Id));
        Assert.Equal("gulierp_hilo_sequence", name);
        Assert.Equal("identity", schema);
    }

    [Fact]
    public void ItemCategory_Id_HiLo_Binds_To_Canonical_Identity_Sequence()
    {
        using var ctx = CreateContext();
        var (name, schema) = GetHiLoSequence(ctx.Model, typeof(ItemCategory), nameof(ItemCategory.Id));
        Assert.Equal("gulierp_hilo_sequence", name);
        Assert.Equal("identity", schema);
    }

    [Fact]
    public void Item_Id_HiLo_Binds_To_Canonical_Identity_Sequence()
    {
        using var ctx = CreateContext();
        var (name, schema) = GetHiLoSequence(ctx.Model, typeof(Item), nameof(Item.Id));
        Assert.Equal("gulierp_hilo_sequence", name);
        Assert.Equal("identity", schema);
    }

    [Fact]
    public void MdmDbContext_DefaultSchema_Is_mdm_And_HiLoSchema_Is_identity_Disjoint()
    {
        // The schema that owns the tables (mdm) is DIFFERENT from
        // the schema that owns the HiLo sequence (identity). This
        // is the cross-module wiring contract.
        using var ctx = CreateContext();
        Assert.Equal("mdm", MdmDbContext.DefaultSchema);
        Assert.Equal("identity", MdmDbContext.HiLoSequenceSchema);
        Assert.NotEqual(MdmDbContext.DefaultSchema, MdmDbContext.HiLoSequenceSchema);

        // The model is built with the mdm default schema.
        var defaultSchemaAnnotation = ctx.Model.GetDefaultSchema();
        Assert.Equal("mdm", defaultSchemaAnnotation);

        // But the HiLo sequence annotations on the Id properties
        // explicitly point at the identity schema.
        foreach (var entityType in new[] { typeof(Uom), typeof(ItemCategory), typeof(Item) })
        {
            var id = ctx.Model.FindEntityType(entityType)!.FindProperty("Id")!;
            Assert.Equal("identity", id.GetHiLoSequenceSchema());
            Assert.Equal("gulierp_hilo_sequence", id.GetHiLoSequenceName());
        }
    }

    [Fact]
    public void MdmDbContext_DoesNot_Declare_Any_Other_HiLo_Sequence()
    {
        // Defensive: there is exactly ONE HiLo sequence in the
        // MDM model — the canonical identity one. No module-local
        // sequence is allowed (ID Strategy V1 freezes the
        // single bigint space).
        using var ctx = CreateContext();
        var sequences = ctx.Model.GetSequences().ToList();
        Assert.Single(sequences);
        Assert.Equal("gulierp_hilo_sequence", sequences[0].Name);
        Assert.Equal("identity", sequences[0].Schema);
    }
}
