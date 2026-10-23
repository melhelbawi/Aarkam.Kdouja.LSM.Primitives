namespace Aarkam.Kdouja.LSM.Primitives.Comparators
{
    /// <summary>
    /// Defines a high-performance contract for establishing the deterministic sorting order of raw binary keys 
    /// within the KdoujaKV storage engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Key-Value stores (such as LSM-Tree or B+ Tree engines) maintain data in a 
    /// strictly sorted layout to enable high-speed binary searches, range queries, and streamlined merge-sorting during 
    /// background log compactions. The system relies entirely on this component to determine data placement in 
    /// MemTables and immutable disk segments (SSTables).
    /// </para>
    /// <para>
    /// All evaluations use <see cref="ReadOnlySpan{T}"/> slices to perform ultra-low-latency byte-level comparisons directly on 
    /// pinned memory pages, entirely bypassing heap objects or allocation costs on critical read/write hot paths.
    /// </para>
    /// </remarks>
    public interface IKeyComparator
    {
        /// <summary>
        /// Compares two read-only binary key spans and evaluates their relative sort order.
        /// </summary>
        /// <param name="x">The first binary key span to evaluate.</param>
        /// <param name="y">The second binary key span to evaluate.</param>
        /// <returns>
        /// A signed 32-bit integer indicating the relative evaluation of the keys:
        /// <list type="table">
        ///   <listheader>
        ///     <term>Value</term>
        ///     <description>Condition</description>
        ///   </listheader>
        ///   <item>
        ///     <term>Less than zero ($&lt; 0$)</term>
        ///     <description><paramref name="x"/> sorts before <paramref name="y"/>.</description>
        ///   </item>
        ///   <item>
        ///     <term>Zero ($0$)</term>
        ///     <description><paramref name="x"/> is structurally identical to <paramref name="y"/>.</description>
        ///   </item>
        ///   <item>
        ///     <term>Greater than zero ($&gt; 0$)</term>
        ///     <description><paramref name="x"/> sorts after <paramref name="y"/>.</description>
        ///   </item>
        /// </list>
        /// </returns>
        int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y);

        /// <summary>
        /// Shortens <paramref name="start"/> in-place to any key in [start, limit).
        /// </summary>
        /// <param name="start">The candidate separator key buffer to shorten.</param>
        /// <param name="limit">The lower boundary of the next block.</param>
        /// <returns>The new shortened length (less than or equal to start.Length).</returns>
        int FindShortestSeparator(Span<byte> start, ReadOnlySpan<byte> limit);

        /// <summary>
        /// Shortens <paramref name="key"/> in-place to any key greater than or equal to <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key buffer to shorten.</param>
        /// <returns>The new shortened length.</returns>
        int FindShortSuccessor(Span<byte> key);

        /// <summary>
        /// Gets the human-readable, unique alphanumeric identifier of this specific key sorting implementation.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> representing the comparator signature (e.g., <c>"leveldb.BytewiseComparator"</c>, <c>"kdouja.BytewiseComparator.v1"</c>).
        /// </value>
        string Name { get; }
    }
}
