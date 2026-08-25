using System;
using GuliERP.DocumentKernel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GuliERP.DocumentKernel.IntegrationTests;

/// <summary>
/// Shared PG connection fixture for DocumentKernel integration tests.
/// Per brief §12 + §13:
/// - All tests target <c>gulierp_g2_003_test</c> (the canonical
///   active DB; NOT <c>gulierp_g2_001</c>, NOT
///   <c>gulierp_adminnet_poc</c>, NOT
///   <c>gulierp_g2_004_test</c>).
/// - PGPASSWORD must be supplied by the Operator via the
///   environment variable. If absent, <see cref="IsAvailable"/>
///   is <c>false</c> and all tests are skipped (NOT marked as
///   PASS; skipped = not-executed).
/// - Migration is applied via <see cref="EnsureSchemaAsync"/>
///   which is idempotent (re-runs no-op).
/// - NO destructive ops: no DROP, no TRUNCATE, no DELETE FROM
///   migration history, no RESET.
/// </summary>
public sealed class DocumentKernelConnectionFixture : IAsyncLifetime
{
    public const string RequiredDatabase = "gulierp_g2_003_test";
    public const string DefaultHost = "192.168.2.228";
    public const int DefaultPort = 5432;
    public const string DefaultUsername = "gulidata";

    /// <summary>
    /// <c>true</c> when PGPASSWORD is set and the fixture has a
    /// valid connection string. When <c>false</c>, all tests that
    /// use this fixture should call
    /// <c>Skip.IfNot(_fx.IsAvailable, "PGPASSWORD not set")</c>
    /// at the top of the test body.
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>
    /// Connection string. <c>null</c> when
    /// <see cref="IsAvailable"/> is <c>false</c>.
    /// </summary>
    public string? ConnectionString { get; }

    public DocumentKernelConnectionFixture()
    {
        var password = Environment.GetEnvironmentVariable("PGPASSWORD");
        if (string.IsNullOrEmpty(password))
        {
            IsAvailable = false;
            ConnectionString = null;
            return;
        }
        var host = Environment.GetEnvironmentVariable("GULIERP_PG_HOST") ?? DefaultHost;
        var port = Environment.GetEnvironmentVariable("GULIERP_PG_PORT") ?? DefaultPort.ToString();
        var database = Environment.GetEnvironmentVariable("GULIERP_PG_DATABASE") ?? RequiredDatabase;
        var username = Environment.GetEnvironmentVariable("GULIERP_PG_USERNAME") ?? DefaultUsername;
        ConnectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=false;Include Error Detail=true";
        IsAvailable = true;
    }

    public DocumentKernelDbContext CreateContext()
    {
        if (!IsAvailable || ConnectionString is null)
        {
            throw new InvalidOperationException(
                "DocumentKernelConnectionFixture is not available (PGPASSWORD not set). " +
                "Tests should call Skip.IfNot(_fx.IsAvailable) before using CreateContext().");
        }
        var options = new DbContextOptionsBuilder<DocumentKernelDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new DocumentKernelDbContext(options);
    }

    public async Task EnsureSchemaAsync()
    {
        if (!IsAvailable) return; // no-op when not available
        // The DOCKERNEL001 migration is registered in the
        // Infrastructure project. We invoke the EF migrator to
        // apply any pending migrations (idempotent; second run
        // is a no-op).
        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task ResetCounterScopeAsync(long tenantId, long companyId, int documentType, string periodKey)
    {
        // Helper for tests that need a clean counter state. NOT
        // a destructive op: only deletes the SPECIFIC scope row,
        // not the entire table.
        if (!IsAvailable) return;
        await using var ctx = CreateContext();
        await ctx.Database.ExecuteSqlRawAsync(
            @"DELETE FROM doc_kernel.document_number_counter
              WHERE ""TenantId"" = {0} AND ""CompanyId"" = {1}
                AND ""DocumentType"" = {2} AND ""PeriodKey"" = {3}",
            new object[] { tenantId, companyId, documentType, periodKey });
    }

    /// <summary>
    /// Read back the <c>LastGeneratedDocumentNo</c> column for a
    /// specific scope. G2-DOCNO-002 regression test helper: after
    /// the connection-in-progress fix, this column must reflect the
    /// actual post-increment rendered Number (not the stale
    /// placeholder).
    /// </summary>
    public async Task<string?> GetLastGeneratedDocumentNoAsync(
        long tenantId, long companyId, int documentType, string periodKey)
    {
        if (!IsAvailable) return null;
        await using var ctx = CreateContext();
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                @"SELECT ""LastGeneratedDocumentNo"" FROM doc_kernel.document_number_counter
                  WHERE ""TenantId"" = @p0 AND ""CompanyId"" = @p1
                    AND ""DocumentType"" = @p2 AND ""PeriodKey"" = @p3";
            cmd.Parameters.Add(new Npgsql.NpgsqlParameter("p0", tenantId));
            cmd.Parameters.Add(new Npgsql.NpgsqlParameter("p1", companyId));
            cmd.Parameters.Add(new Npgsql.NpgsqlParameter("p2", documentType));
            cmd.Parameters.Add(new Npgsql.NpgsqlParameter("p3", periodKey));
            var result = await cmd.ExecuteScalarAsync();
            return result is null or DBNull ? null : (string)result;
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    public async Task InitializeAsync()
    {
        // xUnit calls InitializeAsync once per collection. We
        // attempt to ensure the schema exists; if PG is not
        // available, this is a no-op.
        await EnsureSchemaAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
