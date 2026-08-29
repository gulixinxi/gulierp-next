using System.Collections.Concurrent;
using GuliERP.Foundation.Validation;
using GuliERP.Mdm.Application;
using GuliERP.Mdm.Application.Validation;
using GuliERP.Mdm.Domain.Entities;
using GuliERP.Mdm.Domain.Enums;
using GuliERP.Mdm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GuliERP.Mdm.Infrastructure.Mdm;

public sealed class MasterDataCodeService : IMasterDataCodeService
{
    private const int MaxEntityTypeLength = 64;
    private const int MaxCodeLength = 40;
    private const int MaxRetry = 3;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);

    private readonly MdmDbContext _db;
    private readonly ILogger<MasterDataCodeService> _logger;

    public MasterDataCodeService(
        MdmDbContext db,
        ILogger<MasterDataCodeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MasterDataCodeResult> PreviewAsync(
        MasterDataCodeRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!string.IsNullOrWhiteSpace(request.ExplicitCode))
        {
            var explicitCode = CanonicalizeExplicitCode(request);
            return new MasterDataCodeResult(explicitCode, WasExplicit: true, RuleId: null, SequenceValue: null);
        }

        var rule = await ResolveRuleAsync(request, ct);
        EnsureRuleCanGenerate(rule);
        var state = await ResolveSequenceStateAsync(rule, ct);
        var next = state.CurrentValue + 1;
        EnsureFits(next, rule);
        return new MasterDataCodeResult(Render(rule, next), WasExplicit: false, rule.Id, next);
    }

    public async Task<MasterDataCodeResult> GenerateNextAsync(
        MasterDataCodeRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!string.IsNullOrWhiteSpace(request.ExplicitCode))
        {
            var explicitCode = CanonicalizeExplicitCode(request);
            return new MasterDataCodeResult(explicitCode, WasExplicit: true, RuleId: null, SequenceValue: null);
        }

        var lockKey = ScopeKey(request);
        var gate = Locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            for (var attempt = 1; attempt <= MaxRetry; attempt++)
            {
                var rule = await ResolveRuleAsync(request, ct);
                EnsureRuleCanGenerate(rule);
                var state = await ResolveSequenceStateAsync(rule, ct);
                var next = state.CurrentValue + 1;
                EnsureFits(next, rule);
                var code = Render(rule, next);

                state.CurrentValue = next;
                state.LastGeneratedCode = code;
                state.ModifiedAt = DateTimeOffset.UtcNow;
                state.ConcurrencyVersion += 1;

                try
                {
                    await _db.SaveChangesAsync(ct);
                    return new MasterDataCodeResult(code, WasExplicit: false, rule.Id, next);
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxRetry)
                {
                    _db.ChangeTracker.Clear();
                }
            }
        }
        finally
        {
            gate.Release();
        }

        throw new MdmValidationException(
            MdmErrorCodes.CodeGenerationConflict,
            "Master-data code generation conflicted after retry.");
    }

    private static string CanonicalizeExplicitCode(MasterDataCodeRequest request)
    {
        var code = MdmBusinessPartnerService.CanonicalizeCode(
            request.ExplicitCode!,
            MaxCodeLength,
            nameof(request.ExplicitCode));

        var context = CodeValidationContextExtensions.ForMdm(
            request.EntityType,
            request.TenantId,
            request.CompanyId);
        var result = MasterDataCodeValidator.Validate(code, context);
        if (!result.IsValid)
        {
            throw new MdmValidationException(result.Failure!.ErrorCode, result.Failure.Message);
        }
        return code;
    }

    private async Task<MasterDataCodeRule> ResolveRuleAsync(
        MasterDataCodeRequest request,
        CancellationToken ct)
    {
        var entityType = NormalizeEntityType(request.EntityType);
        var subType = MdmBusinessPartnerService.ValidateOptionalText(request.SubType, MaxEntityTypeLength, nameof(request.SubType));
        var rule = await _db.MasterDataCodeRules
            .FirstOrDefaultAsync(x =>
                x.TenantId == request.TenantId
                && x.CompanyId == request.CompanyId
                && x.WarehouseId == request.WarehouseId
                && x.EntityType == entityType
                && x.SubType == subType,
                ct);
        if (rule is null)
        {
            throw new MdmValidationException(
                MdmErrorCodes.CodeRuleNotFound,
                $"Master-data code rule for {entityType} was not found.");
        }
        return rule;
    }

    private async Task<MasterDataCodeSequenceState> ResolveSequenceStateAsync(
        MasterDataCodeRule rule,
        CancellationToken ct)
    {
        var state = await _db.MasterDataCodeSequenceStates
            .FirstOrDefaultAsync(x => x.RuleId == rule.Id, ct);
        if (state is not null) return state;

        var now = DateTimeOffset.UtcNow;
        state = new MasterDataCodeSequenceState
        {
            RuleId = rule.Id,
            TenantId = rule.TenantId,
            CompanyId = rule.CompanyId,
            WarehouseId = rule.WarehouseId,
            CurrentValue = rule.StartValue - 1,
            CreatedAt = now,
            ModifiedAt = now,
            ConcurrencyVersion = 1,
        };
        _db.MasterDataCodeSequenceStates.Add(state);
        await _db.SaveChangesAsync(ct);
        return state;
    }

    private static void EnsureRuleCanGenerate(MasterDataCodeRule rule)
    {
        if (!rule.IsActive)
        {
            throw new MdmValidationException(
                MdmErrorCodes.CodeRuleInactive,
                $"Master-data code rule for {rule.EntityType} is inactive.");
        }
        if (rule.Mode == MasterDataCodeMode.Manual)
        {
            throw new MdmValidationException(
                MdmErrorCodes.CodeRequired,
                $"Master-data code rule for {rule.EntityType} requires an explicit code.");
        }
    }

    private static void EnsureFits(long next, MasterDataCodeRule rule)
    {
        var max = (long)Math.Pow(10, rule.SequenceLength) - 1;
        if (next > max)
        {
            throw new MdmValidationException(
                MdmErrorCodes.CodeSequenceExhausted,
                $"Master-data code sequence for {rule.EntityType} is exhausted.");
        }
    }

    private static string Render(MasterDataCodeRule rule, long value)
    {
        var sequence = value.ToString().PadLeft(rule.SequenceLength, '0');
        return string.Concat(rule.Prefix, rule.Separator, sequence);
    }

    internal static string NormalizeEntityType(string entityType)
    {
        var normalized = MdmBusinessPartnerService.ValidateRequiredText(
            entityType,
            MaxEntityTypeLength,
            nameof(entityType));
        return normalized;
    }

    private static string ScopeKey(MasterDataCodeRequest request) =>
        string.Join('|',
            request.TenantId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.CompanyId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
            request.WarehouseId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
            NormalizeEntityType(request.EntityType),
            request.SubType ?? "");
}
