using GuliERP.Identity.Domain.Enums;

namespace GuliERP.Identity.Application.Employee;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — DTOs for
/// the Employee write surface.
///
/// <para>
/// Per <c>GULIERP_EMPLOYEE_MASTER_MODEL_V1.md</c> §9.2 (the V1
/// wire contract is frozen). <see cref="EmployeeDto"/> exposes
/// the full V1 contract; the 3 request DTOs are the wire
/// shapes for Create / Update / SetStatus.
/// </para>
///
/// <para>
/// <b>No contact fields</b>: <c>Mobile / Email / WeChatId / WeComId /
/// QRCode</c> are NOT in the V1 DTO. All contact information
/// goes through the future <c>ContactProfile</c> module (per
/// <c>GULIERP_CONTACT_PROFILE_001_DESIGN_REPORT.md</c>). The
/// brief explicitly forbids adding these to the V1 Employee
/// entity / DTO.
/// </para>
/// </summary>
public sealed record EmployeeDto(
    long Id,
    long TenantId,
    long CompanyId,
    long? DepartmentId,
    long? UserId,
    string EmployeeNo,
    string Name,
    EmployeeStatus Status,
    DateTimeOffset CreatedAt,
    long? CreatedBy,
    DateTimeOffset ModifiedAt,
    long? ModifiedBy,
    int ConcurrencyVersion);

/// <summary>
/// V1 wire shape for <c>POST /api/v1/organization/employees</c>.
/// The App service canonicalizes the <c>EmployeeNo</c> (trim +
/// upper) BEFORE the 4-step pipeline runs.
/// </summary>
public sealed record CreateEmployeeRequest(
    string EmployeeNo,
    string Name,
    long? DepartmentId,
    long? UserId);

/// <summary>
/// V1 wire shape for <c>PUT /api/v1/organization/employees/{id}</c>.
/// <c>EmployeeNo</c> is intentionally NOT in the request (V1 codes
/// are immutable on update; the V1 design freeze). The
/// <c>ExpectedConcurrencyVersion</c> is required for optimistic
/// concurrency.
/// </summary>
public sealed record UpdateEmployeeRequest(
    string Name,
    long? DepartmentId,
    int ExpectedConcurrencyVersion);

/// <summary>
/// V1 wire shape for <c>POST /api/v1/organization/employees/{id}/status</c>.
/// The terminal <c>Left</c> status is allowed as a target; no
/// transition FROM <c>Left</c> is allowed.
/// </summary>
public sealed record SetEmployeeStatusRequest(
    EmployeeStatus Status,
    int ExpectedConcurrencyVersion);

/// <summary>
/// V1 list query — supports paged result + filters. Used by
/// <c>GET /api/v1/organization/companies/{companyId}/employees</c>
/// (replaces the existing flat-list read endpoint with a paged
/// DTO).
/// </summary>
public sealed record EmployeeListQuery(
    long CompanyId,
    long? DepartmentId,
    EmployeeStatus? Status,
    string? Keyword,
    int Page,
    int PageSize);
