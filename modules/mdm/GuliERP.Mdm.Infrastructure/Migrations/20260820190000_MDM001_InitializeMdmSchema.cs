using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <summary>
    /// MDM-001 initial schema migration. Creates the <c>mdm</c>
    /// schema + 3 V1 master-data tables (<c>gulierp_uom</c>,
    /// <c>gulierp_item_category</c>, <c>gulierp_item</c>) +
    /// unique indexes + FKs. The migration is purely additive
    /// on the <c>identity.gulierp_hilo_sequence</c> HiLo sequence
    /// (created by the Identity IDGEN001 migration).
    /// </summary>
    public partial class MDM001_InitializeMdmSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------------------------------------------------------
            // 1. mdm schema
            // ---------------------------------------------------------
            migrationBuilder.EnsureSchema(
                name: "mdm");

            // ---------------------------------------------------------
            // 2. gulierp_uom (system master — no TenantId)
            // ---------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "gulierp_uom",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Dimension = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gulierp_uom", x => x.Id);
                });

            migrationBuilder.Sql(
                "ALTER TABLE mdm.gulierp_uom ALTER COLUMN \"Id\" SET DEFAULT nextval('identity.gulierp_hilo_sequence');");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_uom_code",
                schema: "mdm",
                table: "gulierp_uom",
                column: "Code",
                unique: true);

            // ---------------------------------------------------------
            // 3. gulierp_item_category (tenant-scoped, self-FK hierarchy)
            // ---------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "gulierp_item_category",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gulierp_item_category", x => x.Id);
                    table.ForeignKey(
                        name: "fk_gulierp_item_category_parent",
                        column: x => x.ParentId,
                        principalSchema: "mdm",
                        principalTable: "gulierp_item_category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                "ALTER TABLE mdm.gulierp_item_category ALTER COLUMN \"Id\" SET DEFAULT nextval('identity.gulierp_hilo_sequence');");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_item_category_parentid",
                schema: "mdm",
                table: "gulierp_item_category",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_item_category_tenant_code",
                schema: "mdm",
                table: "gulierp_item_category",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            // ---------------------------------------------------------
            // 4. gulierp_item (tenant-scoped, optional Category + required UOM)
            // ---------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "gulierp_item",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Specification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    BaseUomId = table.Column<long>(type: "bigint", nullable: false),
                    ItemNature = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gulierp_item", x => x.Id);
                    table.ForeignKey(
                        name: "fk_gulierp_item_category",
                        column: x => x.CategoryId,
                        principalSchema: "mdm",
                        principalTable: "gulierp_item_category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gulierp_item_base_uom",
                        column: x => x.BaseUomId,
                        principalSchema: "mdm",
                        principalTable: "gulierp_uom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                "ALTER TABLE mdm.gulierp_item ALTER COLUMN \"Id\" SET DEFAULT nextval('identity.gulierp_hilo_sequence');");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_item_categoryid",
                schema: "mdm",
                table: "gulierp_item",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_item_baseuomid",
                schema: "mdm",
                table: "gulierp_item",
                column: "BaseUomId");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_item_tenantid",
                schema: "mdm",
                table: "gulierp_item",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_item_tenant_code",
                schema: "mdm",
                table: "gulierp_item",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "gulierp_item", schema: "mdm");
            migrationBuilder.DropTable(name: "gulierp_item_category", schema: "mdm");
            migrationBuilder.DropTable(name: "gulierp_uom", schema: "mdm");
        }
    }
}
