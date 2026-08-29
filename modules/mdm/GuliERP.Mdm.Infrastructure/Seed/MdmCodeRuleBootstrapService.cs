using GuliERP.Identity.Domain.Enums;
using GuliERP.Identity.Infrastructure.Persistence;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 1.5
/// (2026-08-28). Default <see cref="MasterDataCodeRule"/> bootstrap
/// implementation. See interface docs for the idempotency
/// contract.
/// </summary>
public sealed class MdmCodeRuleBootstrapService : IMdmCodeRuleBootstrapService
{
    // Frozen per GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1 §5:
    //   BusinessPartner = AUTO_EDITABLE, default = BP_000001.
    // These constants are the single source of truth in the
    // V1 service implementation; the unit tests assert against
    // them so any future change must be deliberate.
    private const string BusinessPartnerEntityType = "BusinessPartner";
    private const string BusinessPartnerPrefix = "BP";
    private const string BusinessPartnerSeparator = "_";
    private const int BusinessPartnerSequenceLength = 6;
    private const long BusinessPartnerStartValue = 1;

    private readonly MdmDbContext _mdmDb;
    // IdentityDbContext is used ONLY to enumerate active Tenants.
    // All MDM-table writes go through MdmDbContext. The bootstrap
    // never writes to the Identity schema.
    private readonly IdentityDbContext _identityDb;
    private readonly ILogger<MdmCodeRuleBootstrapService> _logger;

    public MdmCodeRuleBootstrapService(
        MdmDbContext mdmDb,
        IdentityDbContext identityDb,
        ILogger<MdmCodeRuleBootstrapService> logger)
    {
        _mdmDb = mdmDb;
        _identityDb = identityDb;
        _logger = logger;
    }

    public async Task<MdmCodeRuleBootstrapResult> EnsureDefaultBusinessPartnerRuleForTenantAsync(
        long tenantId,
        long? currentUserId,
        CancellationToken ct = default)
    {
        if (tenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tenantId),
                "TenantId must be positive.");
        }

        var existing = await _mdmDb.MasterDataCodeRules
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId
                && x.CompanyId == null
                && x.WarehouseId == null
                && x.EntityType == BusinessPartnerEntityType
                && x.SubType == null,
                ct);

        if (existing is not null)
        {
            // Idempotency: do NOT mutate the existing rule, even
            // if its Prefix / SequenceLength diverges from the V1
            // frozen default. The operator may have customised the
            // rule and we must not silently rewrite it. Likewise
            // do not flip IsActive from false to true.
            _logger.LogInformation(
                "MdmCodeRuleBootstrap: BusinessPartner rule for tenant {TenantId} already exists " +
                "(id={RuleId}, prefix={Prefix}, active={IsActive}); no-op.",
                tenantId, existing.Id, existing.Prefix, existing.IsActive);
            return new MdmCodeRuleBootstrapResult(
                tenantId,
                RuleCreated: false,
                RuleId: existing.Id,
                SequenceStateCreated: false,
                EntityType: existing.EntityType,
                Prefix: existing.Prefix,
                Separator: existing.Separator,
                SequenceLength: existing.SequenceLength,
                StartValue: existing.StartValue);
        }

        var now = DateTimeOffset.UtcNow;
        var rule = new MasterDataCodeRule
        {
            TenantId = tenantId,
            CompanyId = null,
            WarehouseId = null,
            EntityType = BusinessPartnerEntityType,
            SubType = null,
            Mode = MasterDataCodeMode.AutoEditable,
            Prefix = BusinessPartnerPrefix,
            Separator = BusinessPartnerSeparator,
            SequenceLength = BusinessPartnerSequenceLength,
            StartValue = BusinessPartnerStartValue,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = currentUserId,
            ModifiedAt = now,
            ModifiedBy = currentUserId,
            ConcurrencyVersion = 1,
        };
        _mdmDb.MasterDataCodeRules.Add(rule);

        // SaveChanges once to assign the HiLo Id to the rule, then
        // add the sequence state with RuleId populated. This is two
        // SaveChanges calls in the bootstrap path; the runtime
        // path in MasterDataCodeService.ResolveSequenceStateAsync
        // uses one SaveChanges to lazy-create the state when the
        // rule already exists. The two paths are independent and
        // each saves once.
        await _mdmDb.SaveChangesAsync(ct);

        var state = new MasterDataCodeSequenceState
        {
            RuleId = rule.Id,
            TenantId = tenantId,
            CompanyId = null,
            WarehouseId = null,
            CurrentValue = rule.StartValue - 1,
            LastGeneratedCode = null,
            CreatedAt = now,
            CreatedBy = currentUserId,
            ModifiedAt = now,
            ModifiedBy = currentUserId,
            ConcurrencyVersion = 1,
        };
        _mdmDb.MasterDataCodeSequenceStates.Add(state);
        await _mdmDb.SaveChangesAsync(ct);

        _logger.LogInformation(
            "MdmCodeRuleBootstrap: created BusinessPartner rule id={RuleId} + sequence state for tenant {TenantId}.",
            rule.Id, tenantId);

        return new MdmCodeRuleBootstrapResult(
            tenantId,
            RuleCreated: true,
            RuleId: rule.Id,
            SequenceStateCreated: true,
            EntityType: rule.EntityType,
            Prefix: rule.Prefix,
            Separator: rule.Separator,
            SequenceLength: rule.SequenceLength,
            StartValue: rule.StartValue);
    }

    public async Task<IReadOnlyList<MdmCodeRuleBootstrapResult>> EnsureDefaultBusinessPartnerRuleForAllTenantsAsync(
        long? currentUserId = null,
        CancellationToken ct = default)
    {
        // Discover Tenants via the canonical Identity DbContext
        // (the Identity schema is the single source of truth for
        // the Tenant list). The MDM DbContext does not own a
        // Tenants DbSet because MDM does not own the Tenant
        // table. The bootstrap never writes to the Identity schema.
        var tenantIds = await _identityDb.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var results = new List<MdmCodeRuleBootstrapResult>(tenantIds.Count);
        foreach (var tenantId in tenantIds)
        {
            results.Add(await EnsureDefaultBusinessPartnerRuleForTenantAsync(
                tenantId, currentUserId, ct));
        }
        return results;
    }
}
