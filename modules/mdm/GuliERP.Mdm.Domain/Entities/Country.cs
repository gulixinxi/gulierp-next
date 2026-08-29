using GuliERP.Foundation.Kernel;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2
/// (2026-08-28). Country reference data. Carries the ISO 3166-1
/// alpha-2 Code as the canonical business identifier; the
/// EnglishName / localized name fields are sourced from the
/// Unicode CLDR territory database (open source, see
/// <c>docs/verification/ONLYIT_MATURE_ERP_REFERENCE_AUDIT_001.md</c>
/// and the per-Wave-2 Country manifest for license / source).
///
/// <para>
/// <b>Scope</b>: Country is a system-level (NOT Tenant-scoped)
/// reference entity. The Code is globally unique. The V1
/// implementation exposes only read APIs and a one-time
/// <c>EnsureSeedAsync</c> for the operator-side importer; the
/// V1 runtime does NOT allow API-level create/update/delete of
/// countries. Localization fields are pre-baked at seed time.
/// </para>
///
/// <para>
/// Per brief §十五, V1 minimal fields are:
/// <c>Code</c>, <c>Name</c>, <c>EnglishName</c>, <c>IsActive</c>,
/// <c>SortOrder</c>. <c>Alpha3Code</c> is allowed if a reliable
/// dataset supplies it. The brief explicitly forbids
/// field-bloat.
/// </para>
/// </summary>
public sealed class Country : IMultiTenant
{
    /// <summary>
    /// Tenant-scope marker. Per the global-reference-data
    /// exception (countries are system-scoped, NOT tenant-scoped),
    /// the column is nullable + carries the well-known sentinel
    /// value 0 (matching the V1 <c>gulierp_hilo_sequence</c> shared
    /// PK space). The IMultiTenant.TenantId property returns
    /// this sentinel so that the global global query filter
    /// (if ever enabled) does not exclude Country rows.
    /// </summary>
    public long Id { get; set; }

    public long TenantId => 0;

    /// <summary>ISO 3166-1 alpha-2 (e.g. CN, US, JP). Unique, immutable in V1.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Optional ISO 3166-1 alpha-3 (e.g. CHN, USA, JPN). NULL if
    /// the seed dataset does not supply it. V1 does not require
    /// this field.
    /// </summary>
    public string? Alpha3Code { get; set; }

    /// <summary>Display name in the current UI locale (zh-Hans in V1). Sourced from CLDR.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Display name in English (en). Sourced from CLDR.</summary>
    public string EnglishName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>UI sort order. Lower = earlier. Sourced from dataset convention.</summary>
    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
