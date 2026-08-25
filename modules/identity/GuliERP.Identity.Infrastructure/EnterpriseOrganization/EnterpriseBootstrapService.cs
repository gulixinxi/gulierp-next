using System.Security.Cryptography;
using System.Text;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Identity.Application.EnterpriseOrganization;
using GuliERP.Identity.Domain.Entities;
using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Authorization;
using GuliERP.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace GuliERP.Identity.Infrastructure.EnterpriseOrganization;

public sealed class EnterpriseBootstrapService : IEnterpriseBootstrapService
{
    private const string DefaultPlantCode = "MAIN";
    private const string DefaultPlantName = "主工厂";
    private const string RootOrgCode = "ROOT";
    private const string RootOrgName = "公司";
    private const string SystemAdminRoleCode = "ERP_SYSTEM_ADMIN";
    private const string SystemAdminRoleName = "Enterprise System Admin";
    private const string RequiredOrganizationMigration = "20260822090000_G2EnterpriseOrganizationFoundation";

    private readonly IdentityDbContext _db;
    private readonly UserManager<GuliErpUser> _userManager;
    private readonly ILogger<EnterpriseBootstrapService> _logger;

    public EnterpriseBootstrapService(
        IdentityDbContext db,
        UserManager<GuliErpUser> userManager,
        ILogger<EnterpriseBootstrapService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<EnterpriseBootstrapResult> CreateEnterpriseBootstrapAsync(
        CreateEnterpriseBootstrapRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.TenantName))
        {
            throw new ArgumentException("TenantName is required.", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            throw new ArgumentException("CompanyName is required.", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.AdminUserName))
        {
            throw new ArgumentException("AdminUserName is required.", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.AdminDisplayName))
        {
            throw new ArgumentException("AdminDisplayName is required.", nameof(request));
        }

        var tenantName = request.TenantName.Trim();
        var companyName = request.CompanyName.Trim();
        var adminUserName = request.AdminUserName.Trim();
        var adminDisplayName = request.AdminDisplayName.Trim();
        var tenantCode = NormalizeCode(request.TenantCode ?? tenantName);
        var companyCode = NormalizeCode(request.CompanyCode ?? companyName);
        var now = DateTimeOffset.UtcNow;

        await EnsureFormalBootstrapSchemaReadyAsync(ct);

        var existingTenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Code == tenantCode, ct);
        var existingCompany = await _db.Companies.FirstOrDefaultAsync(
            c => c.TenantId == (existingTenant == null ? 0 : existingTenant.Id)
              && c.Code == companyCode,
            ct);
        var existingUser = await _userManager.FindByNameAsync(adminUserName);

        if (existingTenant is null && existingUser is not null)
        {
            throw new EnterpriseBootstrapConflictException(
                $"Admin username '{adminUserName}' already exists outside the requested enterprise.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var created = false;
        var tenant = existingTenant;
        if (tenant is null)
        {
            tenant = new Tenant
            {
                Code = tenantCode,
                Name = tenantName,
                Description = "Enterprise bootstrap.",
                Status = TenantStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            _db.Tenants.Add(tenant);
            created = true;
            await _db.SaveChangesAsync(ct);
        }
        else if (!string.Equals(tenant.Name, tenantName, StringComparison.Ordinal))
        {
            throw new EnterpriseBootstrapConflictException(
                $"Tenant code '{tenantCode}' already belongs to '{tenant.Name}'.");
        }

        var company = existingCompany;
        if (company is null)
        {
            company = new Company
            {
                TenantId = tenant.Id,
                Code = companyCode,
                Name = companyName,
                LegalName = companyName,
                DefaultCurrency = "CNY",
                Timezone = "Asia/Shanghai",
                Status = CompanyStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            _db.Companies.Add(company);
            created = true;
            await _db.SaveChangesAsync(ct);
        }
        else if (!string.Equals(company.Name, companyName, StringComparison.Ordinal))
        {
            throw new EnterpriseBootstrapConflictException(
                $"Company code '{companyCode}' already belongs to '{company.Name}'.");
        }

        var plant = await _db.Plants.FirstOrDefaultAsync(
            p => p.TenantId == tenant.Id && p.CompanyId == company.Id && p.IsDefault,
            ct);
        if (plant is null)
        {
            plant = new Plant
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = DefaultPlantCode,
                Name = DefaultPlantName,
                CountryCode = "CN",
                Timezone = "Asia/Shanghai",
                IsDefault = true,
                Status = PlantStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            _db.Plants.Add(plant);
            created = true;
        }
        var rootOrg = await _db.OrganizationUnits.FirstOrDefaultAsync(
            o => o.TenantId == tenant.Id
              && o.CompanyId == company.Id
              && o.ParentOrganizationUnitId == null,
            ct);
        if (rootOrg is null)
        {
            rootOrg = new OrganizationUnit
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                Code = RootOrgCode,
                Name = RootOrgName,
                Type = OrganizationType.Root,
                Status = OrganizationStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            _db.OrganizationUnits.Add(rootOrg);
            created = true;
        }
        await _db.SaveChangesAsync(ct);

        var adminUser = existingUser;
        if (adminUser is null)
        {
            if (string.IsNullOrWhiteSpace(request.AdminPassword))
            {
                throw new ArgumentException("AdminPassword is required for a new admin user.", nameof(request));
            }
            adminUser = new GuliErpUser
            {
                TenantId = tenant.Id,
                UserName = adminUserName,
                Email = NormalizeOptional(request.AdminEmail),
                PhoneNumber = NormalizeOptional(request.AdminPhoneNumber),
                EmailConfirmed = true,
                DisplayName = adminDisplayName,
                IsPlatformAdmin = false,
                Status = UserStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            var userResult = await _userManager.CreateAsync(adminUser, request.AdminPassword);
            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Enterprise bootstrap failed to create admin user: "
                    + string.Join("; ", userResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
            }
            created = true;
        }
        else if (adminUser.TenantId != tenant.Id)
        {
            throw new EnterpriseBootstrapConflictException(
                $"Admin username '{adminUserName}' belongs to another tenant.");
        }
        else
        {
            adminUser.DisplayName = adminDisplayName;
            adminUser.Email = NormalizeOptional(request.AdminEmail) ?? adminUser.Email;
            adminUser.PhoneNumber = NormalizeOptional(request.AdminPhoneNumber) ?? adminUser.PhoneNumber;
            adminUser.ModifiedAt = now;
            await _userManager.UpdateAsync(adminUser);
        }

        var employee = await _db.Employees.FirstOrDefaultAsync(
            e => e.TenantId == tenant.Id && e.CompanyId == company.Id && e.UserId == adminUser.Id,
            ct);
        if (employee is null)
        {
            employee = new Employee
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                DepartmentId = rootOrg.Id,
                UserId = adminUser.Id,
                // GULIERP_EMPLOYEE_BOOTSTRAP_FIX_001 — the
                // bootstrap admin's EmployeeCode is the frozen
                // "EMP-SYSTEM" (per GULIERP_CODE_RULE_STANDARD_V1
                // §4 + GULIERP_EMPLOYEE_MASTER_MODEL_V1 §3.1).
                // This is a reserved code; operators cannot type
                // it via the Employee write service.
                EmployeeNo = BuildEmployeeNo(adminUserName),
                Name = adminDisplayName,
                Status = EmployeeStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            _db.Employees.Add(employee);
            created = true;
        }
        else
        {
            // GULIERP_EMPLOYEE_BOOTSTRAP_FIX_001 — idempotent
            // dev-only fix-up: if the bootstrap admin's existing
            // EmployeeNo is not the frozen "EMP-SYSTEM" (e.g.,
            // a pre-fix row with EmployeeNo = "ADMIN"), normalize
            // it to "EMP-SYSTEM". This is a no-op when the
            // existing row already has the correct code.
            // The fix runs on every bootstrap call but only when
            // a wrong value is present, so it is idempotent and
            // safe for re-bootstrap.
            var expected = BuildEmployeeNo(adminUserName);
            if (!string.Equals(employee.EmployeeNo, expected, StringComparison.Ordinal))
            {
                employee.EmployeeNo = expected;
                employee.ModifiedAt = now;
                employee.ModifiedBy = null;  // system-driven update
                employee.ConcurrencyVersion += 1;
                _logger.LogInformation(
                    "Identity bootstrap normalized admin EmployeeNo from {Old} to {New} " +
                    "for employeeId={EmployeeId}",
                    employee.EmployeeNo, expected, employee.Id);
            }
        }
        var companyMembershipCreated = false;
        if (!await _db.UserCompanyMemberships.AnyAsync(
            m => m.TenantId == tenant.Id && m.CompanyId == company.Id && m.UserId == adminUser.Id,
            ct))
        {
            _db.UserCompanyMemberships.Add(new UserCompanyMembership
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                UserId = adminUser.Id,
                IsDefault = true,
                JoinedAt = now,
                Status = MembershipStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            });
            companyMembershipCreated = true;
            created = true;
        }
        if (!await _db.UserOrganizationMemberships.AnyAsync(
            m => m.TenantId == tenant.Id
              && m.CompanyId == company.Id
              && m.UserId == adminUser.Id
              && m.OrganizationUnitId == rootOrg.Id,
            ct))
        {
            _db.UserOrganizationMemberships.Add(new UserOrganizationMembership
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                UserId = adminUser.Id,
                OrganizationUnitId = rootOrg.Id,
                IsPrimary = true,
                JoinedAt = now,
                Status = MembershipStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            });
            created = true;
        }
        var (adminRole, adminRoleCreated) = await EnsureSystemAdminRoleAsync(tenant.Id, now, ct);
        var roleAssignmentCreated = await EnsureRoleAssignmentAsync(
            tenant.Id,
            company.Id,
            adminUser.Id,
            adminRole.Id,
            now,
            ct);
        var businessRolePack = await new EnterpriseBusinessRolePackProvisioner(_db)
            .EnsureInitialAdminBusinessRolePackAsync(
                tenant.Id,
                company.Id,
                adminUser.Id,
                now,
                ct);
        created = created || !businessRolePack.Idempotent;
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return new EnterpriseBootstrapResult(
            tenant.Id,
            company.Id,
            plant.Id,
            rootOrg.Id,
            adminUser.Id,
            employee.Id,
            created,
            SystemAdminRoleCode,
            adminRoleCreated,
            companyMembershipCreated,
            roleAssignmentCreated);
    }

    private async Task EnsureFormalBootstrapSchemaReadyAsync(CancellationToken ct)
    {
        if (!_db.Database.IsRelational())
        {
            return;
        }

        IReadOnlyList<string> pendingMigrations;
        try
        {
            pendingMigrations = (await _db.Database.GetPendingMigrationsAsync(ct)).ToArray();
        }
        catch (Exception ex)
        {
            throw new EnterpriseBootstrapSchemaException(
                $"Unable to inspect Identity migration state before formal bootstrap: {ex.GetType().Name}: {ex.Message}");
        }

        if (pendingMigrations.Count > 0)
        {
            throw new EnterpriseBootstrapSchemaException(
                "Identity schema is not current. Pending migrations: "
                + string.Join(", ", pendingMigrations));
        }

        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State == System.Data.ConnectionState.Closed;
        if (shouldClose)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            if (!await HasTableAsync(connection, "identity", "gulierp_employee", ct))
            {
                throw new EnterpriseBootstrapSchemaException(
                    $"Identity schema is missing table identity.gulierp_employee from {RequiredOrganizationMigration}.");
            }

            if (!await HasColumnAsync(connection, "identity", "gulierp_plant", "IsDefault", ct))
            {
                throw new EnterpriseBootstrapSchemaException(
                    $"Identity schema is missing column identity.gulierp_plant.IsDefault from {RequiredOrganizationMigration}.");
            }

            if (!await HasIndexAsync(connection, "identity", "ux_gulierp_plant_company_default", ct))
            {
                throw new EnterpriseBootstrapSchemaException(
                    $"Identity schema is missing index identity.ux_gulierp_plant_company_default from {RequiredOrganizationMigration}.");
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> HasTableAsync(
        DbConnection connection,
        string schema,
        string table,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select exists (
                select 1
                from information_schema.tables
                where table_schema = @schema
                  and table_name = @table
            );
            """;
        AddParameter(command, "schema", schema);
        AddParameter(command, "table", table);
        return await ExecuteBooleanAsync(command, ct);
    }

    private static async Task<bool> HasColumnAsync(
        DbConnection connection,
        string schema,
        string table,
        string column,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select exists (
                select 1
                from information_schema.columns
                where table_schema = @schema
                  and table_name = @table
                  and column_name = @column
            );
            """;
        AddParameter(command, "schema", schema);
        AddParameter(command, "table", table);
        AddParameter(command, "column", column);
        return await ExecuteBooleanAsync(command, ct);
    }

    private static async Task<bool> HasIndexAsync(
        DbConnection connection,
        string schema,
        string index,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select exists (
                select 1
                from pg_indexes
                where schemaname = @schema
                  and indexname = @index
            );
            """;
        AddParameter(command, "schema", schema);
        AddParameter(command, "index", index);
        return await ExecuteBooleanAsync(command, ct);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static async Task<bool> ExecuteBooleanAsync(DbCommand command, CancellationToken ct)
    {
        var value = await command.ExecuteScalarAsync(ct);
        return value is bool result && result;
    }

    private async Task<(GuliErpRole Role, bool Created)> EnsureSystemAdminRoleAsync(
        long tenantId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(
            r => r.TenantId == tenantId && r.Code == SystemAdminRoleCode,
            ct);
        var created = false;
        if (role is null)
        {
            role = new GuliErpRole
            {
                TenantId = tenantId,
                Name = SystemAdminRoleName,
                NormalizedName = SystemAdminRoleName.ToUpperInvariant(),
                Code = SystemAdminRoleCode,
                IsSystem = true,
                Description = "Tenant/company-scoped enterprise administrator.",
                Status = RoleStatus.Active,
                CreatedAt = now,
                ModifiedAt = now,
                ConcurrencyVersion = 1,
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync(ct);
            created = true;
        }

        foreach (var permission in GuliErpPermissions.EnterpriseSystemAdminPermissions)
        {
            if (!await _db.RoleClaims.AnyAsync(
                c => c.RoleId == role.Id
                  && c.ClaimType == GuliErpPermissionClaimTypes.Permission
                  && c.ClaimValue == permission,
                ct))
            {
                _db.RoleClaims.Add(new IdentityRoleClaim<long>
                {
                    RoleId = role.Id,
                    ClaimType = GuliErpPermissionClaimTypes.Permission,
                    ClaimValue = permission,
                });
            }
        }

        return (role, created);
    }

    private async Task<bool> EnsureRoleAssignmentAsync(
        long tenantId,
        long companyId,
        long userId,
        long roleId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (await _db.UserRoleAssignments.AnyAsync(
            a => a.TenantId == tenantId
              && a.CompanyId == companyId
              && a.UserId == userId
              && a.RoleId == roleId,
            ct))
        {
            return false;
        }

        _db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            TenantId = tenantId,
            CompanyId = companyId,
            UserId = userId,
            RoleId = roleId,
            Status = AssignmentStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        });
        return true;
    }

    /// <summary>
    /// GULIERP_EMPLOYEE_BOOTSTRAP_FIX_001 — the bootstrap admin's
    /// EmployeeCode is the frozen <c>"EMP-SYSTEM"</c> constant per
    /// <c>GULIERP_CODE_RULE_STANDARD_V1.md</c> §4 +
    /// <c>GULIERP_EMPLOYEE_MASTER_MODEL_V1.md</c> §3.1. This is a
    /// reserved code (in the Foundation
    /// <see cref="GuliERP.Foundation.Validation.ReservedNameValidator"/>'s
    /// 11-name V1 frozen set); operators cannot type it via the
    /// <see cref="GuliERP.Identity.Application.Employee.IEmployeeWriteService"/>
    /// write surface (the 4-step pipeline rejects it as
    /// <c>identity_employee_code_reserved</c>).
    ///
    /// <para>
    /// The bootstrap creates the row DIRECTLY (bypassing the write
    /// service + the 4-step pipeline) because the bootstrap is a
    /// system-seed operation, not an operator-typed code.
    /// </para>
    /// </summary>
    /// <param name="adminUserName">The admin's UserName (unused
    /// after the freeze; kept for backward compat with the prior
    /// signature). Reserved for future traceability if the
    /// constant needs to vary per-tenant.</param>
    internal static string BuildEmployeeNo(string adminUserName)
    {
        _ = adminUserName; // unused after the freeze
        return BootstrapAdminEmployeeNo;
    }

    /// <summary>
    /// GULIERP_EMPLOYEE_BOOTSTRAP_FIX_001 — the frozen V1
    /// EmployeeCode for the bootstrap admin. Mirrors the same
    /// value in the Foundation
    /// <see cref="GuliERP.Foundation.Validation.ReservedNameValidator"/>'s
    /// reserved set (the bootstrap is the only sanctioned way
    /// to create this row).
    /// </summary>
    private const string BootstrapAdminEmployeeNo = "EMP-SYSTEM";

    private static string NormalizeCode(string source)
    {
        var chars = source.Trim()
            .ToUpperInvariant()
            .Select(ch => ch <= 127 && char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();
        var code = new string(chars).Trim('_');
        while (code.Contains("__", StringComparison.Ordinal))
        {
            code = code.Replace("__", "_", StringComparison.Ordinal);
        }
        if (string.IsNullOrWhiteSpace(code))
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.Trim())));
            code = $"ENT_{hash[..8]}";
        }
        return code[..Math.Min(code.Length, 40)];
    }

    private static string? NormalizeOptional(string? source)
    {
        var value = source?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
