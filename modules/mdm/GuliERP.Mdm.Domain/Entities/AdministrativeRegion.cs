using GuliERP.Foundation.Kernel;

namespace GuliERP.Mdm.Domain.Entities;

/// <summary>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2
/// (2026-08-28). Administrative region reference data (single
/// self-referencing hierarchy table).
///
/// <para>
/// <b>Scope</b>: Region is a system-level (NOT Tenant-scoped)
/// reference entity. The <c>(CountryCode, Code)</c> pair is
/// globally unique. <c>ParentId</c> points to another row in the
/// same table; V1 supports an arbitrary-depth hierarchy (no fixed
/// 3-level assumption per brief §二十).
/// </para>
///
/// <para>
/// Per brief §二十一, V1 minimal fields are:
/// <c>Id</c>, <c>CountryCode</c>, <c>Code</c>, <c>Name</c>,
/// <c>ParentId</c>, <c>Level</c>, <c>RegionType</c>,
/// <c>IsActive</c>, <c>SortOrder</c>. <c>Level</c> is a denormalized
/// depth (0 = country-level, 1 = province, 2 = prefecture/city, ...)
/// kept for fast indexing. <c>RegionType</c> is a short label
/// (e.g. <c>province</c>, <c>prefecture</c>, <c>county</c>,
/// <c>district</c>, <c>special-municipality</c>,
/// <c>autonomous-region</c>, <c>special-administrative-region</c>,
/// etc.) per brief §二十.
/// </para>
///
/// <para>
/// CN data source: <c>https://dmfw.mca.gov.cn/</c> (per brief
/// §二十九). V1 only persists Province / Prefecture / County
/// levels per brief §十九 (town / street / community / village
/// are deferred).
/// </para>
/// </summary>
public sealed class AdministrativeRegion : IMultiTenant
{
    public long Id { get; set; }

    public long TenantId => 0;

    /// <summary>FK to <see cref="Country.Code"/>. The Region's
    /// parent chain must stay within the same Country.</summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// In-country region code. Concatenated with <c>CountryCode</c>
    /// forms the natural key. Examples (CN, GB/T 2260):
    /// <c>110000</c> (Beijing municipality), <c>110100</c>
    /// (Beijing city), <c>110101</c> (Dongcheng District).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name in the current UI locale (zh-Hans in V1).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional English name (sourced from CLDR or the official CN pinyin table).</summary>
    public string? EnglishName { get; set; }

    /// <summary>Optional short name (e.g. 粤 for Guangdong, 京 for Beijing). NULL if N/A.</summary>
    public string? ShortName { get; set; }

    /// <summary>Self-FK to another <see cref="AdministrativeRegion.Id"/>. NULL for country-level roots.</summary>
    public long? ParentId { get; set; }

    /// <summary>Denormalized depth: 0 = country-level, 1 = province, 2 = prefecture, 3 = county, ...</summary>
    public int Level { get; set; }

    /// <summary>Short label: <c>province</c>, <c>prefecture</c>, <c>county</c>, <c>special-municipality</c>, <c>autonomous-region</c>, <c>special-administrative-region</c>, ...</summary>
    public string RegionType { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public long? ModifiedBy { get; set; }
    public int ConcurrencyVersion { get; set; }

    /// <summary>Navigation to the parent Region (self-FK).</summary>
    public AdministrativeRegion? Parent { get; set; }

    /// <summary>Navigation to direct children (loaded on demand by the service).</summary>
    public ICollection<AdministrativeRegion> Children { get; set; } = new List<AdministrativeRegion>();
}
