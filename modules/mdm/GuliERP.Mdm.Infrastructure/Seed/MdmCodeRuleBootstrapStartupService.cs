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
            var results = await bootstrap.EnsureDefaultBusinessPartnerRuleForAllTenantsAsync(
                currentUserId: null, ct: cancellationToken);

            var createdCount = results.Count(r => r.AnyCreated);
            var skippedCount = results.Count - createdCount;
            _logger.LogInformation(
                "MdmCodeRuleBootstrapStartupService: ensured BusinessPartner code rule for {Total} Tenant(s); " +
                "{Created} created, {Skipped} already present (idempotent no-op).",
                results.Count, createdCount, skippedCount);
        }
        catch (Exception ex)
        {
            // The bootstrap is best-effort: it must NOT crash app
            // startup. If the default rule cannot be ensured for
            // any reason (DB down, missing Identity migration,
            // etc.), the user can still create BusinessPartner
            // rows via explicit Code and the operator can retry
            // the bootstrap manually. We log loudly so the failure
            // is visible in the operator console.
            _logger.LogError(ex,
                "MdmCodeRuleBootstrapStartupService: failed to ensure BusinessPartner code rule(s) at startup. " +
                "Operators can retry via EnsureDefaultBusinessPartnerRuleForAllTenantsAsync (or per-Tenant).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
