namespace GuliERP.Mdm.Application;

/// <summary>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 — Wave 1.5
/// (2026-08-28). Default <c>MasterDataCodeRule</c> bootstrap for
/// Tenant-scoped <see cref="GuliERP.Mdm.Domain.Entities.MasterDataCodeMode.AutoEditable"/>
/// code generation.
///
/// <para>
/// Background: per the frozen
/// <c>GULIERP_MASTER_DATA_CODE_RULE_STANDARD_V1</c> §5, the
/// BusinessPartner entity is <b>AUTO_EDITABLE</b> and the default
/// pattern is <c>BP_000001</c> (Prefix=BP, Separator=_,
/// SequenceLength=6, Start=1, Scope=Tenant). The
/// <c>MasterDataCodeService</c> enforces that a rule must exist
/// for the requested (EntityType, TenantId, CompanyId, WarehouseId,
/// SubType) scope before it can generate. Without a default rule,
/// a fresh Tenant would fail to auto-generate BP codes
/// (<c>CodeRuleNotFound</c>).
/// </para>
///
/// <para>
/// <b>Design boundary (per brief §4)</b>: this service is
/// <i>NOT</i> a new bootstrap framework. It is a single-purpose,
/// idempotent MDM-internal service. Hook points:
/// <list type="bullet">
///   <item><b>Per-Tenant</b>: <see cref="EnsureDefaultBusinessPartnerRuleForTenantAsync"/>
///         can be called by the Identity EnterpriseBootstrap path
///         (via IServiceProvider lookup, to preserve the
///         one-way Mdm → Identity dependency).</item>
///   <item><b>Startup</b>: a thin IHostedService
///         (<c>MdmCodeRuleBootstrapStartupService</c>) calls
///         <see cref="EnsureDefaultBusinessPartnerRuleForAllTenantsAsync"/>
///         at app start so existing Tenants that were created
///         before this Wave 1.5 commit also get the default rule
///         on the next deployment (one query per Tenant; cheap).</item>
///   <item><b>Operator CLI / tests</b>: same
///         <see cref="EnsureDefaultBusinessPartnerRuleForAllTenantsAsync"/>
///         is the sanctioned call site for operator-side retry
///         and for the focused test fixture.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Idempotency contract</b>:
/// <list type="bullet">
///   <item>If a rule already exists for the scope
///         <c>(TenantId, CompanyId=null, WarehouseId=null,
///         EntityType="BusinessPartner", SubType=null)</c>, the call
///         is a no-op (does NOT mutate the rule, does NOT touch
///         the rule's Prefix / SequenceLength / StartValue, does
///         NOT bump ConcurrencyVersion).</item>
///   <item>If the rule exists but <c>IsActive=false</c>, the call
///         is still a no-op (operator must explicitly re-activate;
///         we do not silently flip the bit).</item>
///   <item>If the rule does not exist, a new rule + a new
///         <c>MasterDataCodeSequenceState</c> are created atomically
///         in a single SaveChanges call. The sequence is initialized
///         with <c>CurrentValue = rule.StartValue - 1</c> so the first
///         generate returns exactly <c>BP_000001</c>.</item>
///   <item>Existing master data Code values are NOT rewritten. The
///         bootstrap never touches the BusinessPartner / Warehouse
///         / Location / UOM / Item tables.</item>
/// </list>
/// </para>
/// </summary>
public interface IMdmCodeRuleBootstrapService
{
    /// <summary>
    /// Ensure the default BusinessPartner code rule + sequence
    /// state exist for one Tenant. Idempotent (see contract).
    /// </summary>
    /// <param name="tenantId">Target Tenant. The rule scope is
    /// <c>TenantId={tenantId}, CompanyId=null, WarehouseId=null</c>.</param>
    /// <param name="currentUserId">Optional operator user id for
    /// audit columns. <c>null</c> is acceptable for background
    /// / startup invocations.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<MdmCodeRuleBootstrapResult> EnsureDefaultBusinessPartnerRuleForTenantAsync(
        long tenantId,
        long? currentUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Ensure the default BusinessPartner code rule + sequence
    /// state exist for EVERY Tenant in the Identity store. Used
    /// at app startup and by the operator CLI / focused test
    /// fixture. Idempotent.
    /// </summary>
    /// <param name="currentUserId">Optional operator user id for
    /// audit columns. <c>null</c> for startup.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<MdmCodeRuleBootstrapResult>> EnsureDefaultBusinessPartnerRuleForAllTenantsAsync(
        long? currentUserId = null,
        CancellationToken ct = default);

    // ============================================================
    // GULIERP_MDM_FOUNDATION_REUSE_WAVE_V1 (2026-08-30)
    // Default code-rule bootstrap for the 3 reuse-wave entities:
    //   - Warehouse (Company scope)   — prefix WH, length 3
    //   - Location  (Warehouse scope) — prefix LOC, length 6
    //   - Item      (Tenant scope)    — prefix ITEM, length 6
    //
    // These are profile / registration-style additions only.
    // The MasterDataCodeService / MasterDataCodeRule engine
    // itself is FROZEN (GULIERP_MASTER_DATA_FOUNDATION_CLOSURE
    // _V1); we are not extending the engine, we are simply
    // ensuring that the existing engine has a default rule per
    // scope so the new entities can call
    // IMasterDataCodeService.GenerateNextAsync without each
    // object shipping its own counter infrastructure.
    // ============================================================

    /// <summary>
    /// Ensure the default Warehouse code rule + sequence state
    /// exist for one Company. Idempotent. Scope =
    /// (TenantId, CompanyId, WarehouseId=null, EntityType=
    /// "Warehouse"). Prefix=WH, Separator=_, SequenceLength=3.
    /// </summary>
    Task<MdmCodeRuleBootstrapResult> EnsureDefaultWarehouseRuleForCompanyAsync(
        long tenantId,
        long companyId,
        long? currentUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Ensure the default Warehouse code rule + sequence state
    /// exist for every Company in every active Tenant. Used at
    /// app startup and by operator CLI / focused test fixture.
    /// Idempotent.
    /// </summary>
    Task<IReadOnlyList<MdmCodeRuleBootstrapResult>> EnsureDefaultWarehouseRuleForAllCompaniesAsync(
        long? currentUserId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Ensure the default Location code rule + sequence state
    /// exist for one Warehouse. Idempotent. Scope =
    /// (TenantId, CompanyId, WarehouseId, EntityType="Location").
    /// Prefix=LOC, Separator=_, SequenceLength=6. Sequence is
    /// isolated per Warehouse so two Warehouses may both start
    /// at LOC_000001.
    /// </summary>
    Task<MdmCodeRuleBootstrapResult> EnsureDefaultLocationRuleForWarehouseAsync(
        long tenantId,
        long companyId,
        long warehouseId,
        long? currentUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Ensure the default Location code rule + sequence state
    /// exist for every Warehouse in every active Company in
    /// every active Tenant. Idempotent.
    /// </summary>
    Task<IReadOnlyList<MdmCodeRuleBootstrapResult>> EnsureDefaultLocationRuleForAllWarehousesAsync(
        long? currentUserId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Ensure the default Item code rule + sequence state exist
    /// for one Tenant. Idempotent. Scope =
    /// (TenantId, CompanyId=null, WarehouseId=null, EntityType=
    /// "Item"). Prefix=ITEM, Separator=_, SequenceLength=6.
    /// </summary>
    Task<MdmCodeRuleBootstrapResult> EnsureDefaultItemRuleForTenantAsync(
        long tenantId,
        long? currentUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Ensure the default Item code rule + sequence state exist
    /// for every active Tenant. Idempotent.
    /// </summary>
    Task<IReadOnlyList<MdmCodeRuleBootstrapResult>> EnsureDefaultItemRuleForAllTenantsAsync(
        long? currentUserId = null,
        CancellationToken ct = default);
}

public sealed record MdmCodeRuleBootstrapResult(
    long TenantId,
    bool RuleCreated,
    long? RuleId,
    bool SequenceStateCreated,
    string EntityType,
    string Prefix,
    string Separator,
    int SequenceLength,
    long StartValue)
{
    public bool AnyCreated => RuleCreated || SequenceStateCreated;
}
