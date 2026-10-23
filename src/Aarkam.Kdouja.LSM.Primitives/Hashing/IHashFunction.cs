namespace Aarkam.Kdouja.LSM.Primitives.Hashing
{
    /// <summary>
    /// Defines a zero-allocation contract for high-performance, non-cryptographic hash functions 
    /// applied across structural storage and indexing layers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Key-Value architectures depend heavily on low-latency hashing loops to orchestrate 
    /// in-memory index layouts, map database rows to cache buckets, route table shards, and populate probabilistic structures 
    /// like Bloom Filters. Cryptographic hashes introduce heavy CPU overhead; this contract focuses strictly on maximum execution velocity.
    /// </para>
    /// <para>
    /// The design relies entirely on <see cref="ReadOnlySpan{T}"/> inputs, enabling the engine to compute hash offsets directly 
    /// over raw, unmanaged memory blocks or pinned database buffers without triggering managed heap garbage collection churn.
    /// </para>
    /// </remarks>
    public interface IHashFunction
    {
        /// <summary>
        /// Gets the unique structural identifier name tracking this specific hashing algorithm variant.
        /// </summary>
        /// <value>A string literal matching the algorithm identity (e.g., <c>"FNV-1a-32"</c> or <c>"MurmurHash3-32"</c>).</value>
        string Name { get; }

        /// <summary>
        /// Computes a 32-bit unsigned integer hash value over a provided read-only byte sequence utilizing default initial conditions.
        /// </summary>
        /// <param name="data">The read-only byte slice holding the key or payload sequence to hash.</param>
        /// <returns>A 32-bit unsigned integer representing the computed hash coordinate footprint.</returns>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Continuously used to determine entry positioning offsets when mapping 
        /// database keys directly into localized internal memory lookups or page cache routing tables.
        /// </remarks>
        uint Hash(ReadOnlySpan<byte> data);

        /// <summary>
        /// Computes a 32-bit unsigned integer hash value over a provided read-only byte sequence, using a custom initialization seed 
        /// to diversify or cascade the calculation results.
        /// </summary>
        /// <param name="data">The read-only byte slice holding the key or payload sequence to hash.</param>
        /// <param name="seed">A custom initial hash state value used to diversify the mathematical distribution path of the loop.</param>
        /// <returns>A 32-bit unsigned integer representing the computed seed-diversified hash coordinate footprint.</returns>
        /// <remarks>
        /// <b>Advanced Storage Application:</b> Providing an explicit seed parameter is vital for building multi-probe 
        /// <b>Bloom Filters</b>. It enables the filter component to loop a single algorithm instance across $k$ separate iterations, 
        /// varying the seed value each time to compute independent, collision-resistant bit-index allocations for an incoming key.
        /// </remarks>
        uint Hash(ReadOnlySpan<byte> data, uint seed);
    }
}