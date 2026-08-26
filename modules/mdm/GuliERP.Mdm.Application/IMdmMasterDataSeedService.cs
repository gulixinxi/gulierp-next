using GuliERP.Foundation.Kernel;

namespace GuliERP.Mdm.Application;

/// <summary>
/// G3_MDM_MASTERDATA_V1_SEED service contract. Per
/// docs/governance/G3_MDM_MASTERDATA_V1_SEED_PLAN.md — seeds the
/// 25 V1 master-data rows (ItemCategory + Item + BP + Warehouse +
/// Location) from 5 JSON files in a directory.
///
/// <para>
/// <b>Idempotency</b>: per-item natural-key check via the existing
/// Application service (e.g., <see cref="IMdmService.ListItemCategoriesAsync"/>).
/// If the row already exists, the item is skipped. The DB unique
/// index is the atomicity backstop.
/// </para>
///
/// <para>
/// <b>Uom</b> is NOT seeded by this service — Uom is system-scoped
/// and already loaded by <c>MdmSeed.SeedAsync</c> (13 SAFE items).
/// The new service only resolves Uom code → Uom.Id for Item's
/// <c>BaseUomId</c> FK.
/// </para>
///
/// <para>
/// <b>Dependency order</b>:
/// <list type="number">
///   <item>ItemCategory (2-pass for self-FK tree)</item>
///   <item>BusinessPartner (no FK)</item>
///   <item>Warehouse (no FK)</item>
///   <item>Item (FK to Uom + ItemCategory)</item>
///   <item>Location (FK to Warehouse)</item>
/// </list>
/// </para>
/// </summary>
public interface IMdmMasterDataSeedService
{
    /// <summary>
    /// Seed all master-data JSON files in the given directory.
    /// </summary>
    /// <param name="seedPath">Directory containing the 5 *.json files
    /// (item-category.json, item.json, business-partner.json, warehouse.json, location.json).
    /// REQUIRED.</param>
    /// <param name="tenantId">Current tenant. REQUIRED (Uom is system-level but
    /// the rest of the entities are tenant-scoped).</param>
    /// <param name="companyId">Current company. REQUIRED (Warehouse / Location
    /// are ICompanyScoped).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<MasterDataSeedSummary> SeedAllFromPathAsync(
        string seedPath,
        long tenantId,
        long companyId,
        CancellationToken ct = default);
}

/// <summary>
/// Aggregated result of a masterdata seed run.
/// </summary>
public sealed record MasterDataSeedSummary(
    int TotalFilesScanned,
    int ItemCategoriesAttempted,
    int ItemCategoriesCreated,
    int ItemCategoriesFailed,
    int ItemsAttempted,
    int ItemsCreated,
    int ItemsFailed,
    int BusinessPartnersAttempted,
    int BusinessPartnersCreated,
    int BusinessPartnersFailed,
    int WarehousesAttempted,
    int WarehousesCreated,
    int WarehousesFailed,
    int LocationsAttempted,
    int LocationsCreated,
    int LocationsFailed,
    IReadOnlyList<string> UnknownFiles,
    IReadOnlyList<string> DuplicateCodes,
    IReadOnlyList<string> FailedFiles);
