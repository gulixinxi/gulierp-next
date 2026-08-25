using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Shared;

namespace GuliERP.Identity.Application.Employee;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — the
/// V1 Employee write service contract.
///
/// <para>
/// 5 methods: Create / GetById / Update / ChangeStatus / ListByCompany.
/// All methods apply the <c>ICompanyScoped</c> Service Boundary
/// contract: <c>Where(e =&gt; e.TenantId == currentTenant.Id
/// &amp;&amp; e.CompanyId == currentCompany.Id)</c> on every
/// read / write. Cross-Company access is denied via
/// <see cref="IdentityValidationException"/> with
/// <see cref="IdentityErrorCodes.EmployeeCrossCompany"/>.
/// </para>
///
/// <para>
/// Per <c>GULIERP_EMPLOYEE_MASTER_MODEL_V1.md</c> §8 (the
/// service contract is frozen).
/// </para>
/// </summary>
public interface IEmployeeWriteService
{
    Task<EmployeeDto> CreateAsync(
        CreateEmployeeRequest request, CancellationToken ct = default);

    Task<EmployeeDto?> GetByIdAsync(
        long id, CancellationToken ct = default);

    Task<EmployeeDto?> UpdateAsync(
        long id, UpdateEmployeeRequest request, CancellationToken ct = default);

    Task<EmployeeDto> ChangeStatusAsync(
        long id, SetEmployeeStatusRequest request, CancellationToken ct = default);

    Task<PagedResult<EmployeeDto>> ListByCompanyAsync(
        EmployeeListQuery query, CancellationToken ct = default);
}
