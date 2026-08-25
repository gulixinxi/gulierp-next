namespace GuliERP.Identity.Application.Shared;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — local
/// <c>PagedResult&lt;T&gt;</c> for the Identity module. The MDM
/// module has its own <c>PagedResult&lt;T&gt;</c>; the Identity
/// module does not import the MDM module (per
/// <c>GULIERP_MODULE_INDEPENDENCE_RULE</c>), so the Identity
/// Application layer has its own copy.
///
/// <para>
/// The shape matches the MDM's
/// <c>GuliERP.Mdm.Application.PagedResult&lt;T&gt;</c>: a
/// read-only list of items + page metadata. The wire shape is
/// identical; the Identity layer's paged endpoints serialize
/// to the same JSON.
/// </para>
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
