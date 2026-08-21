using Xunit;

namespace GuliERP.Mdm.IntegrationTests;

/// <summary>
/// mdm-001R6 (PostgreSQL Integration Stabilization) — single xUnit
/// collection that DISABLES parallelization for all 3 MDM
/// integration test classes. They all hit the same canonical
/// PostgreSQL database (per the brief: "use the operator's
/// canonical test DB; do not spin up a fresh DB"), and several
/// of their contracts depend on global state on that DB:
/// <list type="bullet">
///   <item><c>MdmUomFacts.UomSeed_Loads_13Rows_And_IsIdempotent</c>
///         reads the global <c>mdm.gulierp_uom</c> sentinel
///         (BENG) and expects 13 SAFE rows to be present.</item>
///   <item><c>MdmItemCategoryAndItemFacts</c> tests create rows
///         under random per-run tenant ids but the KGM UOM is
///         shared system state.</item>
///   <item><c>MdmMigrationFacts.Migration_Applies_Schema_And_Tables_Exist</c>
///         runs <c>db.Database.MigrateAsync</c> against the same
///         <c>__ef_migrations_history</c> table.</item>
/// </list>
///
/// <para>
/// Concurrent test classes would race on the
/// <c>__ef_migrations_history</c> insert / on the BENG
/// insert-then-idempotent-skip pattern. The brief §四 explicitly
/// distinguishes parallel-safety from shared-state: this
/// collection disables parallel execution so the tests run
/// sequentially against the shared DB, while the per-test data
/// isolation (per-run unique tenant ids, raw-SQL DELETE
/// best-effort cleanup) is handled in the individual tests.
/// </para>
///
/// <para>
/// mdm-001R6 also adds a structural
/// <see cref="Mdm.Tests.MdmCurrentTenantParallelTests"/> regression
/// suite that proves the <see cref="ICurrentTenant"/>
/// implementation is <c>AsyncLocal</c>-backed and therefore
/// safe under truly-parallel test execution (we do not need
/// parallel runs in this integration suite; the structural
/// unit test suite certifies that the production tenant
/// context is parallel-safe even though we choose not to
/// exercise it here).
/// </para>
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MdmPostgresIntegrationCollection
{
    /// <summary>
    /// Canonical collection name used by
    /// <c>[Collection(MdmPostgresIntegrationCollection.Name)]</c>
    /// on each test class.
    /// </summary>
    public const string Name = "Mdm-Postgres-Integration-Sequential";
}
