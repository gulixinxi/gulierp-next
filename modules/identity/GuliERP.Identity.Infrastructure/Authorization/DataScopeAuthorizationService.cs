using GuliERP.Foundation.Kernel;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace GuliERP.Identity.Infrastructure.Authorization;

public sealed class DataScopeAuthorizationService : IDataScopeAuthorizationService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentCompany _currentCompany;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;

    public DataScopeAuthorizationService(
        IdentityDbContext db,
        ICurrentTenant currentTenant,
        ICurrentCompany currentCompany,
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentCompany = currentCompany;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    public async Task<bool> CanReadCompanyScopedAsync(
        long resourceTenantId,
        long resourceCompanyId,
        DataScopeMode mode = DataScopeMode.CurrentCompany,
        CancellationToken ct = default)
    {
        if (_currentTenant.Id != resourceTenantId)
        {
            return false;
        }

        if (mode == DataScopeMode.CurrentCompany && _currentCompany.Id != resourceCompanyId)
        {
            return false;
        }

        if (IsTestingHeaderFixture())
        {
            return _currentCompany.Id == resourceCompanyId;
        }

        if (!_currentUser.Id.HasValue)
        {
            return false;
        }

        return await _db.UserCompanyMemberships.AsNoTracking()
            .AnyAsync(m => m.TenantId == resourceTenantId
                        && m.CompanyId == resourceCompanyId
                        && m.UserId == _currentUser.Id.Value
                        && m.Status == MembershipStatus.Active, ct);
    }

    public async Task<bool> CanReadPlantScopedAsync(
        long resourceTenantId,
        long resourceCompanyId,
        long resourcePlantId,
        DataScopeMode mode = DataScopeMode.CurrentCompany,
        CancellationToken ct = default)
    {
        if (!await CanReadCompanyScopedAsync(resourceTenantId, resourceCompanyId, mode, ct))
        {
            return false;
        }

        if (IsTestingHeaderFixture())
        {
            return true;
        }

        return await _db.Plants.AsNoTracking()
            .AnyAsync(p => p.Id == resourcePlantId
                        && p.TenantId == resourceTenantId
                        && p.CompanyId == resourceCompanyId, ct);
    }

    private bool IsTestingHeaderFixture()
    {
        var http = _httpContextAccessor.HttpContext;
        return _environment.IsEnvironment("Testing")
            && http?.Request.Headers.ContainsKey("X-Test-Authenticated") == true;
    }
}
