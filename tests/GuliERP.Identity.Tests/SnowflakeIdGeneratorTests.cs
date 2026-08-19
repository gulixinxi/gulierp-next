using GuliERP.Foundation.Kernel;
using Xunit;

namespace GuliERP.Identity.Tests;

/// <summary>
/// Unit tests for the G2-003 long-snowflake ID generator (DEC-ID-014).
/// </summary>
public class SnowflakeIdGeneratorTests
{
    [Fact]
    public void NextId_DefaultConstructor_Returns_Positive_Long()
    {
        var gen = new SnowflakeIdGenerator();
        var id = gen.NextId();
        Assert.True(id > 0);
    }

    [Fact]
    public void NextId_Monotonically_Increases_Within_Same_Millisecond()
    {
        var gen = new SnowflakeIdGenerator();
        var first = gen.NextId();
        var second = gen.NextId();
        var third = gen.NextId();
        Assert.True(first < second, $"first={first} second={second}");
        Assert.True(second < third, $"second={second} third={third}");
    }

    [Fact]
    public void NextId_ThreadSafe_All_Ids_Are_Unique()
    {
        var gen = new SnowflakeIdGenerator();
        const int total = 5000;
        var ids = new long[total];
        Parallel.For(0, total, i => ids[i] = gen.NextId());
        var unique = new HashSet<long>(ids);
        Assert.Equal(total, unique.Count);
    }

    [Fact]
    public void NextId_Extracts_WorkerId_And_Sequence_Components()
    {
        var gen = new SnowflakeIdGenerator(workerId: 42);
        var id = gen.NextId();
        Assert.Equal(42L, SnowflakeIdGenerator.ToWorkerId(id));
        var sequence = SnowflakeIdGenerator.ToSequence(id);
        Assert.InRange(sequence, 0L, 4095L);
    }

    [Fact]
    public void Constructor_Rejects_OutOfRange_WorkerId()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnowflakeIdGenerator(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnowflakeIdGenerator(1024));
        // Boundary values are accepted.
        _ = new SnowflakeIdGenerator(0);
        _ = new SnowflakeIdGenerator(1023);
    }
}
