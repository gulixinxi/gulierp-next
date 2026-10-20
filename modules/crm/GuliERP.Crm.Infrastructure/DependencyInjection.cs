using GuliERP.Crm.Application;
using GuliERP.Crm.Infrastructure.Persistence;
using GuliERP.Crm.Infrastructure.Services;
using GuliERP.Identity.Application.Authorization;
using GuliERP.Mdm.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GuliERP.Crm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGuliErpCrm(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<CrmDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable(
                "__ef_migrations_history",
                CrmDbContext.DefaultSchema));
        });

        // 服务注册
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IDealService, DealService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IImportExportLogService, ImportExportLogService>();
        services.AddScoped<ISignalService, SignalService>();
        // ERP客商候选匹配（只读）：通过MDM应用层契约查询，不直接用MdmDbContext
        services.AddScoped<ICompanyErpMatchService, CompanyErpMatchService>();
        services.AddHttpClient<QuakeSearchService>();
        services.AddScoped<IcpLookupService>();
        services.AddHttpClient<WebsiteCheckService>();
        services.AddScoped<GeoCheckService>();
        services.AddScoped<ReverseLookupService>();
        services.AddScoped<QuakeQueryHistoryService>();

        // 权限策略
        services.AddAuthorization(options =>
        {
            // 线索池
            options.AddPolicy(CrmPolicies.LeadRead, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.LeadRead)));
            options.AddPolicy(CrmPolicies.LeadManage, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.LeadManage)));
            options.AddPolicy(CrmPolicies.LeadClaim, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.LeadClaim)));
            options.AddPolicy(CrmPolicies.LeadConvert, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.LeadConvert)));
            options.AddPolicy(CrmPolicies.LeadImport, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.LeadImport)));
            options.AddPolicy(CrmPolicies.LeadExport, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.LeadExport)));

            // 企业/客户主档
            options.AddPolicy(CrmPolicies.CompanyRead, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.CompanyRead)));
            options.AddPolicy(CrmPolicies.CompanyManage, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.CompanyManage)));
            options.AddPolicy(CrmPolicies.CompanyAssign, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.CompanyAssign)));
            options.AddPolicy(CrmPolicies.CompanyExport, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.CompanyExport)));
            options.AddPolicy(CrmPolicies.CompanyImport, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.CompanyImport)));

            // 联系人
            options.AddPolicy(CrmPolicies.ContactRead, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.ContactRead)));
            options.AddPolicy(CrmPolicies.ContactManage, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.ContactManage)));

            // 商机
            options.AddPolicy(CrmPolicies.DealRead, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.DealRead)));
            options.AddPolicy(CrmPolicies.DealManage, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.DealManage)));

            // 跟进记录
            options.AddPolicy(CrmPolicies.ActivityRead, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.ActivityRead)));
            options.AddPolicy(CrmPolicies.ActivityManage, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.ActivityManage)));

            // 导入导出日志
            options.AddPolicy(CrmPolicies.LogRead, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.LogRead)));

            // ERP客商候选匹配：双重读取权限（CRM客户读取 + MDM客商读取）
            // 跨模块只读查询，必须同时拥有两个领域的读取权限
            options.AddPolicy(CrmPolicies.CompanyErpMatch, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(CrmPermissions.CompanyRead))
                .AddRequirements(new PermissionRequirement(MdmPermissions.BusinessPartnerRead)));
        });

        return services;
    }
}
