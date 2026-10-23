using System.Diagnostics.Metrics;
using System.Reflection;
using Aarkam.Kdouja.LSM.Primitives.Checksums;
using Aarkam.Kdouja.LSM.Primitives.Hashing;
using Aarkam.Kdouja.LSM.Primitives.IO;
using Aarkam.Kdouja.LSM.Primitives.Memory;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Diagnostics;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Diagnostics)]
public sealed class PrimitiveMetricsTests
{
    private static readonly Type MetricsType =
        typeof(Crc32Algorithm).Assembly.GetType("Aarkam.Kdouja.LSM.Primitives.Diagnostics.PrimitiveMetrics", throwOnError: true)!;

    [Theory]
    [InlineData("IoBytesRead", "Aarkam.Kdouja.LSM.Primitives.io.bytes_read", "bytes")]
    [InlineData("IoBytesWritten", "Aarkam.Kdouja.LSM.Primitives.io.bytes_written", "bytes")]
    [InlineData("BufferRented", "Aarkam.Kdouja.LSM.Primitives.memory.buffer_rented", "allocations")]
    [InlineData("BufferReturned", "Aarkam.Kdouja.LSM.Primitives.memory.buffer_returned", "returns")]
    [InlineData("HashComputed", "Aarkam.Kdouja.LSM.Primitives.hashing.hash_computed", "hashes")]
    [InlineData("ChecksumVerified", "Aarkam.Kdouja.LSM.Primitives.checksums.checksum_verified", "checksums")]
    public void CounterDefinitions_HaveExpectedMetadataAndMeterIdentity(
        string fieldName,
        string expectedInstrumentName,
        string expectedUnit)
    {
        FieldInfo? field = MetricsType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(field);

        var counter = Assert.IsType<Counter<long>>(field.GetValue(null));
        Assert.Equal(expectedInstrumentName, counter.Name);
        Assert.Equal(expectedUnit, counter.Unit);
        Assert.False(string.IsNullOrWhiteSpace(counter.Description));
        Assert.Equal("Aarkam.Kdouja.LSM.Primitives", counter.Meter.Name);
        Assert.Equal("1.0.0", counter.Meter.Version);
    }

    [Fact]
    public void ChecksumVerifiedCounter_IncrementsWhenCrc32Computed()
    {
        long recorded = 0;
        using var listener = CreateListener("Aarkam.Kdouja.LSM.Primitives.checksums.checksum_verified", val => Interlocked.Add(ref recorded, val));

        _ = Crc32Algorithm.Instance.Compute("metrics-test"u8);

        Assert.True(Interlocked.Read(ref recorded) >= 1);
    }

    [Fact]
    public void HashComputedCounter_IncrementsForFnv1aAndXxHash32()
    {
        long recorded = 0;
        using var listener = CreateListener("Aarkam.Kdouja.LSM.Primitives.hashing.hash_computed", val => Interlocked.Add(ref recorded, val));

        _ = Fnv1aHash32.Instance.Hash("hash-1"u8);
        _ = XxHash32Algorithm.Instance.Hash("hash-2"u8);

        Assert.True(Interlocked.Read(ref recorded) >= 2);
    }

    [Fact]
    public void BufferRentedAndReturnedCounters_IncrementDuringPooledBufferLifecycle()
    {
        long rented = 0;
        long returned = 0;
        using var rentListener = CreateListener("Aarkam.Kdouja.LSM.Primitives.memory.buffer_rented", val => Interlocked.Add(ref rented, val));
        using var returnListener = CreateListener("Aarkam.Kdouja.LSM.Primitives.memory.buffer_returned", val => Interlocked.Add(ref returned, val));

        using (var buffer = new PooledBuffer(64))
        {
            Assert.True(Interlocked.Read(ref rented) >= 1);
        }

        Assert.True(Interlocked.Read(ref returned) >= 1);
    }

    [Fact]
    public void IoBytesWrittenAndReadCounters_IncrementOnSequentialStreamOperations()
    {
        long written = 0;
        long read = 0;
        using var writeListener = CreateListener("Aarkam.Kdouja.LSM.Primitives.io.bytes_written", val => Interlocked.Add(ref written, val));
        using var readListener = CreateListener("Aarkam.Kdouja.LSM.Primitives.io.bytes_read", val => Interlocked.Add(ref read, val));

        using var ms = new MemoryStream();
        using (var writer = new SequentialFileWriter(ms, bufferSize: 64, leaveOpen: true))
        {
            writer.Write([1, 2, 3, 4, 5]);
            writer.WriteByte(6);
            writer.Flush();
        }

        ms.Position = 0;
        using (var reader = new SequentialFileReader(ms, bufferSize: 64, leaveOpen: true))
        {
            Span<byte> buf = stackalloc byte[5];
            _ = reader.Read(buf);
            _ = reader.ReadByte();
        }

        Assert.True(Interlocked.Read(ref written) >= 6);
        Assert.True(Interlocked.Read(ref read) >= 6);
    }

    private static MeterListener CreateListener(string instrumentName, Action<long> onMeasurement)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "Aarkam.Kdouja.LSM.Primitives" && instrument.Name == instrumentName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };

        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => onMeasurement(measurement));
        listener.Start();
        return listener;
    }
}
