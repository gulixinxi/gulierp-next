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

    // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30) — frozen
    // per the Reuse Wave brief §十一 / §十九 / §二十六:
    //   Warehouse = AUTO_EDITABLE, Company scope, default = WH_001.
    //   Location  = AUTO_EDITABLE, Warehouse scope, default = LOC_000001.
    //   Item      = AUTO_EDITABLE, Tenant scope, default = ITEM_000001.
    // Same idempotency contract as the BusinessPartner bootstrap
    // (the existing 4 invariants are preserved verbatim).
    private const string WarehouseEntityType = "Warehouse";
    private const string WarehousePrefix = "WH";
    private const int WarehouseSequenceLength = 3;
    private const long WarehouseStartValue = 1;

    private const string LocationEntityType = "Location";
    private const string LocationPrefix = "LOC";
    private const int LocationSequenceLength = 6;
    private const long LocationStartValue = 1;

    private const string ItemEntityType = "Item";
    private const string ItemPrefix = "ITEM";
    private const int ItemSequenceLength = 6;
    private const long ItemStartValue = 1;

    // Shared separator (the engine is FROZEN, so all rules use
    // the same "_" already in the Foundation spec).
    private const string DefaultSeparator = "_";

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

    // ============================================================
    // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30)
    // Default rule bootstrap for Warehouse / Location / Item.
    // The 4 invariants on IMdmCodeRuleBootstrapService apply
    // verbatim: idempotent on (TenantId, CompanyId, WarehouseId,
    // EntityType, SubType); never mutates an existing rule; never
    // touches the Warehouse / Location / Item tables; sequence
    // initialised to StartValue-1.
    //
    // All three implementations below are private refactors of
    // the original EnsureDefaultBusinessPartnerRuleForTenantAsync
    // body — they call the same MasterDataCodeRule +
    // MasterDataCodeSequenceState write path the Foundation
    // already VERIFIED. The Foundation Core (engine +
    // MasterDataCodeService) is NOT modified.
    // ============================================================

    public async Task<MdmCodeRuleBootstrapResult> EnsureDefaultWarehouseRuleForCompanyAsync(
        long tenantId,
        long companyId,
        long? currentUserId,
        CancellationToken ct = default)
    {
        if (tenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tenantId),
                "TenantId must be positive.");
        }
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId),
                "CompanyId must be positive.");
        }

        var existing = await _mdmDb.MasterDataCodeRules
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId
                && x.CompanyId == companyId
                && x.WarehouseId == null
                && x.EntityType == WarehouseEntityType
                && x.SubType == null,
                ct);

        if (existing is not null)
        {
            _logger.LogInformation(
                "MdmCodeRuleBootstrap: Warehouse rule for tenant={TenantId} company={CompanyId} " +
                "already exists (id={RuleId}, prefix={Prefix}, active={IsActive}); no-op.",
                tenantId, companyId, existing.Id, existing.Prefix, existing.IsActive);
            return new MdmCodeRuleBootstrapResult(
                tenantId, RuleCreated: false, RuleId: existing.Id,
                SequenceStateCreated: false,
                EntityType: existing.EntityType, Prefix: existing.Prefix,
                Separator: existing.Separator, SequenceLength: existing.SequenceLength,
                StartValue: existing.StartValue);
        }

        var (rule, state) = await CreateRuleAndStateAsync(
            tenantId: tenantId,
            companyId: companyId,
            warehouseId: null,
            entityType: WarehouseEntityType,
            prefix: WarehousePrefix,
            sequenceLength: WarehouseSequenceLength,
            startValue: WarehouseStartValue,
            currentUserId: currentUserId,
            ct: ct);

        return new MdmCodeRuleBootstrapResult(
            tenantId, RuleCreated: true, RuleId: rule.Id,
            SequenceStateCreated: true,
            EntityType: rule.EntityType, Prefix: rule.Prefix,
            Separator: rule.Separator, SequenceLength: rule.SequenceLength,
            StartValue: rule.StartValue);
    }

    public async Task<IReadOnlyList<MdmCodeRuleBootstrapResult>> EnsureDefaultWarehouseRuleForAllCompaniesAsync(
        long? currentUserId = null,
        CancellationToken ct = default)
    {
        // Iterate Tenant -> Company. Bootstrap writes a
        // Company-scoped rule; the Identity schema is the single
        // source of truth for active Tenants + Companies.
        var tenants = await _identityDb.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var results = new List<MdmCodeRuleBootstrapResult>();
        foreach (var tenantId in tenants)
        {
            var companyIds = await _identityDb.Companies
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId
                    && c.Status == CompanyStatus.Active)
                .Select(c => c.Id)
                .ToListAsync(ct);
            foreach (var companyId in companyIds)
            {
                results.Add(await EnsureDefaultWarehouseRuleForCompanyAsync(
                    tenantId, companyId, currentUserId, ct));
            }
        }
        return results;
    }

    public async Task<MdmCodeRuleBootstrapResult> EnsureDefaultLocationRuleForWarehouseAsync(
        long tenantId,
        long companyId,
        long warehouseId,
        long? currentUserId,
        CancellationToken ct = default)
    {
        if (tenantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tenantId),
                "TenantId must be positive.");
        }
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId),
                "CompanyId must be positive.");
        }
        if (warehouseId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warehouseId),
                "WarehouseId must be positive.");
        }

        var existing = await _mdmDb.MasterDataCodeRules
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId
                && x.CompanyId == companyId
                && x.WarehouseId == warehouseId
                && x.EntityType == LocationEntityType
                && x.SubType == null,
                ct);

        if (existing is not null)
        {
            _logger.LogInformation(
                "MdmCodeRuleBootstrap: Location rule for tenant={TenantId} company={CompanyId} " +
                "warehouse={WarehouseId} already exists (id={RuleId}, prefix={Prefix}); no-op.",
                tenantId, companyId, warehouseId, existing.Id, existing.Prefix);
            return new MdmCodeRuleBootstrapResult(
                tenantId, RuleCreated: false, RuleId: existing.Id,
                SequenceStateCreated: false,
                EntityType: existing.EntityType, Prefix: existing.Prefix,
                Separator: existing.Separator, SequenceLength: existing.SequenceLength,
                StartValue: existing.StartValue);
        }

        var (rule, _) = await CreateRuleAndStateAsync(
            tenantId: tenantId,
            companyId: companyId,
            warehouseId: warehouseId,
            entityType: LocationEntityType,
            prefix: LocationPrefix,
            sequenceLength: LocationSequenceLength,
            startValue: LocationStartValue,
            currentUserId: currentUserId,
            ct: ct);

        return new MdmCodeRuleBootstrapResult(
            tenantId, RuleCreated: true, RuleId: rule.Id,
            SequenceStateCreated: true,
            EntityType: rule.EntityType, Prefix: rule.Prefix,
            Separator: rule.Separator, SequenceLength: rule.SequenceLength,
            StartValue: rule.StartValue);
    }

    public async Task<IReadOnlyList<MdmCodeRuleBootstrapResult>> EnsureDefaultLocationRuleForAllWarehousesAsync(
        long? currentUserId = null,
        CancellationToken ct = default)
    {
        // Iterate Tenant -> Company -> Warehouse. Bootstrap
        // writes a Warehouse-scoped rule; Identity is the
        // canonical source for Tenant / Company, and the MDM
        // schema (Warehouses table) is the canonical source for
        // active Warehouses.
        var tenants = await _identityDb.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var results = new List<MdmCodeRuleBootstrapResult>();
        foreach (var tenantId in tenants)
        {
            var companyIds = await _identityDb.Companies
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId
                    && c.Status == CompanyStatus.Active)
                .Select(c => c.Id)
                .ToListAsync(ct);
            foreach (var companyId in companyIds)
            {
                var warehouseIds = await _mdmDb.Warehouses
                    .AsNoTracking()
                    .Where(w => w.TenantId == tenantId && w.CompanyId == companyId)
                    .Select(w => w.Id)
                    .ToListAsync(ct);
                foreach (var warehouseId in warehouseIds)
                {
                    results.Add(await EnsureDefaultLocationRuleForWarehouseAsync(
                        tenantId, companyId, warehouseId, currentUserId, ct));
                }
            }
        }
        return results;
    }

    public async Task<MdmCodeRuleBootstrapResult> EnsureDefaultItemRuleForTenantAsync(
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
                && x.EntityType == ItemEntityType
                && x.SubType == null,
                ct);

        if (existing is not null)
        {
            _logger.LogInformation(
                "MdmCodeRuleBootstrap: Item rule for tenant={TenantId} already exists " +
                "(id={RuleId}, prefix={Prefix}); no-op.",
                tenantId, existing.Id, existing.Prefix);
            return new MdmCodeRuleBootstrapResult(
                tenantId, RuleCreated: false, RuleId: existing.Id,
                SequenceStateCreated: false,
                EntityType: existing.EntityType, Prefix: existing.Prefix,
                Separator: existing.Separator, SequenceLength: existing.SequenceLength,
                StartValue: existing.StartValue);
        }

        var (rule, _) = await CreateRuleAndStateAsync(
            tenantId: tenantId,
            companyId: null,
            warehouseId: null,
            entityType: ItemEntityType,
            prefix: ItemPrefix,
            sequenceLength: ItemSequenceLength,
            startValue: ItemStartValue,
            currentUserId: currentUserId,
            ct: ct);

        return new MdmCodeRuleBootstrapResult(
            tenantId, RuleCreated: true, RuleId: rule.Id,
            SequenceStateCreated: true,
            EntityType: rule.EntityType, Prefix: rule.Prefix,
            Separator: rule.Separator, SequenceLength: rule.SequenceLength,
            StartValue: rule.StartValue);
    }

    public async Task<IReadOnlyList<MdmCodeRuleBootstrapResult>> EnsureDefaultItemRuleForAllTenantsAsync(
        long? currentUserId = null,
        CancellationToken ct = default)
    {
        var tenantIds = await _identityDb.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var results = new List<MdmCodeRuleBootstrapResult>(tenantIds.Count);
        foreach (var tenantId in tenantIds)
        {
            results.Add(await EnsureDefaultItemRuleForTenantAsync(
                tenantId, currentUserId, ct));
        }
        return results;
    }

    /// <summary>
    /// GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30)
    /// Shared rule + sequence-state writer used by the 3 new
    /// entity bootstraps. Mirrors the existing BusinessPartner
    /// bootstrap path verbatim so the persisted shape matches
    /// (Audit columns, ConcurrencyVersion=1, two SaveChanges
    /// for the Id assignment gap). Returns the persisted rule
    /// for the caller to build a result record.
    /// </summary>
    private async Task<(MasterDataCodeRule Rule, MasterDataCodeSequenceState State)>
        CreateRuleAndStateAsync(
            long tenantId,
            long? companyId,
            long? warehouseId,
            string entityType,
            string prefix,
            int sequenceLength,
            long startValue,
            long? currentUserId,
            CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var rule = new MasterDataCodeRule
        {
            TenantId = tenantId,
            CompanyId = companyId,
            WarehouseId = warehouseId,
            EntityType = entityType,
            SubType = null,
            Mode = MasterDataCodeMode.AutoEditable,
            Prefix = prefix,
            Separator = DefaultSeparator,
            SequenceLength = sequenceLength,
            StartValue = startValue,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = currentUserId,
            ModifiedAt = now,
            ModifiedBy = currentUserId,
            ConcurrencyVersion = 1,
        };
        _mdmDb.MasterDataCodeRules.Add(rule);
        await _mdmDb.SaveChangesAsync(ct);

        var state = new MasterDataCodeSequenceState
        {
            RuleId = rule.Id,
            TenantId = tenantId,
            CompanyId = companyId,
            WarehouseId = warehouseId,
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
            "MdmCodeRuleBootstrap: created {Entity} rule id={RuleId} + sequence state " +
            "for tenant={TenantId} company={CompanyId} warehouse={WarehouseId}.",
            entityType, rule.Id, tenantId, companyId, warehouseId);

        return (rule, state);
    }
}
