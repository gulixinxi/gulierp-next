using GuliERP.Mdm.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 1.5
/// (2026-08-28). IHostedService that runs at app startup and
/// ensures the default BusinessPartner code rule + sequence
/// state exist for every active Tenant.
///
/// <para>
/// Per brief §4, the bootstrap is idempotent and Tenant-scoped.
/// The startup path covers Tenants that were created before the
/// Wave 1.5 deployment without a manual operator run. For Tenants
/// created after this commit, the <see cref="MdmCodeRuleBootstrapService"/>
/// can also be called per-Tenant from the Identity Enterprise
/// bootstrap path (future enhancement, not required for Wave 1.5
/// GREEN).
/// </para>
///
/// <para>
/// Design choice: scoped lifetime + scope per call (a fresh DI
/// scope is created so the call does not share an Entity Framework
/// change tracker with whatever else the host startup is doing).
/// </para>
/// </summary>
public sealed class MdmCodeRuleBootstrapStartupService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MdmCodeRuleBootstrapStartupService> _logger;

    public MdmCodeRuleBootstrapStartupService(
        IServiceProvider services,
        ILogger<MdmCodeRuleBootstrapStartupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var bootstrap = scope.ServiceProvider.GetRequiredService<IMdmCodeRuleBootstrapService>();

            // 1) BusinessPartner — Foundation (V1, unchanged).
            var bpResults = await bootstrap.EnsureDefaultBusinessPartnerRuleForAllTenantsAsync(
                currentUserId: null, ct: cancellationToken);
            var bpCreated = bpResults.Count(r => r.AnyCreated);
            _logger.LogInformation(
                "MdmCodeRuleBootstrapStartupService: BusinessPartner ensured for {Total} Tenant(s); " +
                "{Created} created, {Skipped} already present.",
                bpResults.Count, bpCreated, bpResults.Count - bpCreated);

            // 2) Item — Reuse Wave, Tenant scope. Item is
            // Tenant-scoped (not Company-scoped), so the per-Tenant
            // iteration matches the BusinessPartner shape.
            var itemResults = await bootstrap.EnsureDefaultItemRuleForAllTenantsAsync(
                currentUserId: null, ct: cancellationToken);
            var itemCreated = itemResults.Count(r => r.AnyCreated);
            _logger.LogInformation(
                "MdmCodeRuleBootstrapStartupService: Item ensured for {Total} Tenant(s); " +
                "{Created} created, {Skipped} already present.",
                itemResults.Count, itemCreated, itemResults.Count - itemCreated);

            // 3) Warehouse — Reuse Wave, Company scope. Iterates
            // Tenant -> Company so the rule is unique per Company.
            // On a fresh deployment with no Companies yet, this is
            // an empty no-op.
            var whResults = await bootstrap.EnsureDefaultWarehouseRuleForAllCompaniesAsync(
                currentUserId: null, ct: cancellationToken);
            var whCreated = whResults.Count(r => r.AnyCreated);
            _logger.LogInformation(
                "MdmCodeRuleBootstrapStartupService: Warehouse ensured for {Total} Company(s); " +
                "{Created} created, {Skipped} already present.",
                whResults.Count, whCreated, whResults.Count - whCreated);

            // 4) Location — Reuse Wave, Warehouse scope. Iterates
            // Tenant -> Company -> Warehouse so the sequence is
            // isolated per Warehouse. On a fresh deployment with
            // no Warehouses yet, this is an empty no-op.
            var locResults = await bootstrap.EnsureDefaultLocationRuleForAllWarehousesAsync(
                currentUserId: null, ct: cancellationToken);
            var locCreated = locResults.Count(r => r.AnyCreated);
            _logger.LogInformation(
                "MdmCodeRuleBootstrapStartupService: Location ensured for {Total} Warehouse(s); " +
                "{Created} created, {Skipped} already present.",
                locResults.Count, locCreated, locResults.Count - locCreated);
        }
        catch (Exception ex)
        {
            // The bootstrap is best-effort: it must NOT crash app
            // startup. If the default rule cannot be ensured for
            // any reason (DB down, missing Identity migration,
            // etc.), the user can still create rows via explicit
            // Code and the operator can retry the bootstrap
            // manually. We log loudly so the failure is visible in
            // the operator console.
            _logger.LogError(ex,
                "MdmCodeRuleBootstrapStartupService: failed to ensure code rule(s) at startup. " +
                "Operators can retry via EnsureDefaultXxxRuleForAllYyyAsync (or per-scope).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
