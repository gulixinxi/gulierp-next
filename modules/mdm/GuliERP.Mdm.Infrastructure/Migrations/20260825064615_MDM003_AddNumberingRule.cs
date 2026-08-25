using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MDM003_AddNumberingRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gulierp_numbering_rule",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DatePattern = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SequenceLength = table.Column<int>(type: "integer", nullable: false),
                    ResetMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_numbering_rule", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_numbering_rule_status",
                schema: "mdm",
                table: "gulierp_numbering_rule",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_gulierp_numbering_rule_tenant_company",
                schema: "mdm",
                table: "gulierp_numbering_rule",
                columns: new[] { "TenantId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_numbering_rule_scope_document_type",
                schema: "mdm",
                table: "gulierp_numbering_rule",
                columns: new[] { "TenantId", "CompanyId", "DocumentType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gulierp_numbering_rule",
                schema: "mdm");
        }
    }
}
