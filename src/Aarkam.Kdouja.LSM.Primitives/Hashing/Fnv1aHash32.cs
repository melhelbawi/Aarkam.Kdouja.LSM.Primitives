using Aarkam.Kdouja.LSM.Primitives.Diagnostics;

namespace Aarkam.Kdouja.LSM.Primitives.Hashing
{
    /// <summary>
    /// A high-performance, stateless concrete implementation of <see cref="IHashFunction"/> executing 
    /// the non-cryptographic 32-bit Fowler–Noll–Vo (FNV-1a) hash algorithm loop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In low-latency Key-Value engines, internal mapping layers (such as in-memory row indexing, 
    /// page cache bucket assignment, and partition routing) require high-speed hashing over short binary keys. 
    /// Cryptographic hashes (e.g., SHA-256) introduce unnecessary CPU instruction overhead. FNV-1a provides rapid hash generation 
    /// with an exceptionally low instruction count per byte.
    /// </para>
    /// <para>
    /// FNV-1a is optimized for short string or integer keys, shifting bits aggressively via an iterative XOR-and-multiply scheme 
    /// to maximize the mathematical <i>avalanche effect</i> and minimize collision risk inside structural tables.
    /// </para>
    /// </remarks>
    public sealed class Fnv1aHash32 : IHashFunction
    {
        /// <summary>
        /// The 32-bit FNV prime value adjustment parameter ($2^{24} + 2^8 + 0x93 = 16,777,619$) applied to shift bits across storage boundaries.
        /// </summary>
        private const uint FnvPrime = 16777619;

        /// <summary>
        /// The 32-bit initialization offset basis value vector ($2166136261$) used to seed the initial hashing register state.
        /// </summary>
        private const uint FnvOffsetBasis = 2166136261;

        /// <summary>
        /// The global thread-safe singleton instance allocated for shared engine hashing operations.
        /// </summary>
        /// <remarks>
        /// Using a pre-allocated singleton avoids unneeded garbage collection churn on highly active hot execution paths.
        /// </remarks>
        public static readonly Fnv1aHash32 Instance = new();

        /// <summary>
        /// Prevents direct external initialization of the <see cref="Fnv1aHash32"/> instance.
        /// </summary>
        private Fnv1aHash32() { }

        /// <summary>
        /// Gets the formal standardized name tracking identifier allocated for this algorithm variant.
        /// </summary>
        /// <value>The string literal identity label <c>"FNV-1a-32"</c>.</value>
        public string Name => "FNV-1a-32";

        /// <summary>
        /// Calculates a 32-bit hash value over a provided read-only byte sequence using the default FNV offset basis.
        /// </summary>
        /// <param name="data">The read-only byte slice holding the key or payload sequence to hash.</param>
        /// <returns>A 32-bit unsigned integer representing the computed hash coordinate footprint.</returns>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Continuously used to determine entry positioning offsets when mapping 
        /// database keys directly into localized internal memory lookups.
        /// </remarks>
        public uint Hash(ReadOnlySpan<byte> data)
        {
            return Hash(data, FnvOffsetBasis);
        }

        /// <summary>
        /// Calculates a 32-bit hash value over a provided read-only byte sequence, substituting or extending execution 
        /// boundaries via an explicit initialization seed.
        /// </summary>
        /// <param name="data">The read-only byte slice holding the key or payload sequence to hash.</param>
        /// <param name="seed">A custom initial hash state multiplier value; if configured to $0$, defaults internally to the standard <see cref="FnvOffsetBasis"/> vector.</param>
        /// <returns>A 32-bit unsigned integer representing the computed seed-diversified hash coordinate footprint.</returns>
        /// <remarks>
        /// <para>
        /// <b>Advanced Storage Application:</b> Passing a custom seed enables cascading hash calculations across separate 
        /// components. This property is essential for constructing multi-probe <b>Bloom Filters</b> or coordinating partitioned 
        /// data distribution tracks across clustered machine ring coordinates.
        /// </para>
        /// <para>
        /// This routine safely tracks runtime telemetry by incrementing atomic diagnostic counters inside 
        /// the global <see cref="PrimitiveMetrics.HashComputed"/> collection block.
        /// </para>
        /// </remarks>
        public uint Hash(ReadOnlySpan<byte> data, uint seed)
        {
            PrimitiveMetrics.HashComputed.Add(1);

            // If the user specifies 0 as the seed, we fall back to standard FnvOffsetBasis.
            // Otherwise, we seed with their value.
            uint hash = seed == 0 ? FnvOffsetBasis : seed;

            for (int i = 0; i < data.Length; i++)
            {
                hash ^= data[i];
                hash *= FnvPrime;
            }

            return hash;
        }
    }
}