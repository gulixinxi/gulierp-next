using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <summary>
    /// GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30) —
    /// No-op migration marker. The actual schema change (add
    /// <c>mdm.gulierp_item.MnemonicCode</c> + non-unique index
    /// <c>ix_gulierp_item_mnemoniccode</c>) is owned by
    /// <c>MDM006_ItemMnemonicCode_Regen</c>; this file exists
    /// solely so the Wave-declaration commit carries a
    /// human-named migration on disk for traceability. Both
    /// migrations are part of the same logical Wave.
    /// </summary>
    public partial class MDM006_ItemMnemonicCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
