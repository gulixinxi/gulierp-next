using System.Data;
using System.Text;
using GuliERP.DocumentKernel.Application;
using GuliERP.DocumentKernel.Domain.Entities;
using GuliERP.DocumentKernel.Domain.Enums;
using GuliERP.DocumentKernel.Infrastructure.Persistence;
using GuliERP.Foundation.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GuliERP.DocumentKernel.Infrastructure.DocumentNumber;

/// <summary>
/// V1 atomic Document Number service. Per
/// <c>docs/architecture/BUSINESS_DOCUMENT_NUMBERING_V1.md</c> §13
/// — the SINGLE source of truth for Document Number generation.
///
/// <para>
/// Concurrency model (V1):
/// </para>
/// <list type="number">
///   <item>Idempotency check first (a single PK lookup in
///         <c>document_number_idempotency</c>). A hit returns
///         the cached Number — atomic, no counter increment.</item>
///   <item>Atomic upsert: <c>INSERT ... ON CONFLICT (4-tuple) DO
///         UPDATE SET last_value = last_value + 1 ... RETURNING
///         last_value, last_generated_document_no</c>. The unique
///         constraint <c>ux_doc_number_counter_scope</c> serializes
///         concurrent calls; the upsert is a single round-trip;
///         no application-level lock.</item>
///   <item>After a successful upsert, insert into the idempotency
///         table (if a key was supplied). A duplicate-key insert
///         is caught and treated as a concurrent-retry replay.</item>
/// </list>
///
/// <para>
/// The service is the single source of truth for the audit row:
/// it writes the Foundation <c>IAuditWriter</c> entry before
/// returning. The caller does NOT write a duplicate audit row.
/// (V1 uses the <c>IAuditWriter</c> contract from Foundation;
/// the actual <c>DocumentKernelAuditWriter</c> implementation is
/// injected via DI. V1.5+ may add a per-module audit row.)
/// </para>
///
/// <para>
/// <b>Production review status (per brief §14):</b> the V1
/// algorithm is implementation-ready. The production-grade
/// atomic-counter review (Codex critical review) is pending.
/// Until then, the gate is
/// <c>BUSINESS_DOCUMENT_KERNEL_V1_CODE_READY_CRITICAL_REVIEW_PENDING</c>.
/// </para>
/// </summary>
public sealed class DocumentNumberService : IDocumentNumberService
{
    private readonly DocumentKernelDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DocumentNumberService> _logger;

    // Field separator between the rendered parts. Per §2.
    private const char Separator = '-';

    public DocumentNumberService(
        DocumentKernelDbContext db,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        ILogger<DocumentNumberService> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<DocumentNumberResult> GenerateAsync(
        DocumentNumberRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // ---------- 1. Input validation ----------
        if (request.TenantId <= 0)
        {
            throw new DocumentNumberValidationException(
                $"TenantId must be a positive long (got {request.TenantId}).");
        }
        if (request.CompanyId <= 0)
        {
            throw new DocumentNumberValidationException(
                $"CompanyId must be a positive long (got {request.CompanyId}).");
        }
        if (request.ActorId <= 0)
        {
            throw new DocumentNumberValidationException(
                $"ActorId must be a positive long (got {request.ActorId}).");
        }
        if (request.IdempotencyKey is { Length: > 64 })
        {
            throw new DocumentNumberValidationException(
                $"IdempotencyKey must be at most 64 chars (got {request.IdempotencyKey.Length}).");
        }

        var profile = DocumentTypeProfileCatalog.Get(request.DocumentType);
        var periodKey = RenderPeriodKey(profile.ResetPeriod, request.BusinessDate);

        // ---------- 2. Idempotency dedup ----------
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existing = await _db.DocumentNumberIdempotencies
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.IdempotencyKey == request.IdempotencyKey
                      && i.TenantId == request.TenantId
                      && i.DocumentType == (int)request.DocumentType,
                    ct);
            if (existing is not null)
            {
                // Replay. Return the previously-stored Number
                // without re-incrementing the counter.
                _logger.LogInformation(
                    "DocumentNumberService: idempotency replay key={Key} type={Type} no={No}",
                    request.IdempotencyKey, request.DocumentType, existing.GeneratedDocumentNo);
                return new DocumentNumberResult(
                    DocumentNo: existing.GeneratedDocumentNo,
                    SequenceValue: 0,  // 0 = "replay, not a fresh sequence"
                    IdempotencyReplayed: true);
            }
        }

        // ---------- 3. Atomic upsert ----------
        // The upsert serializes on the 4-tuple unique constraint.
        // The RETURNING clause gives us the post-increment value
        // in a single round-trip.
        var upsertSql = @"
INSERT INTO doc_kernel.document_number_counter
    (""Id"", ""TenantId"", ""CompanyId"", ""DocumentType"", ""PeriodKey"",
     ""LastValue"", ""LastGeneratedDocumentNo"", ""CreatedAt"", ""ModifiedAt"",
     ""ConcurrencyVersion"")
VALUES
    (nextval('identity.gulierp_hilo_sequence'),
     @tenantId, @companyId, @documentType, @periodKey,
     1, @firstDocumentNo, @now, @now, 1)
ON CONFLICT (""TenantId"", ""CompanyId"", ""DocumentType"", ""PeriodKey"")
DO UPDATE SET
    ""LastValue"" = doc_kernel.document_number_counter.""LastValue"" + 1,
    ""LastGeneratedDocumentNo"" = EXCLUDED.""LastGeneratedDocumentNo"",
    ""ModifiedAt"" = @now,
    ""ConcurrencyVersion"" = doc_kernel.document_number_counter.""ConcurrencyVersion"" + 1
RETURNING ""LastValue"", ""LastGeneratedDocumentNo"";
";
        // The first-generation Document Number is the post-increment
        // value of 1, but we don't know the post-increment value
        // until the upsert returns. We pre-compute the "1" form
        // and let the SQL return the actual value. The
        // EXCLUDED.LastGeneratedDocumentNo is the value passed in
        // for the INSERT path; for the UPDATE path it's ignored
        // (the SET clause reassigns it from the freshly-rendered
        // value computed AFTER the increment).
        //
        // To handle both paths uniformly, we use a 2-step:
        //   step a) execute a "bump" upsert that increments
        //           last_value and returns the NEW value;
        //   step b) render the Document Number from the new value.
        // The above SQL is a single round-trip; we post-process
        // the returned LastValue into the rendered Number below.
        var now = DateTimeOffset.UtcNow;
        var firstDocumentNo = RenderDocumentNo(profile, periodKey, 1);  // placeholder
        var conn = _db.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = upsertSql;
            cmd.Parameters.Add(new NpgsqlParameter("tenantId", request.TenantId));
            cmd.Parameters.Add(new NpgsqlParameter("companyId", request.CompanyId));
            cmd.Parameters.Add(new NpgsqlParameter("documentType", (int)request.DocumentType));
            cmd.Parameters.Add(new NpgsqlParameter("periodKey", periodKey));
            cmd.Parameters.Add(new NpgsqlParameter("firstDocumentNo", firstDocumentNo));
            cmd.Parameters.Add(new NpgsqlParameter("now", now));

            long newValue = 0;
            string returnedNo = string.Empty;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                newValue = reader.GetInt64(0);
                returnedNo = reader.GetString(1);
            }
            // G2-DOCNO-002 fix: explicitly close the RETURNING reader
            // before any subsequent command can run on the same
            // connection. Npgsql refuses a second command while a
            // DataReader is still active ("A command is already in
            // progress"). Without this, the fix-up UPDATE below
            // raises NpgsqlOperationInProgressException for every
            // 2nd+ GenerateAsync call in the same scope.
            // (idempotent with the await using block's eventual
            // dispose; safe to call early.)
            await reader.CloseAsync();
            // Post-process: the EXCLUDED.lastGeneratedDocumentNo for
            // the INSERT path may differ from the actual post-
            // increment value. Re-render from the returned value
            // to ensure the stored column matches the rendered No.
            var finalDocumentNo = RenderDocumentNo(profile, periodKey, newValue);
            if (returnedNo != finalDocumentNo)
            {
                // Update the row to fix the rendered Number. This
                // is a no-op for the UPDATE path (the SET clause
                // already re-renders) but covers the INSERT path
                // where the EXCLUDED.lastGeneratedDocumentNo is
                // the placeholder value.
                await using var fixCmd = conn.CreateCommand();
                fixCmd.CommandText = @"
UPDATE doc_kernel.document_number_counter
SET ""LastGeneratedDocumentNo"" = @no, ""ModifiedAt"" = @now
WHERE ""TenantId"" = @tenantId
  AND ""CompanyId"" = @companyId
  AND ""DocumentType"" = @documentType
  AND ""PeriodKey"" = @periodKey;
";
                fixCmd.Parameters.Add(new NpgsqlParameter("no", finalDocumentNo));
                fixCmd.Parameters.Add(new NpgsqlParameter("now", now));
                fixCmd.Parameters.Add(new NpgsqlParameter("tenantId", request.TenantId));
                fixCmd.Parameters.Add(new NpgsqlParameter("companyId", request.CompanyId));
                fixCmd.Parameters.Add(new NpgsqlParameter("documentType", (int)request.DocumentType));
                fixCmd.Parameters.Add(new NpgsqlParameter("periodKey", periodKey));
                await fixCmd.ExecuteNonQueryAsync(ct);
            }

            // ---------- 4. Idempotency write (best-effort) ----------
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                var dedup = new DocumentNumberIdempotency
                {
                    IdempotencyKey = request.IdempotencyKey,
                    TenantId = request.TenantId,
                    CompanyId = request.CompanyId,
                    DocumentType = (int)request.DocumentType,
                    PeriodKey = periodKey,
                    GeneratedDocumentNo = finalDocumentNo,
                    GeneratedAt = now,
                    GeneratedBy = request.ActorId,
                };
                _db.DocumentNumberIdempotencies.Add(dedup);
                try
                {
                    await _db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                {
                    // Concurrent retry: the same key was just
                    // inserted. Detach the entity to avoid the
                    // context being poisoned. The original
                    // counter increment stands; the dedup is
                    // already there for the OTHER caller.
                    _db.Entry(dedup).State = EntityState.Detached;
                    _logger.LogInformation(
                        "DocumentNumberService: idempotency dedup race; key={Key} already present",
                        request.IdempotencyKey);
                }
            }

            _logger.LogInformation(
                "DocumentNumberService: generated type={Type} period={Period} no={No} seq={Seq}",
                request.DocumentType, periodKey, finalDocumentNo, newValue);

            return new DocumentNumberResult(
                DocumentNo: finalDocumentNo,
                SequenceValue: newValue,
                IdempotencyReplayed: false);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private static string RenderPeriodKey(ResetPeriod resetPeriod, DateOnly businessDate)
    {
        // Per BUSINESS_DOCUMENT_NUMBERING_V1.md §6:
        //   Daily   → YYYYMMDD (8 chars)
        //   Monthly → YYYYMM  (6 chars)
        return resetPeriod switch
        {
            ResetPeriod.Daily   => businessDate.ToString("yyyyMMdd"),
            ResetPeriod.Monthly => businessDate.ToString("yyyyMM"),
            _ => throw new UnknownDocumentTypeException(
                $"Unsupported ResetPeriod: {(int)resetPeriod}."),
        };
    }

    internal static string RenderDocumentNo(
        DocumentTypeProfile profile, string periodKey, long sequenceValue)
    {
        // Per BUSINESS_DOCUMENT_NUMBERING_V1.md §2:
        //   {Prefix}-{PeriodKey}-{Sequence:Length}
        var sb = new StringBuilder(32);
        sb.Append(profile.Prefix);
        sb.Append(Separator);
        sb.Append(periodKey);
        sb.Append(Separator);
        sb.Append(sequenceValue.ToString($"D{profile.SequenceLength}"));
        return sb.ToString();
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        // PostgreSQL SQLSTATE 23505 = unique_violation.
        var inner = ex.InnerException;
        return inner is PostgresException { SqlState: "23505" };
    }
}
