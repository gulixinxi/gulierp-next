using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScopeQuakeQueryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                schema: "crm",
                table: "quake_query_history",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TenantId",
                schema: "crm",
                table: "quake_query_history",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_quake_query_history_TenantId_CompanyId_QueryHash_QueriedAt",
                schema: "crm",
                table: "quake_query_history",
                columns: new[] { "TenantId", "CompanyId", "QueryHash", "QueriedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_quake_query_history_TenantId_CompanyId_QueryHash_QueriedAt",
                schema: "crm",
                table: "quake_query_history");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "crm",
                table: "quake_query_history");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "crm",
                table: "quake_query_history");
        }
    }
}
