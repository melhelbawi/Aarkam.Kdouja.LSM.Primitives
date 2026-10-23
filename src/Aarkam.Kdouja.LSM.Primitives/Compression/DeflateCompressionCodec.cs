using System.IO.Compression;

namespace Aarkam.Kdouja.LSM.Primitives.Compression
{
    /// <summary>
    /// A built-in, concrete implementation of <see cref="ICompressionCodec"/> using the standard DEFLATE compression algorithm.
    /// Provides cross-platform, zero-dependency compression matching the DEFLATE industry specification (RFC 1951).
    /// </summary>
    public sealed class DeflateCompressionCodec : ICompressionCodec
    {
        /// <inheritdoc/>
        public int GetMaxCompressedLength(int sourceLength)
        {
            if (sourceLength < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceLength));
            // Conservative upper bound for DEFLATE: source + 12.5% + 64 bytes header/footer overhead
            return sourceLength + (sourceLength >> 3) + 64;
        }

        /// <summary>
        /// Compresses the raw uncompressed source buffer payload into the destination buffer using DEFLATE block formatting rules.
        /// </summary>
        /// <param name="source">The read-only byte slice containing the uncompressed data payload.</param>
        /// <param name="destination">The mutable target memory span where the resulting compressed binary stream will be written.</param>
        /// <returns>The exact number of bytes safely written to the <paramref name="destination"/> buffer span.</returns>
        public int Compress(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            using var outputStream = new MemoryStream();
            using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Fastest, leaveOpen: true))
            {
                deflateStream.Write(source);
            }

            byte[] compressed = outputStream.ToArray();
            if (destination.Length < compressed.Length)
            {
                throw new ArgumentException("Destination buffer is too small.", nameof(destination));
            }

            compressed.CopyTo(destination);
            return compressed.Length;
        }

        /// <summary>
        /// Decompresses a raw DEFLATE-compressed data payload from the source buffer and hydrates the uncompressed bytes into the destination buffer.
        /// </summary>
        /// <param name="source">The read-only byte slice containing the previously compressed data stream.</param>
        /// <param name="destination">The mutable target memory span where the restored uncompressed data blocks will be written.</param>
        /// <returns>The exact number of bytes safely written to the <paramref name="destination"/> buffer span.</returns>
        public int Decompress(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            using var inputStream = new MemoryStream(source.ToArray());
            using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();

            deflateStream.CopyTo(outputStream);
            byte[] decompressed = outputStream.ToArray();

            if (destination.Length < decompressed.Length)
            {
                throw new ArgumentException("Destination buffer is too small.", nameof(destination));
            }

            decompressed.CopyTo(destination);
            return decompressed.Length;
        }
    }
}
