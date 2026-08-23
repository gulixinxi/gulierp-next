using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class G2EnterpriseOrganizationSchemaAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_gulierp_employee_CompanyId",
                schema: "identity",
                table: "gulierp_employee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_gulierp_employee_CompanyId",
                schema: "identity",
                table: "gulierp_employee",
                column: "CompanyId");
        }
    }
}
