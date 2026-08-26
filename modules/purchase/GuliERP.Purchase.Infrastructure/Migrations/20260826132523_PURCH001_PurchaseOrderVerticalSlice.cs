using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Purchase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PURCH001_PurchaseOrderVerticalSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // G3-R2B: The "identity" schema and the "gulierp_hilo_sequence"
            // sequence are owned by the Identity IDGEN001 migration. The
            // Purchase module reuses the canonical sequence via UseHiLo
            // (see PurchaseOrderConfiguration) just like the Mdm and Sales
            // modules do. We do NOT recreate them here. See
            // MdmDbContext.HiLoSequenceSchema / MdmDbContext.HiLoSequenceName
            // for the same reasoning.
            migrationBuilder.EnsureSchema(
                name: "purchase");

            migrationBuilder.CreateTable(
                name: "gulierp_purchase_order",
                schema: "purchase",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    OrderNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: false),
                    SupplierCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SupplierNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TotalNetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalTaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_purchase_order", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gulierp_purchase_order_line",
                schema: "purchase",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseOrderId = table.Column<long>(type: "bigint", nullable: false),
                    LineNo = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UomId = table.Column<long>(type: "bigint", nullable: false),
                    UomCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UomNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_purchase_order_line", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gulierp_purchase_order_line_gulierp_purchase_order_Purchase~",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "purchase",
                        principalTable: "gulierp_purchase_order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_purchase_order_scope",
                schema: "purchase",
                table: "gulierp_purchase_order",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_purchase_order_supplierid",
                schema: "purchase",
                table: "gulierp_purchase_order",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_purchase_order_scope_orderno",
                schema: "purchase",
                table: "gulierp_purchase_order",
                columns: new[] { "TenantId", "CompanyId", "OrderNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_purchase_order_line_itemid",
                schema: "purchase",
                table: "gulierp_purchase_order_line",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_purchase_order_line_orderid",
                schema: "purchase",
                table: "gulierp_purchase_order_line",
                column: "PurchaseOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // G3-R2B: do NOT drop the canonical "identity"."gulierp_hilo_sequence"
            // sequence on rollback — it is owned by the Identity IDGEN001
            // migration and is shared by the Mdm, Sales, and Purchase modules.
            migrationBuilder.DropTable(
                name: "gulierp_purchase_order_line",
                schema: "purchase");

            migrationBuilder.DropTable(
                name: "gulierp_purchase_order",
                schema: "purchase");
        }
    }
}
