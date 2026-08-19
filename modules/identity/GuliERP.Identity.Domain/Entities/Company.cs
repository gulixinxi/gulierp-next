using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// Company — legal entity under a Tenant. Per G2-003A DEC-ID-002
/// a Company has its own books (COA, currency, tax, default accounts).
/// 1 Tenant → N Company; one Company may have a parent Company
/// (group / subsidiary tree).
///
/// <para>
/// <b>Not a Plant</b>. Per G2-003A-R2 DEC-ID-018: 1 Company → N Plant;
/// Plant is a logistics site, Company is a legal entity.
/// </para>
/// </summary>
public sealed class Company : ICompanyScoped
{
    public long Id { get; set; }

    public long TenantId { get; set; }
    public long CompanyId => Id;   // self-reference for ICompanyScoped

    /// <summary>Nullable self-FK for the group / subsidiary tree.</summary>
    public long? ParentCompanyId { get; set; }

    /// <summary>UPPER_SNAKE; unique within Tenant (DEC-ID-007).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Full registered legal name (for tax invoices).</summary>
    public string? LegalName { get; set; }

    /// <summary>VAT / EIN / 统一社会信用代码.</summary>
    public string? TaxId { get; set; }

    /// <summary>ISO 4217 3-letter code (e.g. "CNY", "USD").</summary>
    public string DefaultCurrency { get; set; } = "CNY";

    /// <summary>IANA timezone (e.g. "Asia/Shanghai").</summary>
    public string Timezone { get; set; } = "UTC";

    public CompanyStatus Status { get; set; } = CompanyStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }
}
