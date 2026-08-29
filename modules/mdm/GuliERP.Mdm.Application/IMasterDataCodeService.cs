namespace GuliERP.Mdm.Application;

public sealed record MasterDataCodeRequest(
    string EntityType,
    long TenantId,
    long? CompanyId,
    long? WarehouseId,
    string? ExplicitCode,
    string? SubType = null);

public sealed record MasterDataCodeResult(
    string Code,
    bool WasExplicit,
    long? RuleId,
    long? SequenceValue);

public interface IMasterDataCodeService
{
    Task<MasterDataCodeResult> GenerateNextAsync(
        MasterDataCodeRequest request,
        CancellationToken ct = default);

    Task<MasterDataCodeResult> PreviewAsync(
        MasterDataCodeRequest request,
        CancellationToken ct = default);
}
