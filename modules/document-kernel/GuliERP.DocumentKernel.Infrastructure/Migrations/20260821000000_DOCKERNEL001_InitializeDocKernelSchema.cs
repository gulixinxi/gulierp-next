using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.DocumentKernel.Infrastructure.Migrations
{
    /// <summary>
    /// DOC-KERNEL-001 initial schema migration. Creates the
    /// <c>doc_kernel</c> schema + 2 V1 tables (counter +
    /// idempotency dedup) + indexes. The migration is purely
    /// additive on the <c>identity.gulierp_hilo_sequence</c>
    /// HiLo sequence (created by the Identity IDGEN001
    /// migration; no separate sequence is created here).
    /// </summary>
    public partial class DOCKERNEL001_InitializeDocKernelSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. doc_kernel schema
            migrationBuilder.EnsureSchema(name: "doc_kernel");

            // 2. document_number_counter (atomic counter)
            migrationBuilder.CreateTable(
                name: "document_number_counter",
                schema: "doc_kernel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false),
                    LastGeneratedDocumentNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_number_counter", x => x.Id);
                });

            migrationBuilder.Sql(
                "ALTER TABLE doc_kernel.document_number_counter ALTER COLUMN \"Id\" SET DEFAULT nextval('identity.gulierp_hilo_sequence');");

            // The single point of atomicity. The upsert pattern in
            // the service relies on this unique constraint to
            // serialize concurrent GenerateAsync calls.
            migrationBuilder.CreateIndex(
                name: "ux_doc_number_counter_scope",
                schema: "doc_kernel",
                table: "document_number_counter",
                columns: new[] { "TenantId", "CompanyId", "DocumentType", "PeriodKey" },
                unique: true);

            // 3. document_number_idempotency (dedup)
            migrationBuilder.CreateTable(
                name: "document_number_idempotency",
                schema: "doc_kernel",
                columns: table => new
                {
                    IdempotencyKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    PeriodKey = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    GeneratedDocumentNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GeneratedBy = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_number_idempotency", x => x.IdempotencyKey);
                });

            migrationBuilder.CreateIndex(
                name: "ix_doc_number_idempotency_lookup",
                schema: "doc_kernel",
                table: "document_number_idempotency",
                columns: new[] { "TenantId", "DocumentType", "IdempotencyKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "document_number_idempotency", schema: "doc_kernel");
            migrationBuilder.DropTable(name: "document_number_counter", schema: "doc_kernel");
        }
    }
}
