using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <summary>
    /// GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_SCHEMA_AND_OPERATOR_CLOSURE
    /// (2026-08-30) — MDM007.
    ///
    /// <para>
    /// Replaces the V1 Location Code-uniqueness constraint
    /// <c>(TenantId, CompanyId, Code)</c> with the frozen
    /// Contract shape
    /// <c>(TenantId, CompanyId, WarehouseId, Code)</c>. This
    /// matches the Foundation's per-Warehouse code-rule scope
    /// for the <c>Location</c> entity and lets two Warehouses
    /// in the same Company independently start at
    /// <c>LOC_000001</c> without colliding.
    /// </para>
    ///
    /// <para>
    /// <b>OPERATOR PREREQUISITE (BLOCKING)</b>: run
    /// <c>tools/dev/gulierp-mdm007-location-warehouse-precheck.ps1</c>
    /// BEFORE applying this migration. The precheck scans
    /// <c>mdm.gulierp_location</c> for any
    /// <c>(TenantId, CompanyId, WarehouseId, Code)</c> row with
    /// count &gt; 1. If found, the precheck returns
    /// <c>LOCATION_EXISTING_COLLISION_BLOCKER</c> (exit 1) and
    /// the migration must NOT be applied. The brief
    /// (GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_SCHEMA_AND_OPERATOR_CLOSURE
    /// §七) explicitly forbids destructive data rewrite, automatic
    /// renumbering, and code auto-append.
    /// </para>
    ///
    /// <para>
    /// <b>SCOPE</b>: the migration itself is purely a constraint
    /// swap. No data is rewritten; no row is dropped; no
    /// column is dropped. The Up migration:
    /// </para>
    ///
    /// <list type="number">
    ///   <item>Drops the old unique index
    ///         <c>ux_gulierp_location_tenant_company_code</c>
    ///         on <c>(TenantId, CompanyId, Code)</c>.</item>
    ///   <item>Creates the new unique index
    ///         <c>ux_gulierp_location_tenant_company_warehouse_code</c>
    ///         on <c>(TenantId, CompanyId, WarehouseId, Code)</c>.</item>
    /// </list>
    ///
    /// <para>
    /// <b>Down</b> reverses the swap (drop new, recreate old)
    /// only when no (TenantId, CompanyId, WarehouseId, Code)
    /// collision would result. EF does not check this; the
    /// Operator must re-run the precheck before Down.
    /// </para>
    /// </summary>
    public partial class MDM007_LocationWarehouseScopedCodeUniqueness : Migration
    {
        private const string Schema = "mdm";
        private const string Table = "gulierp_location";
        private const string OldIndex = "ux_gulierp_location_tenant_company_code";
        private const string NewIndex = "ux_gulierp_location_tenant_company_warehouse_code";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop the old V1 unique index IF it exists. (The
            //    brief §9 mandates a constraint swap; per the
            //    §33 destructive-data-write ban, we do NOT touch
            //    rows. If the old index is missing (e.g. the
            //    schema was previously reconciled out-of-band by
            //    an Operator), the DROP is a silent no-op so the
            //    migration remains idempotent on the Operator's
            //    already-advanced DBs.)
            migrationBuilder.Sql(
                $"DROP INDEX IF EXISTS \"{Schema}\".\"{OldIndex}\";");

            // 2. Create the new warehouse-scoped unique index.
            //    The same IF NOT EXISTS guard protects against
            //    double-applies.
            migrationBuilder.Sql(
                $"CREATE UNIQUE INDEX IF NOT EXISTS \"{NewIndex}\" " +
                $"ON \"{Schema}\".\"{Table}\" " +
                "(\"TenantId\", \"CompanyId\", \"WarehouseId\", \"Code\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the swap. The Operator MUST re-run the
            // precheck before applying this Down — the old index
            // is tighter and may reject rows the new one allows.
            migrationBuilder.Sql(
                $"DROP INDEX IF EXISTS \"{Schema}\".\"{NewIndex}\";");

            migrationBuilder.Sql(
                $"CREATE UNIQUE INDEX IF NOT EXISTS \"{OldIndex}\" " +
                $"ON \"{Schema}\".\"{Table}\" " +
                "(\"TenantId\", \"CompanyId\", \"Code\");");
        }
    }
}
