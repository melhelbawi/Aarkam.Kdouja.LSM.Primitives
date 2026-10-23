namespace Aarkam.Kdouja.LSM.Primitives.Compression
{
    /// <summary>
    /// A pass-through, no-op implementation of <see cref="ICompressionCodec"/> that transfers raw data blocks 
    /// without executing any underlying compression or alteration algorithms.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> This codec represents the <see cref="CompressionType.None"/> state configuration. 
    /// It provides architectural polymorphism, allowing the storage engine's write and read paths to process uncompressed 
    /// files, metadata snapshots, and block layouts uniformly without requiring complex conditional branching (<c>if/else</c>) 
    /// scattered throughout hot data-shuffling routines.
    /// </para>
    /// <para>
    /// It achieves true hardware line speed by directly leveraging <see cref="ReadOnlySpan{T}.CopyTo(Span{T})"/>, 
    /// ensuring zero-allocation memory routing with minimal CPU cycle intervention.
    /// </para>
    /// </remarks>
    public sealed class IdentityCompressionCodec : ICompressionCodec
    {
        /// <inheritdoc/>
        public int GetMaxCompressedLength(int sourceLength)
        {
            if (sourceLength < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceLength));
            return sourceLength;
        }

        /// <summary>
        /// Acts as a straight-line pass-through, copying uncompressed source bytes directly into the target destination buffer.
        /// </summary>
        /// <param name="source">The read-only byte slice containing the raw input payload data.</param>
        /// <param name="destination">The mutable target memory span where the raw data will be replicated.</param>
        /// <returns>The exact number of bytes written to the <paramref name="destination"/> span, which perfectly matches the source length.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="destination"/> span bounds are smaller than the incoming <paramref name="source"/> payload length.
        /// </exception>
        public int Compress(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < source.Length)
            {
                throw new ArgumentException(
                    "Destination buffer is too small.",
                    nameof(destination));
            }

            source.CopyTo(destination);
            return source.Length;
        }

        /// <summary>
        /// Acts as a straight-line pass-through, copying raw source bytes directly into the target destination buffer without decompression logic.
        /// </summary>
        /// <param name="source">The read-only byte slice containing the uncompressed input block data fetched from storage.</param>
        /// <param name="destination">The mutable target memory span where the uncompressed data will be duplicated.</param>
        /// <returns>The exact number of bytes written to the <paramref name="destination"/> span, which perfectly matches the source length.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="destination"/> span bounds are smaller than the incoming <paramref name="source"/> payload length.
        /// </exception>
        public int Decompress(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < source.Length)
            {
                throw new ArgumentException(
                    "Destination buffer is too small.",
                    nameof(destination));
            }

            source.CopyTo(destination);
            return source.Length;
        }
    }
}
