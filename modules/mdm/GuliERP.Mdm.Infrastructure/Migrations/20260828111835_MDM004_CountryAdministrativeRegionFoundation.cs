using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MDM004_CountryAdministrativeRegionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gulierp_administrative_region",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    CountryCode = table.Column<string>(type: "character(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShortName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    RegionType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_administrative_region", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gulierp_administrative_region_gulierp_administrative_region~",
                        column: x => x.ParentId,
                        principalSchema: "mdm",
                        principalTable: "gulierp_administrative_region",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gulierp_country",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Alpha3Code = table.Column<string>(type: "character(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_country", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_region_country_level",
                schema: "mdm",
                table: "gulierp_administrative_region",
                columns: new[] { "CountryCode", "Level" });

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_region_parent",
                schema: "mdm",
                table: "gulierp_administrative_region",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_region_country_code",
                schema: "mdm",
                table: "gulierp_administrative_region",
                columns: new[] { "CountryCode", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_country_alpha3",
                schema: "mdm",
                table: "gulierp_country",
                column: "Alpha3Code",
                filter: "\"Alpha3Code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_country_code",
                schema: "mdm",
                table: "gulierp_country",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gulierp_administrative_region",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "gulierp_country",
                schema: "mdm");
        }
    }
}
