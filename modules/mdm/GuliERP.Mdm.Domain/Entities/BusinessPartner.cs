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
