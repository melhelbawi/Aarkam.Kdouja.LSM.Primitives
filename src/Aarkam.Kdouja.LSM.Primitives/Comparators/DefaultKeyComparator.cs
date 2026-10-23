namespace Aarkam.Kdouja.LSM.Primitives.Comparators
{
    /// <summary>
    /// A high-performance, stateless, singleton implementation of <see cref="IKeyComparator"/> that orders binary keys 
    /// using standard lexicographical (bytewise) comparison rules.
    /// </summary>
    public sealed class DefaultKeyComparator : IKeyComparator
    {
        /// <summary>
        /// Provides a globally accessible, thread-safe, and immutable singleton instance of the <see cref="DefaultKeyComparator"/>.
        /// </summary>
        public static readonly DefaultKeyComparator Instance = new();

        private DefaultKeyComparator() { }

        /// <inheritdoc/>
        public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
            => x.SequenceCompareTo(y);

        /// <inheritdoc/>
        public int FindShortestSeparator(Span<byte> start, ReadOnlySpan<byte> limit)
        {
            int min = Math.Min(start.Length, limit.Length);
            int diff = 0;
            while (diff < min && start[diff] == limit[diff])
            {
                diff++;
            }

            if (diff >= min)
            {
                return start.Length; // One is a prefix of the other; cannot shorten safely
            }

            if (start[diff] < 0xFF && start[diff] + 1 < limit[diff])
            {
                start[diff]++;
                return diff + 1; // Shortened and still strictly less than limit
            }

            return start.Length;
        }

        /// <inheritdoc/>
        public int FindShortSuccessor(Span<byte> key)
        {
            for (int i = 0; i < key.Length; i++)
            {
                if (key[i] != 0xFF)
                {
                    key[i]++;
                    return i + 1;
                }
            }
            return key.Length; // All 0xFF: leave as is
        }

        /// <inheritdoc/>
        public string Name => "kdouja.BytewiseComparator.v1";
    }
}
