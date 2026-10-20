using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Sales.Infrastructure.Migrations
{
    /// <summary>
    /// B3.6-A — SalesOrder V1 Header / Audit / Snapshot / UoM Contract Reconstruction.
    /// Goal ID: GULIERP_TRANSACTION_PHASE_B3_6_A_GOAL_01.
    ///
    /// Additive only. New columns are nullable; no data backfill required.
    /// Per V1 Sales §2 + §9 + §11, the columns are:
    ///   sales.gulierp_sales_order
    ///     + WarehouseId              (bigint, nullable; P0 OPTIONAL)
    ///     + DeliveryAddressSnapshot  (varchar(500), nullable; P0 DERIVED, captured at Confirm)
    ///     + ConfirmedAt              (timestamptz, nullable; P0 audit)
    ///     + ConfirmedBy              (bigint, nullable; P0 audit)
    ///     + CancelledAt              (timestamptz, nullable; P0 audit)
    ///     + CancelledBy              (bigint, nullable; P0 audit)
    ///     + CancellationReason       (varchar(500), nullable; required when Cancelled from Confirmed)
    ///   sales.gulierp_sales_order_line
    ///     + SpecificationSnapshot    (varchar(500), nullable; P0 DERIVED, captured at line create / Item change)
    ///
    /// Index:
    ///   + ix_gulierp_sales_order_warehouseid on (WarehouseId) — for warehouse-scoped lookups.
    /// </summary>
    public partial class SALES002_SalesOrderV1HeaderAuditSnapshotUom : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WarehouseId",
                schema: "sales",
                table: "gulierp_sales_order",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddressSnapshot",
                schema: "sales",
                table: "gulierp_sales_order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                schema: "sales",
                table: "gulierp_sales_order",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ConfirmedBy",
                schema: "sales",
                table: "gulierp_sales_order",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                schema: "sales",
                table: "gulierp_sales_order",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CancelledBy",
                schema: "sales",
                table: "gulierp_sales_order",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "sales",
                table: "gulierp_sales_order",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationSnapshot",
                schema: "sales",
                table: "gulierp_sales_order_line",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_sales_order_warehouseid",
                schema: "sales",
                table: "gulierp_sales_order",
                column: "WarehouseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_gulierp_sales_order_warehouseid",
                schema: "sales",
                table: "gulierp_sales_order");

            migrationBuilder.DropColumn(
                name: "SpecificationSnapshot",
                schema: "sales",
                table: "gulierp_sales_order_line");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "sales",
                table: "gulierp_sales_order");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                schema: "sales",
                table: "gulierp_sales_order");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "sales",
                table: "gulierp_sales_order");

            migrationBuilder.DropColumn(
                name: "ConfirmedBy",
                schema: "sales",
                table: "gulierp_sales_order");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                schema: "sales",
                table: "gulierp_sales_order");

            migrationBuilder.DropColumn(
                name: "DeliveryAddressSnapshot",
                schema: "sales",
                table: "gulierp_sales_order");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                schema: "sales",
                table: "gulierp_sales_order");
        }
    }
}
