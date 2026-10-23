using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;



namespace Aarkam.Kdouja.LSM.Primitives.Diagnostics
{
    /// <summary>
    /// Provides low-overhead, OpenTelemetry-compliant performance metrics counters tracking the internal structural activity of storage engine primitives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> High-throughput database systems operate at intense scales where traditional logging structures 
    /// degrade performance. This class provides lock-free telemetry hooks into hot data-shuffling tracks, tracking memory lease balances, 
    /// physical disk I/O velocity, and cryptographic checksum passes.
    /// </para>
    /// <para>
    /// By building upon the native .NET <see cref="Meter"/> API, these metrics integrate out-of-the-box with Prometheus, Grafana, 
    /// and Azure Monitor collectors with zero added application allocation footprint.
    /// </para>
    /// </remarks>
    internal static class PrimitiveMetrics
    {
        /// <summary>
        /// The central telemetry tracking engine context instance allocated for the primitives domain workspace.
        /// </summary>
        private static readonly Meter Meter = new("Aarkam.Kdouja.LSM.Primitives", "1.0.0");

        /// <summary>
        /// Tracks the cumulative total volume of raw bytes fetched via sequential file reading components.
        /// </summary>
        /// <remarks>
        /// <b>Telemetry Target:</b> Incremented by file channel iterators and page readers during physical disk reads, 
        /// serving as a direct index of physical storage read amplification and page cache misses.
        /// </remarks>
        public static readonly Counter<long> IoBytesRead = Meter.CreateCounter<long>(
            name: "Aarkam.Kdouja.LSM.Primitives.io.bytes_read",
            unit: "bytes",
            description: "Total number of bytes read via sequential binary file readers");

        /// <summary>
        /// Tracks the cumulative total volume of raw bytes committed via sequential file writing systems.
        /// </summary>
        /// <remarks>
        /// <b>Telemetry Target:</b> Triggered on the write hot path whenever data segments are explicitly flushed out of 
        /// the Write-Ahead Log (WAL) appenders or newly generated SSTable block layers into persistent storage handles.
        /// </remarks>
        public static readonly Counter<long> IoBytesWritten = Meter.CreateCounter<long>(
            name: "Aarkam.Kdouja.LSM.Primitives.io.bytes_written",
            unit: "bytes",
            description: "Total number of bytes written via sequential binary file writers");

        /// <summary>
        /// Tracks total buffer allocation operations rented directly out of active system memory pools.
        /// </summary>
        /// <remarks>
        /// <b>Telemetry Target:</b> Incremented whenever data processors lease a temporary working block out of an shared <c>ArrayPool{T}</c>. 
        /// Paired with <see cref="BufferReturned"/> to screen for memory tracking anomalies and leaked memory blocks.
        /// </remarks>
        public static readonly Counter<long> BufferRented = Meter.CreateCounter<long>(
            name: "Aarkam.Kdouja.LSM.Primitives.memory.buffer_rented",
            unit: "allocations",
            description: "Total number of buffer rent operations from BCL pools");

        /// <summary>
        /// Tracks total buffer return operations recycled back into active system memory pools.
        /// </summary>
        /// <remarks>
        /// <b>Telemetry Target:</b> Incremented upon disposal scopes when working blocks finish their execution lifecycles and safely release 
        /// back into the allocation pool, lowering garbage collection churn.
        /// </remarks>
        public static readonly Counter<long> BufferReturned = Meter.CreateCounter<long>(
            name: "Aarkam.Kdouja.LSM.Primitives.memory.buffer_returned",
            unit: "returns",
            description: "Total number of buffer return operations to BCL pools");

        /// <summary>
        /// Tracks the total count of hash allocations calculated across structural database assets.
        /// </summary>
        /// <remarks>
        /// <b>Telemetry Target:</b> Monitored on key processing paths to evaluate key indexing distribution costs, 
        /// MurmurHash evaluations, or cryptographic key stretching routines.
        /// </remarks>
        public static readonly Counter<long> HashComputed = Meter.CreateCounter<long>(
            name: "Aarkam.Kdouja.LSM.Primitives.hashing.hash_computed",
            unit: "hashes",
            description: "Total count of hash computations calculated");

        /// <summary>
        /// Tracks the total number of checksum verification runs executed against stored data frames.
        /// </summary>
        /// <remarks>
        /// <b>Telemetry Target:</b> Crucial for checking systemic data health. Incremented whenever an xxHash, CRC32, or similar checksum block 
        /// validation is parsed on a block fetch, verifying physical disk health and highlighting data corruption events.
        /// </remarks>
        public static readonly Counter<long> ChecksumVerified = Meter.CreateCounter<long>(
            name: "Aarkam.Kdouja.LSM.Primitives.checksums.checksum_verified",
            unit: "checksums",
            description: "Total count of checksum computations performed");
    }
}