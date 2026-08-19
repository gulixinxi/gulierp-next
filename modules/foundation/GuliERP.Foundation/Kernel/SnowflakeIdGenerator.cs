using System.Threading;

namespace GuliERP.Foundation.Kernel;

/// <summary>
/// G2-003 ID generator per DEC-ID-014 (long snowflake, 8 bytes, bigint in
/// PostgreSQL). Single shared generator for ALL GuliERP entities; per
/// brief §十四: "不要多个实体各自发明 ID strategy."
///
/// <para>
/// Layout (Twitter-style 64-bit snowflake, V1 single-host):
/// <code>
/// |--- 41 bits timestamp (ms) ---|--- 10 bits worker ---|--- 12 bits seq ---|
/// </code>
/// <list type="bullet">
///   <item>41 bits of ms since custom epoch (2026-01-01T00:00:00Z) — ~69 years.</item>
///   <item>10 bits of worker (machine id) — up to 1024 workers. V1 single-host
///         uses worker 0; future multi-host deployments set this via
///         configuration.</item>
///   <item>12 bits of sequence within the same ms — up to 4096 ids/ms/worker.</item>
/// </list>
/// </para>
///
/// <para>
/// All values are <see cref="long"/> (signed 64-bit). The sign bit is always 0
/// for the next ~69 years; EF Core / Npgsql maps this to PostgreSQL
/// <c>bigint</c> natively.
/// </para>
/// </summary>
public sealed class SnowflakeIdGenerator
{
    // Custom epoch: 2026-01-01T00:00:00Z. Gives ~69 years of usable ids
    // before the 41-bit timestamp overflows. The choice of epoch is
    // intentionally inside the G2-001 baseline era so that early-stage
    // ids are visually distinguishable from random 64-bit numbers.
    private static readonly DateTimeOffset Epoch =
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Bit widths
    private const int WorkerIdBits  = 10;
    private const int SequenceBits  = 12;

    // Max values
    private const long MaxWorkerId  = -1L ^ (-1L << WorkerIdBits);   // 1023
    private const long MaxSequence   = -1L ^ (-1L << SequenceBits);   // 4095

    // Bit shifts
    private const int WorkerIdShift = SequenceBits;                   // 12
    private const int TimestampShift = SequenceBits + WorkerIdBits;    // 22

    private readonly long _workerId;
    private long _lastTimestamp = -1L;
    private long _sequence;
    private readonly object _lock = new();

    /// <summary>
    /// Construct a snowflake generator. <paramref name="workerId"/> must be
    /// between 0 and 1023 inclusive; for V1 single-host deployments, pass 0.
    /// </summary>
    public SnowflakeIdGenerator(long workerId = 0)
    {
        if (workerId < 0 || workerId > MaxWorkerId)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workerId),
                workerId,
                $"workerId must be between 0 and {MaxWorkerId} (inclusive).");
        }
        _workerId = workerId;
    }

    /// <summary>
    /// Generate the next id. Thread-safe; the in-process lock is the
    /// only synchronization required. Per-millisecond throughput is
    /// 4096 ids / ms (or 4,096,000 ids / second).
    /// </summary>
    public long NextId()
    {
        lock (_lock)
        {
            var now = CurrentTimestamp();

            if (now == _lastTimestamp)
            {
                // Same millisecond: increment the sequence. If the
                // sequence is exhausted, spin until the next millisecond.
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                {
                    // Sequence overflow at the same ms: wait for the
                    // next millisecond.
                    while (now <= _lastTimestamp)
                    {
                        now = CurrentTimestamp();
                    }
                }
            }
            else
            {
                // New millisecond: reset the sequence.
                _sequence = 0;
            }

            _lastTimestamp = now;

            // Layout: 41 bits ts | 10 bits worker | 12 bits seq
            return (now << TimestampShift)
                 | (_workerId << WorkerIdShift)
                 | _sequence;
        }
    }

    private static long CurrentTimestamp()
    {
        var elapsed = DateTimeOffset.UtcNow - Epoch;
        if (elapsed.TotalMilliseconds < 0)
        {
            throw new InvalidOperationException(
                "Snowflake clock is before the GuliERP epoch (2026-01-01T00:00:00Z). " +
                "Check the system clock; do not generate ids with a negative timestamp.");
        }
        return (long)elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// Extract the timestamp component (ms since the GuliERP epoch) from a
    /// snowflake id. Used by audit + diagnostic tooling.
    /// </summary>
    public static DateTimeOffset ToTimestamp(long id)
    {
        var ts = id >> TimestampShift;
        return Epoch.AddMilliseconds(ts);
    }

    /// <summary>
    /// Extract the worker id component from a snowflake id.
    /// </summary>
    public static long ToWorkerId(long id) => (id >> WorkerIdShift) & MaxWorkerId;

    /// <summary>
    /// Extract the per-millisecond sequence component from a snowflake id.
    /// </summary>
    public static long ToSequence(long id) => id & MaxSequence;
}
