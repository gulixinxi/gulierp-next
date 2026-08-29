using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <summary>
    /// GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30) —
    /// Additive column for the Item-level MnemonicCode field.
    /// Per Reuse Wave brief §二十八: nullable, non-unique, max 40
    /// chars, hand-typed by the operator (no auto-generation, no
    /// pinyin library). The migration is ADDITIVE only — no DROP,
    /// no destructive backfill, no data rewrite.
    /// </summary>
    public partial class MDM006_ItemMnemonicCode_Regen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MnemonicCode",
                schema: "mdm",
                table: "gulierp_item",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            // Non-unique index to accelerate keyword search
            // that hits MnemonicCode. The index is intentionally
            // non-unique (the brief requires MnemonicCode to be
            // NOT unique).
            migrationBuilder.CreateIndex(
                name: "ix_gulierp_item_mnemoniccode",
                schema: "mdm",
                table: "gulierp_item",
                column: "MnemonicCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_gulierp_item_mnemoniccode",
                schema: "mdm",
                table: "gulierp_item");

            migrationBuilder.DropColumn(
                name: "MnemonicCode",
                schema: "mdm",
                table: "gulierp_item");
        }
    }
}
