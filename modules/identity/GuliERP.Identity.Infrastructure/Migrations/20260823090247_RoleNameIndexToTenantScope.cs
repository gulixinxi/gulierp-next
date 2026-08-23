using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RoleNameIndexToTenantScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ----------------------------------------------------------------
            // Step 0: preflight — refuse to migrate if any intra-tenant
            // duplicate NormalizedName rows exist. Cross-tenant
            // duplicates are EXPECTED and allowed (they are the
            // design target of this migration).
            // ----------------------------------------------------------------
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    dup_count bigint;
                BEGIN
                    SELECT COUNT(*) INTO dup_count
                    FROM (
                        SELECT ""TenantId"", ""NormalizedName"", COUNT(*) AS c
                        FROM identity.""AspNetRoles""
                        WHERE ""NormalizedName"" IS NOT NULL
                        GROUP BY ""TenantId"", ""NormalizedName""
                        HAVING COUNT(*) > 1
                    ) d;
                    IF dup_count > 0 THEN
                        RAISE EXCEPTION
                            'RoleNameIndexToTenantScope: found % intra-tenant duplicate NormalizedName rows. Resolve manually before migration.', dup_count;
                    END IF;
                END $$;
            ");

            // ----------------------------------------------------------------
            // Step 1: DROP the legacy ASP.NET Identity `RoleNameIndex`
            // (GLOBAL UNIQUE on NormalizedName). After this step there
            // is no index on NormalizedName alone; we re-add a
            // per-tenant composite in Step 3.
            // ----------------------------------------------------------------
            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "AspNetRoles");

            // ----------------------------------------------------------------
            // Step 2: backfill any NULL NormalizedName rows to a
            // deterministic placeholder so the new UNIQUE index is
            // safe. In the GuliERP model, NormalizedName is REQUIRED
            // but historically could be NULL on the initial EF Core
            // CreateTable. The new per-tenant UNIQUE index
            // (TenantId, NormalizedName) accepts NULLs (PG semantics:
            // NULL != NULL), but RoleManager.FindByNameAsync can never
            // find a Role with NULL NormalizedName, leaving it as dead
            // code. Backfill first to keep the database consistent.
            // ----------------------------------------------------------------
            migrationBuilder.Sql(@"
                UPDATE identity.""AspNetRoles""
                SET ""NormalizedName"" = UPPER(COALESCE(""Name"", 'UNNAMED_' || ""Id""::text))
                WHERE ""NormalizedName"" IS NULL OR ""NormalizedName"" = '';
            ");

            // ----------------------------------------------------------------
            // Step 3: CREATE the per-tenant composite UNIQUE index
            // (TenantId, NormalizedName). Two Tenants may now
            // legitimately define the same business role (e.g.
            // "ERP_MDM_OPERATOR"). The pre-existing
            // `ux_gulierp_role_tenant_code` (TenantId, Code) is
            // preserved.
            // ----------------------------------------------------------------
            migrationBuilder.CreateIndex(
                name: "ux_gulierp_role_tenant_normalizedname",
                schema: "identity",
                table: "AspNetRoles",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the per-tenant composite first.
            migrationBuilder.DropIndex(
                name: "ux_gulierp_role_tenant_normalizedname",
                schema: "identity",
                table: "AspNetRoles");

            // Re-create the legacy GLOBAL UNIQUE on NormalizedName.
            // NOTE: this Down() will FAIL if any cross-tenant
            // duplicate NormalizedName exists (i.e. if the migration
            // was Up'd and then any Tenant successfully created a
            // business role whose NormalizedName was already used by
            // another Tenant). The Operator must resolve the
            // duplicates manually (rename or delete the offending
            // rows) before re-running Down.
            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);
        }
    }
}
