using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MDM005_BusinessPartnerPostalAddressFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AdministrativeRegionId",
                schema: "mdm",
                table: "gulierp_business_partner",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MnemonicCode",
                schema: "mdm",
                table: "gulierp_business_partner",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionCodeSnapshot",
                schema: "mdm",
                table: "gulierp_business_partner",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegionNameSnapshot",
                schema: "mdm",
                table: "gulierp_business_partner",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_business_partner_regionid",
                schema: "mdm",
                table: "gulierp_business_partner",
                column: "AdministrativeRegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_gulierp_business_partner_administrative_region",
                schema: "mdm",
                table: "gulierp_business_partner",
                column: "AdministrativeRegionId",
                principalSchema: "mdm",
                principalTable: "gulierp_administrative_region",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gulierp_business_partner_administrative_region",
                schema: "mdm",
                table: "gulierp_business_partner");

            migrationBuilder.DropIndex(
                name: "ix_gulierp_business_partner_regionid",
                schema: "mdm",
                table: "gulierp_business_partner");

            migrationBuilder.DropColumn(
                name: "AdministrativeRegionId",
                schema: "mdm",
                table: "gulierp_business_partner");

            migrationBuilder.DropColumn(
                name: "MnemonicCode",
                schema: "mdm",
                table: "gulierp_business_partner");

            migrationBuilder.DropColumn(
                name: "RegionCodeSnapshot",
                schema: "mdm",
                table: "gulierp_business_partner");

            migrationBuilder.DropColumn(
                name: "RegionNameSnapshot",
                schema: "mdm",
                table: "gulierp_business_partner");
        }
    }
}
