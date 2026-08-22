using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// Plant — logistics organizational unit. A physical location where
/// products are produced, stored, or distributed. Per G2-003A-R2
/// DEC-ID-017: Plant is a first-class entity, NOT a subtype of
/// <see cref="OrganizationUnit"/>. 1 Company → N Plant (DEC-ID-018).
///
/// <para>
/// Plant is a SEPARATE dimension from <see cref="OrganizationUnit"/>.
/// Plant is logistics / production; OrganizationUnit is HR / team tree.
/// A User can simultaneously be a member of both. (DEC-ID-019.)
/// </para>
///
/// <para>
/// The <c>CalendarCode</c> field is reserved for the future V1.5+
/// PlantCalendar (working days, shifts, holidays). V1 carries the
/// reference but does not enforce a calendar.
/// </para>
/// </summary>
public sealed class Plant : ICompanyScoped
{
    public long Id { get; set; }

    public long TenantId { get; set; }
    public long CompanyId { get; set; }

    /// <summary>Nullable self-FK for sub-plant / sub-factory tree.</summary>
    public long? ParentPlantId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Company.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code (e.g. "CN", "US").</summary>
    public string CountryCode { get; set; } = "CN";

    /// <summary>IANA timezone (e.g. "Asia/Shanghai").</summary>
    public string Timezone { get; set; } = "UTC";

    /// <summary>Reference to the future V1.5+ PlantCalendar.</summary>
    public string? CalendarCode { get; set; }

    /// <summary>
    /// Default factory/plant for the Company. In the simple single-factory
    /// scenario this is created automatically and ordinary users never need
    /// to choose a plant.
    /// </summary>
    public bool IsDefault { get; set; }

    public PlantStatus Status { get; set; } = PlantStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
