using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Domain.Enums;

namespace GuliERP.Identity.Domain.Entities;

/// <summary>
/// Tenant — customer / SaaS / data-isolation boundary. Per G2-003A
/// DEC-ID-001 a Tenant is the top-level isolation unit; V1 has one
/// Tenant per host, V1.5+ will support N Tenants per host (SaaS).
///
/// <para>
/// <b>Not a Company</b>. Per G2-003A Q1 the Tenant boundary is a
/// customer-billing / isolation boundary; a Company is a legal entity
/// that lives UNDER a Tenant (1 Tenant → N Company, DEC-ID-002).
/// </para>
/// </summary>
public sealed class Tenant : IMultiTenant
{
    public long Id { get; set; }
    public long TenantId => Id;   // self-reference for IMultiTenant

    /// <summary>UPPER_SNAKE; unique within the database (V1 single-host).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description / display name.</summary>
    public string? Description { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }

    /// <summary>EF Core optimistic concurrency token.</summary>
    public int ConcurrencyVersion { get; set; }
}
