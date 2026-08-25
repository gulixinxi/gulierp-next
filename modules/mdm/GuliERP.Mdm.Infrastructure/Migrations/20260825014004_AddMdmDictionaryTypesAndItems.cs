using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMdmDictionaryTypesAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gulierp_dictionary_type",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_dictionary_type", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gulierp_dictionary_item",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    DictionaryTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_dictionary_item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gulierp_dictionary_item_gulierp_dictionary_type_DictionaryT~",
                        column: x => x.DictionaryTypeId,
                        principalSchema: "mdm",
                        principalTable: "gulierp_dictionary_type",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_dictionary_item_sort_order",
                schema: "mdm",
                table: "gulierp_dictionary_item",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_dictionary_item_status",
                schema: "mdm",
                table: "gulierp_dictionary_item",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_dictionary_item_tenantid",
                schema: "mdm",
                table: "gulierp_dictionary_item",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_dictionary_item_typeid",
                schema: "mdm",
                table: "gulierp_dictionary_item",
                column: "DictionaryTypeId");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_dictionary_item_tenant_type_code",
                schema: "mdm",
                table: "gulierp_dictionary_item",
                columns: new[] { "TenantId", "DictionaryTypeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_dictionary_type_sort_order",
                schema: "mdm",
                table: "gulierp_dictionary_type",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_dictionary_type_status",
                schema: "mdm",
                table: "gulierp_dictionary_type",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_dictionary_type_tenantid",
                schema: "mdm",
                table: "gulierp_dictionary_type",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_dictionary_type_tenant_code",
                schema: "mdm",
                table: "gulierp_dictionary_type",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gulierp_dictionary_item",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "gulierp_dictionary_type",
                schema: "mdm");
        }
    }
}
