namespace Aarkam.Kdouja.LSM.Primitives.Comparators
{
    /// <summary>
    /// Defines a zero-allocation, span-aware contract used to evaluate the relative sort order of an instance 
    /// directly against a contiguous slice of memory elements.
    /// </summary>
    /// <typeparam name="T">The underlying primitive type or unmanaged value type contained within the span layout.</typeparam>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Traditional .NET comparison structures (<see cref="IComparable{T}"/>) operate strictly 
    /// on instantiated object entities. In an active database engine, lookups continuously search across raw page segments, 
    /// index structures, and transaction blocks. Hydrating fully managed object instances from these raw chunks merely to 
    /// perform a comparison creates devastating heap allocation footprints.
    /// </para>
    /// <para>
    /// Implementing <see cref="ISpanComparable{T}"/> allows a lookup key, boundary sentinel, or metadata entity to compare 
    /// its state directly against unmanaged, raw memory segments (via <see cref="ReadOnlySpan{T}"/>) in-place. This enables 
    /// high-performance binary searching and indexing lookups with zero heap overhead.
    /// </para>
    /// </remarks>
    public interface ISpanComparable<T>
    {
        /// <summary>
        /// Evaluates the sort positioning of the current instance directly against a read-only contiguous memory slice of elements.
        /// </summary>
        /// <param name="other">The target memory span slice to evaluate against the current instance state.</param>
        /// <returns>
        /// A signed 32-bit integer reflecting the relative sorting evaluation:
        /// <list type="table">
        ///   <listheader>
        ///     <term>Value</term>
        ///     <description>Condition</description>
        ///   </listheader>
        ///   <item>
        ///     <term>Less than zero ($&lt; 0$)</term>
        ///     <description>The current instance precedes <paramref name="other"/> in the sort sequence.</description>
        ///   </item>
        ///   <item>
        ///     <term>Zero ($0$)</term>
        ///     <description>The current instance is structurally identical to the contents of <paramref name="other"/>.</description>
        ///   </item>
        ///   <item>
        ///     <term>Greater than zero ($&gt; 0$)</term>
        ///     <description>The current instance follows <paramref name="other"/> in the sort sequence.</description>
        ///   </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// This hot-path method must remain entirely deterministic, deterministic, and free of side effects. It is primarily called 
        /// by page layout iterators, binary-search boundary finders, and low-level lookup structures traversing active index caches.
        /// </remarks>
        int CompareTo(ReadOnlySpan<T> other);
    }
}