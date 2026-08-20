using Microsoft.Extensions.Logging;

namespace GuliERP.Identity.Bootstrap;

/// <summary>
/// G2-004V1R3 — a tiny ILoggerProvider that writes every log
/// record to <see cref="Console.Error"/>. Used by the bootstrap
/// tool so stdout is reserved for the final machine-readable
/// JSON result. The default Microsoft.Extensions.Logging.Console
/// SimpleConsoleFormatter writes to stdout, which broke the
/// PowerShell wrapper's <c>$stdout | ConvertFrom-Json</c> call
/// when info logs were mixed in.
///
/// Contract:
///   - stdout = final JSON object (1 line)
///   - stderr = human-readable log lines
///
/// The provider is registered via
/// <c>services.AddLogging(b =&gt; b.AddProvider(new StderrLoggerProvider()))</c>.
/// The log record format is a single line:
/// <c>HH:mm:ss [LEVEL] category: message</c>.
/// Exceptions are appended as a second line.
/// </summary>
public sealed class StderrLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new StderrLogger(categoryName);

    public void Dispose() { }

    private sealed class StderrLogger : ILogger
    {
        private readonly string _category;

        public StderrLogger(string category) { _category = category; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) { return; }
            var ts = DateTimeOffset.Now.ToString("HH:mm:ss");
            var level = logLevel.ToString().ToUpperInvariant();
            Console.Error.WriteLine($"{ts} [{level}] {_category}: {formatter(state, exception)}");
            if (exception != null)
            {
                Console.Error.WriteLine($"  {exception.GetType().Name}: {exception.Message}");
            }
        }
    }
}
