using GuliERP.Foundation.Kernel;
using GuliERP.Foundation.Validation;
using GuliERP.Identity.Application.Employee;
using GuliERP.Identity.Application.Employee.Validation;
using GuliERP.Identity.Application.Shared;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Identity.Infrastructure.EmployeeSvc;

/// <summary>
/// GULIERP_EMPLOYEE_MASTER_001_DOMAIN_IMPLEMENTATION — the
/// <see cref="IEmployeeWriteService"/> implementation.
///
/// <para>
/// 5 methods: <see cref="CreateAsync"/> / <see cref="GetByIdAsync"/>
/// / <see cref="UpdateAsync"/> / <see cref="ChangeStatusAsync"/>
/// / <see cref="ListByCompanyAsync"/>. The 4-step code pipeline
/// runs at the top of CreateAsync (after canonicalization,
/// before the DB uniqueness check).
/// </para>
///
/// <para>
/// Service Boundary: every read / write applies the
/// <c>ICompanyScoped</c> predicate <c>Where(e =&gt; e.TenantId ==
/// currentTenant.Id &amp;&amp; e.CompanyId == currentCompany.Id)</c>.
/// Cross-Company access is denied with
/// <see cref="IdentityErrorCodes.EmployeeCrossCompany"/> (the
/// 404 to avoid leaking existence).
/// </para>
///
/// <para>
/// EmployeeCode is immutable on update (V1 design freeze). The
/// Update DTO has no <c>EmployeeNo</c> field; the helper
/// canonicalizes (trim + upper) only on Create.
/// </para>
/// </summary>
public sealed class EmployeeWriteService : IEmployeeWriteService
{
    private const int MaxNameLength = 200;
    private const int MaxEmployeeNoLength = 40;
    private const int MaxPageSize = 200;
    private const int DefaultPageSize = 20;

    private readonly IdentityDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EmployeeWriteService> _logger;

    public EmployeeWriteService(
        IdentityDbContext db,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        ILogger<EmployeeWriteService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<EmployeeDto> CreateAsync(
        CreateEmployeeRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var companyId = RequireCompany();

        // 1. Canonicalize EmployeeNo (trim + upper).
        var employeeNo = CanonicalizeEmployeeNo(request.EmployeeNo, nameof(request.EmployeeNo));
        // 2. Validate Name (1..200 chars, trim).
        var name = ValidateName(request.Name, nameof(request.Name));
        // 3. Validate DepartmentId (cross-Company + Active status).
        await EnsureDepartmentValidAsync(request.DepartmentId, tenantId, companyId, ct);
        // 4. Validate UserId (cross-Tenant + Active status).
        await EnsureUserValidAsync(request.UserId, tenantId, ct);

        // 5. Run the 4-step Foundation pipeline (Steps 1, 2, 4).
        ThrowIfEmployeeCodeInvalid(employeeNo, tenantId, companyId);

        // 6. Uniqueness check (Step 3 is the DB's job).
        var duplicate = await _db.Employees.AsNoTracking()
            .AnyAsync(e => e.TenantId == tenantId
                && e.CompanyId == companyId
                && e.EmployeeNo == employeeNo, ct);
        if (duplicate)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeCodeDuplicate,
                $"Employee with EmployeeNo '{employeeNo}' already exists in this Company.");
        }

        var now = DateTimeOffset.UtcNow;
        var employee = new Employee
        {
            TenantId = tenantId,
            CompanyId = companyId,
            DepartmentId = request.DepartmentId,
            UserId = request.UserId,
            EmployeeNo = employeeNo,
            Name = name,
            Status = EmployeeStatus.Active,
            CreatedAt = now,
            CreatedBy = _currentUser.Id,
            ModifiedAt = now,
            ModifiedBy = _currentUser.Id,
            ConcurrencyVersion = 1,
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Identity Employee created id={Id} tenant={TenantId} company={CompanyId} employeeNo={EmployeeNo}",
            employee.Id, employee.TenantId, employee.CompanyId, employee.EmployeeNo);

        return MapToDto(employee);
    }

    public async Task<EmployeeDto?> GetByIdAsync(
        long id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var companyId = RequireCompany();
        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id
                && e.TenantId == tenantId
                && e.CompanyId == companyId, ct);
        return employee is null ? null : MapToDto(employee);
    }

    public async Task<EmployeeDto?> UpdateAsync(
        long id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var companyId = RequireCompany();
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id
                && e.TenantId == tenantId
                && e.CompanyId == companyId, ct);
        if (employee is null) return null;

        // Terminal status check: Left is irreversible.
        if (employee.Status == EmployeeStatus.Left)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeAlreadyLeft,
                $"Employee id={id} has status=Left and cannot be modified. Left is terminal.");
        }

        // Optimistic concurrency check.
        if (employee.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeConcurrencyConflict,
                $"Employee id={id} has been modified by another user. " +
                $"Expected ConcurrencyVersion={request.ExpectedConcurrencyVersion}, " +
                $"actual={employee.ConcurrencyVersion}. Reload and retry.");
        }

        var name = ValidateName(request.Name, nameof(request.Name));
        await EnsureDepartmentValidAsync(request.DepartmentId, tenantId, companyId, ct);
        // Note: UserId is immutable in V1 (no UI path to change
        // the login link; only the Admin DB script or a future
        // V1.5+ "system settings" page can do it).

        employee.Name = name;
        employee.DepartmentId = request.DepartmentId;
        employee.ModifiedAt = DateTimeOffset.UtcNow;
        employee.ModifiedBy = _currentUser.Id;
        employee.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Identity Employee updated id={Id} name={Name} departmentId={DepartmentId}",
            employee.Id, employee.Name, employee.DepartmentId);
        return MapToDto(employee);
    }

    public async Task<EmployeeDto> ChangeStatusAsync(
        long id, SetEmployeeStatusRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tenantId = RequireTenant();
        var companyId = RequireCompany();
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id
                && e.TenantId == tenantId
                && e.CompanyId == companyId, ct);
        if (employee is null)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeNotFound,
                $"Employee id={id} not found in current Tenant + Company scope.");
        }

        if (employee.ConcurrencyVersion != request.ExpectedConcurrencyVersion)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeConcurrencyConflict,
                $"Employee id={id} has been modified by another user. " +
                $"Expected ConcurrencyVersion={request.ExpectedConcurrencyVersion}, " +
                $"actual={employee.ConcurrencyVersion}. Reload and retry.");
        }

        // Status transition rules:
        //   Active   → Inactive : allowed
        //   Inactive → Active   : allowed
        //   Active   → Left     : allowed (offboarding)
        //   Inactive → Left     : allowed
        //   Left     → *        : forbidden (Left is terminal)
        if (employee.Status == EmployeeStatus.Left && request.Status != EmployeeStatus.Left)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeAlreadyLeft,
                $"Employee id={id} has status=Left and cannot transition to '{request.Status}'. " +
                "Left is terminal.");
        }

        employee.Status = request.Status;
        employee.ModifiedAt = DateTimeOffset.UtcNow;
        employee.ModifiedBy = _currentUser.Id;
        employee.ConcurrencyVersion += 1;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Identity Employee status changed id={Id} status={Status}",
            employee.Id, employee.Status);
        return MapToDto(employee);
    }

    public async Task<PagedResult<EmployeeDto>> ListByCompanyAsync(
        EmployeeListQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var tenantId = RequireTenant();
        var companyId = RequireCompany();

        // The V1 contract: the request's CompanyId must match the
        // current Company (Tenant + Company scope is enforced via
        // ICurrentCompany). Cross-Company list access is
        // denied with 403 (the caller is asking for the wrong
        // Company).
        if (query.CompanyId != companyId)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeCrossCompany,
                $"List query CompanyId={query.CompanyId} does not match " +
                $"current CompanyId={companyId}. Cross-Company list access is denied.");
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize,
        };

        var keyword = query.Keyword?.Trim();
        var hasKeyword = !string.IsNullOrEmpty(keyword);

        var q = _db.Employees.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.CompanyId == companyId);

        if (query.DepartmentId.HasValue)
        {
            var deptId = query.DepartmentId.Value;
            q = q.Where(e => e.DepartmentId == deptId);
        }
        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            q = q.Where(e => e.Status == status);
        }
        if (hasKeyword)
        {
            q = q.Where(e => e.EmployeeNo.Contains(keyword!) || e.Name.Contains(keyword!));
        }

        var totalCount = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(e => e.EmployeeNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<EmployeeDto>(
            items.Select(MapToDto).ToList(),
            page, pageSize, (int)totalCount);
    }

    // ----------------------------------------------------------------
    // Helpers (mirrors the MDM service pattern)
    // ----------------------------------------------------------------

    private long RequireTenant()
    {
        if (!_currentTenant.Id.HasValue)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeNotFound,
                "Current Tenant is not resolved. Tenant-scoped Employee data cannot be read or written.");
        }
        return _currentTenant.Id.Value;
    }

    private long RequireCompany()
    {
        if (!_currentCompany.Id.HasValue)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeNotFound,
                "Current Company is not resolved. Company-scoped Employee data cannot be read or written.");
        }
        return _currentCompany.Id.Value;
    }

    private static string CanonicalizeEmployeeNo(string code, string paramName)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeCodeFormatInvalid,
                $"{paramName} is required.");
        }
        var trimmed = code.Trim();
        if (trimmed.Length > MaxEmployeeNoLength)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeCodeFormatInvalid,
                $"{paramName} exceeds max length of {MaxEmployeeNoLength}.");
        }
        return trimmed.ToUpperInvariant();
    }

    private static string ValidateName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeCodeFormatInvalid,  // 4-step pipeline wraps "name" validation here
                $"{paramName} is required.");
        }
        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeCodeFormatInvalid,
                $"{paramName} exceeds max length of {MaxNameLength}.");
        }
        return trimmed;
    }

    private async Task EnsureDepartmentValidAsync(
        long? departmentId, long tenantId, long companyId, CancellationToken ct)
    {
        if (!departmentId.HasValue) return;
        var dept = await _db.OrganizationUnits.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == departmentId.Value, ct);
        if (dept is null)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeDepartmentCrossCompany,
                $"DepartmentId={departmentId} does not exist.");
        }
        if (dept.TenantId != tenantId || dept.CompanyId != companyId)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeDepartmentCrossCompany,
                $"DepartmentId={departmentId} exists but in a different " +
                $"Tenant ({dept.TenantId} vs {tenantId}) or Company " +
                $"({dept.CompanyId} vs {companyId}).");
        }
        if (dept.Status != OrganizationStatus.Active)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeDepartmentInactive,
                $"DepartmentId={departmentId} has status={dept.Status}. " +
                "Only Active Departments can be assigned to a new Employee.");
        }
    }

    private async Task EnsureUserValidAsync(
        long? userId, long tenantId, CancellationToken ct)
    {
        if (!userId.HasValue) return;
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user is null)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeUserCrossTenant,
                $"UserId={userId} does not exist.");
        }
        if (user.TenantId != tenantId)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeUserCrossTenant,
                $"UserId={userId} exists but in Tenant={user.TenantId} " +
                $"(expected {tenantId}). Cross-Tenant User link is denied.");
        }
        if (user.Status == UserStatus.Disabled || user.Status == UserStatus.Locked)
        {
            throw new IdentityValidationException(
                IdentityErrorCodes.EmployeeUserInactive,
                $"UserId={userId} has status={user.Status}. " +
                "Only Active / Pending / Locked users can be linked to a new Employee.");
        }
    }

    /// <summary>
    /// Run the 4-step Foundation pipeline (Steps 1, 2, 4) for
    /// the EmployeeCode. Step 3 (uniqueness) is checked by the
    /// caller (the DB unique index + the service-level check).
    /// Throws <see cref="IdentityValidationException"/> on the
    /// first failure. The code is expected to have been
    /// canonicalized (trim + upper) by
    /// <see cref="CanonicalizeEmployeeNo"/> before this is
    /// called.
    /// </summary>
    internal static void ThrowIfEmployeeCodeInvalid(
        string code, long tenantId, long companyId)
    {
        var context = CodeValidationContextExtensions.ForIdentity(
            entityScope: "IdentityEmployee",
            tenantId: tenantId,
            companyId: companyId);

        var result = MasterDataCodeValidator.Validate(code, context);
        if (!result.IsValid)
        {
            throw new IdentityValidationException(
                result.Failure!.ErrorCode,
                result.Failure.Message);
        }
    }

    private static EmployeeDto MapToDto(Employee e) => new(
        e.Id, e.TenantId, e.CompanyId, e.DepartmentId, e.UserId,
        e.EmployeeNo, e.Name, e.Status,
        e.CreatedAt, e.CreatedBy, e.ModifiedAt, e.ModifiedBy, e.ConcurrencyVersion);
}
