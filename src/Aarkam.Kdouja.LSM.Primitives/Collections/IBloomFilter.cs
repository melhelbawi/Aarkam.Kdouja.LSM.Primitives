namespace Aarkam.Kdouja.LSM.Primitives.Collections
{
    /// <summary>
    /// Defines a standardized contract for a high-performance, probabilistic data structure 
    /// optimized to reduce redundant disk reads within transactional storage engines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In low-latency data layouts (such as LSM-Tree architectures or tiered SSTables), 
    /// performing point-lookups on keys that do not exist can cause crippling disk I/O penalties. A Bloom Filter acts as an 
    /// in-memory gatekeeper. If <see cref="MayContain"/> returns <see langword="false"/>, the storage engine guarantees the 
    /// key is absent and completely skips an expensive disk seek. If it returns <see langword="true"/>, the engine proceeds to search disk storage.
    /// </para>
    /// <para>
    /// This contract leverages <see cref="ReadOnlySpan{T}"/> inputs to process binary keys at hardware speeds without generating 
    /// managed heap allocations or triggering garbage collection during lookups.
    /// </para>
    /// </remarks>
    public interface IBloomFilter
    {
        /// <summary>
        /// Inserts a raw binary key into the probabilistic filter by hashing its bytes and tracking it in the underlying bit vector.
        /// </summary>
        /// <param name="key">The read-only byte span representing the unique identifier or record key being indexed.</param>
        /// <remarks>
        /// This method is heavily utilized during compaction routines or when flushing write-buffers (MemTables) out into 
        /// immutable, finalized storage blocks on disk.
        /// </remarks>
        void Add(ReadOnlySpan<byte> key);

        /// <summary>
        /// Evaluates the filter to determine whether a specific binary key is potentially present within the associated storage block, 
        /// or definitely absent.
        /// </summary>
        /// <param name="key">The read-only byte span representing the lookup key to search for.</param>
        /// <returns>
        /// <see langword="false"/> if the key is **guaranteed** to be completely absent from the dataset (allowing the engine to bypass disk reads); 
        /// <see langword="true"/> if the key **might** be present (indicating the engine must proceed to parse disk pages, subject to false-positive rates).
        /// </returns>
        /// <remarks>
        /// This is the primary fast-path method used during point lookups to shield physical storage layers from unnecessary, expensive disk seeks.
        /// </remarks>
        bool MayContain(ReadOnlySpan<byte> key);

        /// <summary>
        /// Serializes the current state of the Bloom Filter's internal bit array into a compact, contiguous byte sequence.
        /// </summary>
        /// <returns>A flat <see cref="byte"/> array representing the dehydrated state of the filter.</returns>
        /// <remarks>
        /// Used by the storage subsystem to write the filter directly into the trailer or metadata sector of an index block file on disk, 
        /// allowing it to be reconstituted rapidly into memory next time the file is opened.
        /// </remarks>
        byte[] Serialize();
    }
}