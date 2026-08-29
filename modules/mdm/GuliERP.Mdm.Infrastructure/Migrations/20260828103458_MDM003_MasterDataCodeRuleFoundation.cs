using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MDM003_MasterDataCodeRuleFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gulierp_master_data_code_rule",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Prefix = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Separator = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    SequenceLength = table.Column<int>(type: "integer", nullable: false),
                    StartValue = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_master_data_code_rule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gulierp_master_data_code_sequence_state",
                schema: "mdm",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    RuleId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: true),
                    CurrentValue = table.Column<long>(type: "bigint", nullable: false),
                    LastGeneratedCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gulierp_master_data_code_sequence_state", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gulierp_master_data_code_sequence_state_gulierp_master_data~",
                        column: x => x.RuleId,
                        principalSchema: "mdm",
                        principalTable: "gulierp_master_data_code_rule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_master_code_rule_scope",
                schema: "mdm",
                table: "gulierp_master_data_code_rule",
                columns: new[] { "TenantId", "CompanyId", "WarehouseId", "EntityType", "SubType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_gulierp_master_code_sequence_rule",
                schema: "mdm",
                table: "gulierp_master_data_code_sequence_state",
                column: "RuleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gulierp_master_data_code_sequence_state",
                schema: "mdm");

            migrationBuilder.DropTable(
                name: "gulierp_master_data_code_rule",
                schema: "mdm");
        }
    }
}
