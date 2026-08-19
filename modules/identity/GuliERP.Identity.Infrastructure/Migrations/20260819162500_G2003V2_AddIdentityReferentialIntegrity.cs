using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class G2003V2_AddIdentityReferentialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_gulierp_user_role_assignment_CompanyId",
                schema: "identity",
                table: "gulierp_user_role_assignment",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_user_role_assignment_RoleId",
                schema: "identity",
                table: "gulierp_user_role_assignment",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_user_role_assignment_UserId",
                schema: "identity",
                table: "gulierp_user_role_assignment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_user_organization_membership_CompanyId",
                schema: "identity",
                table: "gulierp_user_organization_membership",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_user_organization_membership_OrganizationUnitId",
                schema: "identity",
                table: "gulierp_user_organization_membership",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_user_organization_membership_UserId",
                schema: "identity",
                table: "gulierp_user_organization_membership",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_user_company_membership_CompanyId",
                schema: "identity",
                table: "gulierp_user_company_membership",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_user_company_membership_UserId",
                schema: "identity",
                table: "gulierp_user_company_membership",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_plant_TenantId",
                schema: "identity",
                table: "gulierp_plant",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_gulierp_organization_unit_TenantId",
                schema: "identity",
                table: "gulierp_organization_unit",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoles_gulierp_tenant_TenantId",
                schema: "identity",
                table: "AspNetRoles",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "gulierp_tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_gulierp_tenant_TenantId",
                schema: "identity",
                table: "AspNetUsers",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "gulierp_tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_company_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_company",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "gulierp_tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_organization_unit_gulierp_company_CompanyId",
                schema: "identity",
                table: "gulierp_organization_unit",
                column: "CompanyId",
                principalSchema: "identity",
                principalTable: "gulierp_company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_organization_unit_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_organization_unit",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "gulierp_tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_plant_gulierp_company_CompanyId",
                schema: "identity",
                table: "gulierp_plant",
                column: "CompanyId",
                principalSchema: "identity",
                principalTable: "gulierp_company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_plant_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_plant",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "gulierp_tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_company_membership_AspNetUsers_UserId",
                schema: "identity",
                table: "gulierp_user_company_membership",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_company_membership_gulierp_company_CompanyId",
                schema: "identity",
                table: "gulierp_user_company_membership",
                column: "CompanyId",
                principalSchema: "identity",
                principalTable: "gulierp_company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_company_membership_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_user_company_membership",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "gulierp_tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_organization_membership_AspNetUsers_UserId",
                schema: "identity",
                table: "gulierp_user_organization_membership",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_organization_membership_gulierp_company_Compan~",
                schema: "identity",
                table: "gulierp_user_organization_membership",
                column: "CompanyId",
                principalSchema: "identity",
                principalTable: "gulierp_company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_organization_membership_gulierp_organization_u~",
                schema: "identity",
                table: "gulierp_user_organization_membership",
                column: "OrganizationUnitId",
                principalSchema: "identity",
                principalTable: "gulierp_organization_unit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_organization_membership_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_user_organization_membership",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "gulierp_tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_role_assignment_AspNetRoles_RoleId",
                schema: "identity",
                table: "gulierp_user_role_assignment",
                column: "RoleId",
                principalSchema: "identity",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_role_assignment_AspNetUsers_UserId",
                schema: "identity",
                table: "gulierp_user_role_assignment",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_role_assignment_gulierp_company_CompanyId",
                schema: "identity",
                table: "gulierp_user_role_assignment",
                column: "CompanyId",
                principalSchema: "identity",
                principalTable: "gulierp_company",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_user_role_assignment_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_user_role_assignment",
                column: "TenantId",
                principalSchema: "identity",
                principalTable: "gulierp_tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_gulierp_tenant_TenantId",
                schema: "identity",
                table: "AspNetRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_gulierp_tenant_TenantId",
                schema: "identity",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_company_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_company");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_organization_unit_gulierp_company_CompanyId",
                schema: "identity",
                table: "gulierp_organization_unit");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_organization_unit_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_organization_unit");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_plant_gulierp_company_CompanyId",
                schema: "identity",
                table: "gulierp_plant");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_plant_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_plant");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_company_membership_AspNetUsers_UserId",
                schema: "identity",
                table: "gulierp_user_company_membership");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_company_membership_gulierp_company_CompanyId",
                schema: "identity",
                table: "gulierp_user_company_membership");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_company_membership_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_user_company_membership");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_organization_membership_AspNetUsers_UserId",
                schema: "identity",
                table: "gulierp_user_organization_membership");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_organization_membership_gulierp_company_Compan~",
                schema: "identity",
                table: "gulierp_user_organization_membership");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_organization_membership_gulierp_organization_u~",
                schema: "identity",
                table: "gulierp_user_organization_membership");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_organization_membership_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_user_organization_membership");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_role_assignment_AspNetRoles_RoleId",
                schema: "identity",
                table: "gulierp_user_role_assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_role_assignment_AspNetUsers_UserId",
                schema: "identity",
                table: "gulierp_user_role_assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_role_assignment_gulierp_company_CompanyId",
                schema: "identity",
                table: "gulierp_user_role_assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_user_role_assignment_gulierp_tenant_TenantId",
                schema: "identity",
                table: "gulierp_user_role_assignment");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_user_role_assignment_CompanyId",
                schema: "identity",
                table: "gulierp_user_role_assignment");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_user_role_assignment_RoleId",
                schema: "identity",
                table: "gulierp_user_role_assignment");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_user_role_assignment_UserId",
                schema: "identity",
                table: "gulierp_user_role_assignment");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_user_organization_membership_CompanyId",
                schema: "identity",
                table: "gulierp_user_organization_membership");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_user_organization_membership_OrganizationUnitId",
                schema: "identity",
                table: "gulierp_user_organization_membership");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_user_organization_membership_UserId",
                schema: "identity",
                table: "gulierp_user_organization_membership");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_user_company_membership_CompanyId",
                schema: "identity",
                table: "gulierp_user_company_membership");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_user_company_membership_UserId",
                schema: "identity",
                table: "gulierp_user_company_membership");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_plant_TenantId",
                schema: "identity",
                table: "gulierp_plant");

            migrationBuilder.DropIndex(
                name: "IX_gulierp_organization_unit_TenantId",
                schema: "identity",
                table: "gulierp_organization_unit");
        }
    }
}
