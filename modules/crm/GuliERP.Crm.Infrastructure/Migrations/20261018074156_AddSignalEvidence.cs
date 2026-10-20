using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvidenceType",
                schema: "crm",
                table: "signals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QueryParams",
                schema: "crm",
                table: "signals",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawContent",
                schema: "crm",
                table: "signals",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                schema: "crm",
                table: "signals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VerifiedAt",
                schema: "crm",
                table: "signals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VerifiedBy",
                schema: "crm",
                table: "signals",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvidenceType",
                schema: "crm",
                table: "signals");

            migrationBuilder.DropColumn(
                name: "QueryParams",
                schema: "crm",
                table: "signals");

            migrationBuilder.DropColumn(
                name: "RawContent",
                schema: "crm",
                table: "signals");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                schema: "crm",
                table: "signals");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                schema: "crm",
                table: "signals");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                schema: "crm",
                table: "signals");
        }
    }
}
