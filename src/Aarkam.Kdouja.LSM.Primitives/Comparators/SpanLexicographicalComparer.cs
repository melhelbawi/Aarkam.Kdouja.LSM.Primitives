namespace Aarkam.Kdouja.LSM.Primitives.Comparators
{
    /// <summary>
    /// A high-performance, stateless, singleton implementation of the standard .NET <see cref="IComparer{T}"/> interface 
    /// that enforces bytewise lexicographical sorting across heap-allocated memory blocks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Fast-path execution layers pass short-lived memory slices using stack-bound structures. 
    /// However, when tracking transaction snapshot tables, index page manifests, or cached metadata within standard .NET sorted structures 
    /// (such as <see cref="SortedDictionary{TKey, TValue}"/> or <see cref="SortedSet{T}"/>), stack-bound types cannot be utilized. 
    /// This class bridges that gap by allowing heap-safe memory structures to be evaluated with the exact same sorting semantics.
    /// </para>
    /// <para>
    /// This design provides overloaded comparison channels for both <see cref="ReadOnlyMemory{T}"/> blocks and raw <see cref="byte"/> arrays, 
    /// ensuring uniform sort orders across separate memory lifetime tiers.
    /// </para>
    /// </remarks>
    public sealed class SpanLexicographicalComparer : IComparer<ReadOnlyMemory<byte>>, IComparer<byte[]>
    {
        /// <summary>
        /// Provides a globally accessible, thread-safe, and immutable singleton instance of the <see cref="SpanLexicographicalComparer"/>.
        /// </summary>
        /// <remarks>
        /// Instantiating separate comparers across millions of independent index sub-layers fragments memory and wastes CPU cycles. 
        /// Reusing this static instance guarantees that all high-level collection segments share reference-identical sorting rules.
        /// </remarks>
        public static readonly SpanLexicographicalComparer Instance = new();

        /// <summary>
        /// Prevents external instantiation of the <see cref="SpanLexicographicalComparer"/> class to safeguard the singleton architecture.
        /// </summary>
        private SpanLexicographicalComparer() { }

        /// <summary>
        /// Compares two heap-safe, read-only memory blocks lexicographically by routing their structural spans into SIMD-accelerated execution pipelines.
        /// </summary>
        /// <param name="x">The first <see cref="ReadOnlyMemory{T}"/> block to evaluate.</param>
        /// <param name="y">The second <see cref="ReadOnlyMemory{T}"/> block to evaluate.</param>
        /// <returns>
        /// A signed 32-bit integer indicating the relative sort relationship:
        /// <list type="table">
        ///   <listheader>
        ///     <term>Value</term>
        ///     <description>Condition</description>
        ///   </listheader>
        ///   <item>
        ///     <term>Less than zero ($&lt; 0$)</term>
        ///     <description>The bytes in <paramref name="x"/> sort before the bytes in <paramref name="y"/>.</description>
        ///   </item>
        ///   <item>
        ///     <term>Zero ($0$)</term>
        ///     <description>Both memory slices contain identical byte configurations.</description>
        ///   </item>
        ///   <item>
        ///     <term>Greater than zero ($&gt; 0$)</term>
        ///     <description>The bytes in <paramref name="x"/> sort after the bytes in <paramref name="y"/>.</description>
        ///   </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// This method slices out internal <see cref="ReadOnlySpan{T}"/> windows from the managed memory wrappers in-place. 
        /// It avoids copying any underlying bytes, executing at raw hardware speeds while matching standard collection constraints.
        /// </remarks>
        /// <inheritdoc />
        public int Compare(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            return x.Span.SequenceCompareTo(y.Span);
        }

        /// <summary>
        /// Compares two nullable raw byte arrays lexicographically, providing defensive null-boundary fallback handling.
        /// </summary>
        /// <param name="x">The first byte array to evaluate.</param>
        /// <param name="y">The second byte array to evaluate.</param>
        /// <returns>
        /// A signed 32-bit integer indicating the relative sort relationship:
        /// <list type="table">
        ///   <listheader>
        ///     <term>Value</term>
        ///     <description>Condition</description>
        ///   </listheader>
        ///   <item>
        ///     <term>Less than zero ($&lt; 0$)</term>
        ///     <description><paramref name="x"/> sorts before <paramref name="y"/>, or <paramref name="x"/> is <see langword="null"/> while <paramref name="y"/> is initialized.</description>
        ///   </item>
        ///   <item>
        ///     <term>Zero ($0$)</term>
        ///     <description>Both arrays contain identical byte configurations, or both parameters evaluate to <see langword="null"/>.</description>
        ///   </item>
        ///   <item>
        ///     <term>Greater than zero ($&gt; 0$)</term>
        ///     <description><paramref name="x"/> sorts after <paramref name="y"/>, or <paramref name="y"/> is <see langword="null"/> while <paramref name="x"/> is initialized.</description>
        ///   </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// Treats <see langword="null"/> entities as structural sorting minimum boundaries (placing them before valid datasets). 
        /// When both arrays are initialized, it converts them into temporary spans to trigger optimized, hardware-vectorized byte evaluations.
        /// </remarks>
        /// <inheritdoc />
        public int Compare(byte[]? x, byte[]? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            return x.AsSpan().SequenceCompareTo(y.AsSpan());
        }
    }
}