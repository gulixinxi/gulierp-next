using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GuliERP.Crm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitCrmSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crm");

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreditCode = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    ShortName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WebsiteDomain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<int>(type: "integer", nullable: false),
                    District = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Industry = table.Column<int>(type: "integer", nullable: false),
                    Scale = table.Column<int>(type: "integer", nullable: true),
                    Revenue = table.Column<int>(type: "integer", nullable: true),
                    HasWebsite = table.Column<bool>(type: "boolean", nullable: false),
                    WebsiteStatus = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OwnerId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    IsRatingSuggested = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "import_export_logs",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    OperatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    SkippedCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SourceFile = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureDetails = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_export_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "leads",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreditCode = table.Column<string>(type: "text", nullable: true),
                    WebsiteDomain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactPosition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Wechat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    City = table.Column<int>(type: "integer", nullable: true),
                    District = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Industry = table.Column<int>(type: "integer", nullable: true),
                    Scale = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CollectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExternalKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AiScore = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    IsRatingSuggested = table.Column<bool>(type: "boolean", nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConvertedToCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    ConvertedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConvertedBy = table.Column<long>(type: "bigint", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "contacts",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CrmCompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DecisionRole = table.Column<int>(type: "integer", nullable: true),
                    Mobile = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Wechat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsKeyPerson = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contacts_companies_CrmCompanyId",
                        column: x => x.CrmCompanyId,
                        principalSchema: "crm",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deals",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    DealNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CrmCompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    ExpectedCloseMonth = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    WinRate = table.Column<int>(type: "integer", nullable: true),
                    OwnerId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LostReason = table.Column<int>(type: "integer", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deals_companies_CrmCompanyId",
                        column: x => x.CrmCompanyId,
                        principalSchema: "crm",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    CrmCompanyId = table.Column<long>(type: "bigint", nullable: false),
                    DealId = table.Column<long>(type: "bigint", nullable: true),
                    FollowType = table.Column<int>(type: "integer", nullable: false),
                    FollowAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Attitude = table.Column<int>(type: "integer", nullable: true),
                    NextAction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NextActionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OwnerId = table.Column<long>(type: "bigint", nullable: true),
                    Attachments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_activities_companies_CrmCompanyId",
                        column: x => x.CrmCompanyId,
                        principalSchema: "crm",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_activities_deals_DealId",
                        column: x => x.DealId,
                        principalSchema: "crm",
                        principalTable: "deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activities_CompanyId",
                schema: "crm",
                table: "activities",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_activities_CrmCompanyId",
                schema: "crm",
                table: "activities",
                column: "CrmCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_activities_DealId",
                schema: "crm",
                table: "activities",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_activities_NextActionDate",
                schema: "crm",
                table: "activities",
                column: "NextActionDate");

            migrationBuilder.CreateIndex(
                name: "IX_activities_OwnerId",
                schema: "crm",
                table: "activities",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_activities_TenantId",
                schema: "crm",
                table: "activities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_companies_CompanyId",
                schema: "crm",
                table: "companies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_companies_CompanyNo",
                schema: "crm",
                table: "companies",
                column: "CompanyNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companies_CreditCode",
                schema: "crm",
                table: "companies",
                column: "CreditCode");

            migrationBuilder.CreateIndex(
                name: "IX_companies_ExternalKey",
                schema: "crm",
                table: "companies",
                column: "ExternalKey");

            migrationBuilder.CreateIndex(
                name: "IX_companies_OwnerId",
                schema: "crm",
                table: "companies",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_companies_Status",
                schema: "crm",
                table: "companies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_companies_TenantId",
                schema: "crm",
                table: "companies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_companies_WebsiteDomain",
                schema: "crm",
                table: "companies",
                column: "WebsiteDomain");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_CompanyId",
                schema: "crm",
                table: "contacts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_CrmCompanyId",
                schema: "crm",
                table: "contacts",
                column: "CrmCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_Mobile",
                schema: "crm",
                table: "contacts",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_contacts_TenantId",
                schema: "crm",
                table: "contacts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_deals_CompanyId",
                schema: "crm",
                table: "deals",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_deals_CrmCompanyId",
                schema: "crm",
                table: "deals",
                column: "CrmCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_deals_DealNo",
                schema: "crm",
                table: "deals",
                column: "DealNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deals_OwnerId",
                schema: "crm",
                table: "deals",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_deals_Stage",
                schema: "crm",
                table: "deals",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_deals_Status",
                schema: "crm",
                table: "deals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_deals_TenantId",
                schema: "crm",
                table: "deals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_import_export_logs_CompanyId",
                schema: "crm",
                table: "import_export_logs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_import_export_logs_OperatedAt",
                schema: "crm",
                table: "import_export_logs",
                column: "OperatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_import_export_logs_OperationType",
                schema: "crm",
                table: "import_export_logs",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_import_export_logs_Status",
                schema: "crm",
                table: "import_export_logs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_import_export_logs_TenantId",
                schema: "crm",
                table: "import_export_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_CompanyId",
                schema: "crm",
                table: "leads",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_ConvertedToCompanyId",
                schema: "crm",
                table: "leads",
                column: "ConvertedToCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_Email",
                schema: "crm",
                table: "leads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_leads_ExternalKey",
                schema: "crm",
                table: "leads",
                column: "ExternalKey");

            migrationBuilder.CreateIndex(
                name: "IX_leads_Mobile",
                schema: "crm",
                table: "leads",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_leads_OwnerId",
                schema: "crm",
                table: "leads",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_Status",
                schema: "crm",
                table: "leads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_leads_TenantId",
                schema: "crm",
                table: "leads",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_leads_WebsiteDomain",
                schema: "crm",
                table: "leads",
                column: "WebsiteDomain");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activities",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "contacts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "import_export_logs",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "leads",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "deals",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "crm");
        }
    }
}
