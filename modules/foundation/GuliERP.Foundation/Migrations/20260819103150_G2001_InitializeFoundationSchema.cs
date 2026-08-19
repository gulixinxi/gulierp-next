using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Foundation.Migrations
{
    /// <summary>
    /// G2-001 initial migration. The FoundationDbContext ships with zero
    /// <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}"/> entities in G2-001
    /// (per task brief), so EF Core's diff-engine cannot produce any
    /// <c>CreateTable</c> operation. Per the task's explicit allowance we hand-author
    /// the only operation that matters: ensure the <c>foundation</c> schema exists.
    ///
    /// The EF Migrations History table (<c>__ef_migrations_history</c>) is created
    /// automatically by <c>Database.Migrate()</c> on the first apply because
    /// <see cref="FoundationDbContext"/> configures it via
    /// <c>npg.MigrationsHistoryTable("__ef_migrations_history", FoundationDbContext.DefaultSchema)</c>.
    /// The history table therefore lives in the <c>foundation</c> schema, so dropping
    /// the schema drops the migration ledger too (single-source-of-truth for the
    /// Foundation's database footprint).
    ///
    /// Schema choice rationale: per the V1 architecture draft the default schema is
    /// <c>public</c>, but the task instruction explicitly directs the Foundation
    /// baseline into a dedicated <c>foundation</c> schema. This makes the
    /// modular-monolith principle visible at the database level and leaves
    /// <c>public</c> for cross-module shared objects (e.g. enums, helper functions)
    /// that future G2-005+ goals may introduce.
    /// </summary>
    public partial class G2001_InitializeFoundationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent schema creation: the Foundation module owns this schema and
            // can re-run the migration safely. PostgreSQL's CREATE SCHEMA IF NOT
            // EXISTS is supported since 9.3; we target 16.x per the G2 PostgreSQL
            // engineering standard.
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS foundation;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Symmetric to Up. IF EXISTS protects against the case where the
            // schema was never created (e.g. partial-failure replay).
            //
            // SECURITY NOTE: dropping the schema also drops the EF Migrations
            // History table, which means a subsequent `dotnet ef database update`
            // would re-apply ALL migrations. This is intentional — the schema
            // is the unit of the Foundation's footprint, not the table.
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS foundation;");
        }
    }
}
