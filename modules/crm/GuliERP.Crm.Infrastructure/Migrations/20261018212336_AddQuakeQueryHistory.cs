using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuliERP.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuakeQueryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quake_query_history",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QueryText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    QueryHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    ImportedCount = table.Column<int>(type: "integer", nullable: false),
                    QueriedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quake_query_history", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quake_query_history_QueriedAt",
                schema: "crm",
                table: "quake_query_history",
                column: "QueriedAt");

            migrationBuilder.CreateIndex(
                name: "IX_quake_query_history_QueryHash",
                schema: "crm",
                table: "quake_query_history",
                column: "QueryHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quake_query_history",
                schema: "crm");
        }
    }
}
