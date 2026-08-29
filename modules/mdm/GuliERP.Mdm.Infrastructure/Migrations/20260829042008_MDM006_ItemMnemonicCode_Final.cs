using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuliERP.Mdm.Infrastructure.Migrations
{
    /// <summary>
    /// GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1_SCHEMA_AND_OPERATOR_CLOSURE
    /// (2026-08-30) — MDM006 hygiene.
    ///
    /// <para>
    /// This migration is now a NO-OP. Originally it was the second
    /// half of the Item.MnemonicCode rollout (the index
    /// <c>ix_gulierp_item_mnemoniccode</c>). It has been collapsed
    /// into
    /// <c>20260829041719_MDM006_ItemMnemonicCode_Regen</c>, which
    /// adds both the column and the index in a single Up. The
    /// previous Goal's separate "Final" + "marker" three-file
    /// structure polluted the migration history and triggered
    /// "relation already exists" errors when EF re-applied the
    /// pending migrations against a test DB that had the index
    /// out-of-band. This collapse fixes that.
    /// </para>
    ///
    /// <para>
    /// The Designer file is preserved with the post-state model
    /// (column + index) so the diff vs.
    /// <c>20260829041719_MDM006_ItemMnemonicCode_Regen</c>'s
    /// Designer is 0 — the noop Up/Down matches the zero-diff
    /// model. The migration is safely skipped at apply time, but
    /// the entry is still recorded in <c>__EFMigrationsHistory</c>
    /// so EF sees a consistent history.
    /// </para>
    /// </summary>
    public partial class MDM006_ItemMnemonicCode_Final : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Noop — see class doc. The column + index were added
            // by 20260829041719_MDM006_ItemMnemonicCode_Regen.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Noop — reverses nothing because the Up did nothing.
            // The real column + index Drop lives in the Regen
            // migration's Down.
        }
    }
}
