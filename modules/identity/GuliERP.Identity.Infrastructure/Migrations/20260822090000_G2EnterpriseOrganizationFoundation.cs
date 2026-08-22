using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class G2EnterpriseOrganizationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                schema: "identity",
                table: "gulierp_plant",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "gulierp_employee",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    EmployeeNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_employee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gulierp_employee_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gulierp_employee_gulierp_company_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "identity",
                        principalTable: "gulierp_company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gulierp_employee_gulierp_organization_unit_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "identity",
                        principalTable: "gulierp_organization_unit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gulierp_employee_gulierp_tenant_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "identity",
                        principalTable: "gulierp_tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_employee_department",
                schema: "identity",
                table: "gulierp_employee",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_employee_CompanyId",
                schema: "identity",
                table: "gulierp_employee",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_employee_TenantId",
                schema: "identity",
                table: "gulierp_employee",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_employee_company_no",
                schema: "identity",
                table: "gulierp_employee",
                columns: new[] { "CompanyId", "EmployeeNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_employee_user",
                schema: "identity",
                table: "gulierp_employee",
                column: "UserId",
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_plant_company_default",
                schema: "identity",
                table: "gulierp_plant",
                column: "CompanyId",
                unique: true,
                filter: "\"IsDefault\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gulierp_employee",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "ux_gulierp_plant_company_default",
                schema: "identity",
                table: "gulierp_plant");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                schema: "identity",
                table: "gulierp_plant");
        }
    }
}
