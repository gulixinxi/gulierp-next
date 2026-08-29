using GuliERP.Foundation.Kernel;
using GuliERP.Mdm.Domain.Enums;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// BusinessPartner — tenant-scoped counterparty master (per MDM-002
/// scope, replacing the prior out-of-scope note in MDM-000). A
/// single <see cref="BusinessPartner"/> row can serve as Customer,
/// Supplier, or Both via the <see cref="BusinessPartnerRole"/>
/// bit-flag. The role is a single denormalized integer column —
/// split tables (Customer / Supplier / Both per partner) are
/// DEFERRED.
///
/// <para>
/// Identity V1 (per G2-003A DEC-ID-013) requires the Application
/// service to apply <c>Where(e =&gt; e.TenantId == currentTenant.Id)</c>
/// for every read / write path; the EF Core global query filter
/// placeholder is the structural mirror per DEC-ID-013 / G2-003A
/// §18 line 636.
/// </para>
///
/// <para>
/// <b>V1 explicitly does NOT carry</b>: multi-row address book
/// (single Address / ContactPerson / Phone / Email / TaxNumber set
/// per row — multi-address / multi-contact is V2+), bank account
/// table (V2+), credit limit (V2+), price list (V2+), and any
/// financial-subject linkage (Finance module owns that).
/// </para>
///
/// <para>
/// <b>GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 3
/// (2026-08-28) PostalAddress + MnemonicCode additive
/// extension</b>:
/// <list type="bullet">
///   <item><see cref="AdministrativeRegionId"/> — nullable FK to
///         <c>mdm.gulierp_administrative_region</c>. V1 supports
///         binding a BusinessPartner's primary address to an
///         AdministrativeRegion reference (CN dataset: 0 rows in
///         repo pending Operator MCA import; international
///         datasets not yet supplied; <c>NULL</c> is the supported
///         legacy / international free-text fallback). FK is
///         <c>Restrict</c> (no cascade).</item>
///   <item><see cref="RegionCodeSnapshot"/> + <see cref="RegionNameSnapshot"/>
///         — server-derived cache of the bound Region's Code +
///         Name, captured at write time. These snapshots are
///         written by the server from the reference data; the SPA
///         MUST NOT supply them. Snapshots are the contract for
///         "what address text was in effect when this row was
///         last edited" and are kept even when the underlying
///         Region is later deactivated (Region.IsActive = false)
///         so historical data remains readable.</item>
///   <item><see cref="MnemonicCode"/> — short hand-typed code for
///         fast lookup (no auto pinyin / NLP in V1). Nullable,
///         not unique, max length 40, case-insensitive search.</item>
/// </list>
///
/// <b>Legacy text fields (Region / City / AddressLine1 /
/// AddressLine2 / PostalCode / CountryCode) are preserved as-is</b>.
/// Existing BusinessPartner rows whose <c>AdministrativeRegionId
/// IS NULL</c> continue to read / write their legacy text fields
/// unchanged. The Wave 3 migration is ADDITIVE only — it does not
/// backfill <c>AdministrativeRegionId</c> from existing
/// <c>Region</c> text, and it does not normalize or rewrite
/// existing address values. Per brief §十, this is a hard
/// regression test: editing Phone alone must not erase legacy
/// address text.
/// </para>
/// </summary>
public sealed class BusinessPartner : IMultiTenant
{
    public long Id { get; set; }
    public long TenantId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Tenant.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Display short name (≤40 chars). Nullable.</summary>
    public string? ShortName { get; set; }

    /// <summary>Bit-flag: Customer | Supplier | Both. Default Both.</summary>
    public BusinessPartnerRole Role { get; set; } = BusinessPartnerRole.Both;

    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // Address (single; multi-address is V2+)
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }  // ISO 3166-1 alpha-2

    /// <summary>Tax registration number. Nullable for non-VAT entities.</summary>
    public string? TaxNumber { get; set; }

    /// <summary>
    /// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 3
    /// (2026-08-28). Hand-typed mnemonic / short lookup code
    /// (max 40, nullable, NOT unique). V1 has no auto pinyin
    /// or NLP fill. Search is case-insensitive substring
    /// against the typed value.
    /// </summary>
    public string? MnemonicCode { get; set; }

    /// <summary>
    /// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 3
    /// (2026-08-28). Nullable FK to
    /// <c>mdm.gulierp_administrative_region.Id</c>. NULL is the
    /// supported legacy / international fallback. FK is
    /// <c>Restrict</c> (no cascade) — Regions cannot be hard-
    /// deleted while any BusinessPartner references them; use
    /// <c>IsActive = false</c> instead.
    /// </summary>
    public long? AdministrativeRegionId { get; set; }

    /// <summary>
    /// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 3.
    /// Server-derived cache of the bound Region's <c>Code</c>
    /// captured at write time. The SPA MUST NOT supply this
    /// field; the service recomputes it from the reference
    /// data on every write. Survives Region deactivation.
    /// </summary>
    public string? RegionCodeSnapshot { get; set; }

    /// <summary>
    /// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 3.
    /// Server-derived cache of the bound Region's <c>Name</c>
    /// captured at write time. Same contract as
    /// <see cref="RegionCodeSnapshot"/>.
    /// </summary>
    public string? RegionNameSnapshot { get; set; }

    public MasterDataStatus Status { get; set; } = MasterDataStatus.Active;

    /// <summary>Optional free-text description / display name.</summary>
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }

    /// <summary>EF Core optimistic concurrency token.</summary>
    public int ConcurrencyVersion { get; set; }
}
