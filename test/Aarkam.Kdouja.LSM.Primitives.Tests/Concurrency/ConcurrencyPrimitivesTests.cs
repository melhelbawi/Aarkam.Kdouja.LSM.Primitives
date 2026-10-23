using Aarkam.Kdouja.LSM.Primitives.Concurrency;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Concurrency;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Concurrency)]
public sealed class ConcurrencyPrimitivesTests
{
    [Fact]
    public void SequenceNumber_DefaultConstructor_StartsAtZero()
    {
        var seq = new SequenceNumber();
        Assert.Equal(0L, seq.Value);
        Assert.Equal("0", seq.ToString());
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(42L)]
    [InlineData(1_000_000L)]
    [InlineData(long.MaxValue - 10)]
    public void SequenceNumber_CustomInitialValue_SetsStartingValue(long initial)
    {
        var seq = new SequenceNumber(initial);
        Assert.Equal(initial, seq.Value);
        Assert.Equal(initial.ToString(), seq.ToString());
    }

    [Fact]
    public void SequenceNumber_Increment_IncrementsMonotonically()
    {
        var seq = new SequenceNumber(10);

        Assert.Equal(11L, seq.Increment());
        Assert.Equal(12L, seq.Increment());
        Assert.Equal(13L, seq.Increment());
        Assert.Equal(13L, seq.Value);
    }

    [Fact]
    public void SequenceNumber_SetValue_UpdatesAtomically()
    {
        var seq = new SequenceNumber(5);
        seq.Value = 500;

        Assert.Equal(500L, seq.Value);
    }

    [Fact]
    public void SequenceNumber_CompareAndSwap_UpdatesOnlyWhenExpectedMatches()
    {
        var seq = new SequenceNumber(100);

        Assert.False(seq.CompareAndSwap(expected: 99, update: 200));
        Assert.Equal(100L, seq.Value);

        Assert.True(seq.CompareAndSwap(expected: 100, update: 200));
        Assert.Equal(200L, seq.Value);
    }

    [Fact]
    public async Task SequenceNumber_ConcurrentIncrements_ProducesExactTotal()
    {
        const int workers = 8;
        const int incrementsPerWorker = 1000;
        var seq = new SequenceNumber(0);

        Task[] tasks = Enumerable.Range(0, workers).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < incrementsPerWorker; i++)
            {
                seq.Increment();
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(workers * incrementsPerWorker, seq.Value);
    }

    [Fact]
    public void ReadWriteVersionGuard_WhenNoActiveReaders_ReturnsDefaultVersion()
    {
        var guard = new ReadWriteVersionGuard();

        Assert.Equal(42L, guard.GetMinActiveVersion(42L));
        Assert.Equal(100L, guard.GetMinActiveVersion(100L));
    }

    [Fact]
    public void ReadWriteVersionGuard_MultipleActiveReaders_TracksMinimumAndUpdatesOnDispose()
    {
        var guard = new ReadWriteVersionGuard();

        IDisposable r1 = guard.AcquireReader(50);
        IDisposable r2 = guard.AcquireReader(20);
        IDisposable r3 = guard.AcquireReader(80);

        Assert.Equal(20L, guard.GetMinActiveVersion(100L));

        r2.Dispose();
        Assert.Equal(50L, guard.GetMinActiveVersion(100L));

        // Disposing a second time is idempotent
        r2.Dispose();
        Assert.Equal(50L, guard.GetMinActiveVersion(100L));

        r1.Dispose();
        Assert.Equal(80L, guard.GetMinActiveVersion(100L));

        r3.Dispose();
        Assert.Equal(100L, guard.GetMinActiveVersion(100L));
    }

    [Fact]
    public void ReadWriteVersionGuard_WhenActiveReadersExist_ReturnsMinimumReaderVersionRegardlessOfDefault()
    {
        var guard = new ReadWriteVersionGuard();
        using (IDisposable reader = guard.AcquireReader(500))
        {
            Assert.Equal(500L, guard.GetMinActiveVersion(100L));
            Assert.Equal(500L, guard.GetMinActiveVersion(600L));
        }

        Assert.Equal(100L, guard.GetMinActiveVersion(100L));
    }

    [Fact]
    public async Task ReadWriteVersionGuard_ConcurrentAcquireAndDispose_CleansUpAllRegistrations()
    {
        var guard = new ReadWriteVersionGuard();

        Task[] tasks = Enumerable.Range(1, 32).Select(v => Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                using IDisposable token = guard.AcquireReader(v + i);
                long min = guard.GetMinActiveVersion(10_000);
                Assert.True(min <= v + i);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(9999L, guard.GetMinActiveVersion(9999L));
    }

    [Fact]
    public void ReadWriteVersionGuard_AcquireSnapshot_PinsCommittedVersion()
    {
        var seq = new SequenceNumber(42);
        var guard = new ReadWriteVersionGuard(seq);

        Assert.Equal(42L, guard.GetMinActiveVersion());

        using (ReadWriteVersionGuard.Snapshot s1 = guard.Acquire())
        {
            Assert.Equal(42L, s1.Version);

            // Writer advances committed sequence
            seq.Increment(); // now 43
            seq.Increment(); // now 44

            using (ReadWriteVersionGuard.Snapshot s2 = guard.Acquire())
            {
                Assert.Equal(44L, s2.Version);

                // Min active version should still be 42 because s1 is alive
                Assert.Equal(42L, guard.GetMinActiveVersion());
            }

            // After s2 is disposed, min active version should still be 42
            Assert.Equal(42L, guard.GetMinActiveVersion());
        }

        // Once s1 is disposed, min active version jumps to current committed (44)
        Assert.Equal(44L, guard.GetMinActiveVersion());
    }
}
