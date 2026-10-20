using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuliERP.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "signals",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    LeadId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyRefId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Detail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EventAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HandledBy = table.Column<long>(type: "bigint", nullable: true),
                    HandledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HandleNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_signals_CollectedAt",
                schema: "crm",
                table: "signals",
                column: "CollectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_signals_CompanyId",
                schema: "crm",
                table: "signals",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_signals_CompanyRefId",
                schema: "crm",
                table: "signals",
                column: "CompanyRefId");

            migrationBuilder.CreateIndex(
                name: "IX_signals_LeadId",
                schema: "crm",
                table: "signals",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_signals_Status",
                schema: "crm",
                table: "signals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_signals_TenantId",
                schema: "crm",
                table: "signals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_signals_Type",
                schema: "crm",
                table: "signals",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "signals",
                schema: "crm");
        }
    }
}
