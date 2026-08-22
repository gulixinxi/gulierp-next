using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Sales.Infrastructure.Migrations
{
    public partial class SALES001_RealSalesOrderVerticalSlice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "sales");

            migrationBuilder.CreateTable(
                name: "gulierp_sales_order",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    OrderNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_gulierp_sales_order", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gulierp_sales_order_line",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    SalesOrderId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_gulierp_sales_order_line", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gulierp_sales_order_line_gulierp_sales_order_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "gulierp_sales_order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_sales_order_customerid",
                schema: "sales",
                table: "gulierp_sales_order",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_sales_order_scope",
                schema: "sales",
                table: "gulierp_sales_order",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_sales_order_scope_orderno",
                schema: "sales",
                table: "gulierp_sales_order",
                columns: new[] { "TenantId", "CompanyId", "OrderNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_sales_order_line_itemid",
                schema: "sales",
                table: "gulierp_sales_order_line",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_sales_order_line_orderid",
                schema: "sales",
                table: "gulierp_sales_order_line",
                column: "SalesOrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "gulierp_sales_order_line", schema: "sales");
            migrationBuilder.DropTable(name: "gulierp_sales_order", schema: "sales");
        }
    }
}
