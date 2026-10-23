using Aarkam.Kdouja.LSM.Primitives.Diagnostics;
using System.Buffers.Binary;
using System.IO.Hashing;

namespace Aarkam.Kdouja.LSM.Primitives.Hashing
{
    /// <summary>
    /// A high-performance, stateless concrete implementation of <see cref="IHashFunction"/> executing 
    /// the industry-standard, extreme-throughput 32-bit xxHash (xxHash32) algorithm loop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> While simpler byte-by-byte hashes like FNV-1a excel at mapping short string keys, 
    /// they degrade significantly when processing large byte payloads. xxHash32 processes data via parallel 32-bit block lanes, 
    /// maximizing modern CPU instruction pipelines. It is optimal for processing large chunks, such as checking uncompressed 
    /// SSTable data blocks, verifying WAL records, or processing multi-megabyte index frames.
    /// </para>
    /// <para>
    /// This implementation relies entirely on zero-allocation stack constraints (<c>stackalloc</c>) paired with span processing 
    /// structures to keep hot-path storage operations completely free of managed garbage collection churn.
    /// </para>
    /// </remarks>
    public sealed class XxHash32Algorithm : IHashFunction
    {
        /// <summary>
        /// The global thread-safe singleton instance allocated for shared engine hashing operations.
        /// </summary>
        /// <remarks>
        /// Reusing a pre-allocated singleton instance across thread execution loops eliminates unneeded garbage collection 
        /// object allocation cycles on highly active storage hot paths.
        /// </remarks>
        public static readonly XxHash32Algorithm Instance = new();

        /// <summary>
        /// Prevents direct external initialization of the <see cref="XxHash32Algorithm"/> instance.
        /// </summary>
        private XxHash32Algorithm() { }

        /// <summary>
        /// Gets the formal standardized name tracking identifier allocated for this algorithm variant.
        /// </summary>
        /// <value>The string literal identity label <c>"XXHash32"</c>.</value>
        public string Name => "XXHash32";

        /// <summary>
        /// Calculates a 32-bit hash value over a provided read-only byte sequence using a baseline initialization seed of $0$.
        /// </summary>
        /// <param name="data">The read-only byte slice holding the large binary data frame or payload block to hash.</param>
        /// <returns>A 32-bit unsigned integer representing the computed xxHash checksum footprint.</returns>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Continuously used to verify large storage page segments, checking data health 
        /// and preventing corrupt payloads from expanding into active internal memory systems.
        /// </remarks>
        public uint Hash(ReadOnlySpan<byte> data)
        {
            return Hash(data, 0);
        }

        /// <summary>
        /// Calculates a 32-bit hash value over a provided read-only byte sequence, substituting or extending execution 
        /// boundaries via an explicit initialization seed.
        /// </summary>
        /// <param name="data">The read-only byte slice holding the target database block payload sequence to hash.</param>
        /// <param name="seed">A custom initial hash state seed used to diversify the mathematical rotation paths of the parallel processing loop.</param>
        /// <returns>A 32-bit unsigned integer representing the computed seed-diversified hash footprint.</returns>
        /// <remarks>
        /// <para>
        /// <b>Advanced Storage Application:</b> Explicit seeding is vital for building multi-probe <b>Bloom Filters</b> or establishing 
        /// consistent hash distributions across clustered data sharding networks, ensuring that uniform bit layout vectors map cleanly.
        /// </para>
        /// <para>
        /// This routine safely tracks runtime telemetry by incrementing atomic diagnostic counters inside 
        /// the global <see cref="PrimitiveMetrics.HashComputed"/> collection block.
        /// </para>
        /// </remarks>
        public uint Hash(ReadOnlySpan<byte> data, uint seed)
        {
            PrimitiveMetrics.HashComputed.Add(1);

            // Carve a tiny 4-byte buffer frame straight out of the active thread stack to hold the computed output bytes
            Span<byte> hashBytes = stackalloc byte[4];

            // Delegate memory processing tasks to the highly optimized platform BCL utility
            XxHash32.Hash(data, hashBytes, (int)seed);

            // Read the stack bytes instantly using Little-Endian format alignment rules
            return BinaryPrimitives.ReadUInt32LittleEndian(hashBytes);
        }
    }
}