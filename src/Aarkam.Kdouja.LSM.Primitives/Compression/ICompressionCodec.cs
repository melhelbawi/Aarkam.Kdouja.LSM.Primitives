namespace Aarkam.Kdouja.LSM.Primitives.Compression
{
    /// <summary>
    /// Defines a zero-allocation, high-performance contract for plugging in distinct compression and decompression algorithms (codecs) 
    /// across the storage subsystem.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Transactional storage infrastructures use compression codecs across multiple critical subsystems to reduce 
    /// physical disk footprint and maximize I/O throughput. Typical ingestion pipelines apply fast compression to **SSTable data blocks**, 
    /// **WAL segments**, and **Manifest state snapshots** prior to scheduling a physical storage sync.
    /// </para>
    /// <para>
    /// This contract processes raw data exclusively through <see cref="ReadOnlySpan{T}"/> and <see cref="Span{T}"/> representations. 
    /// This enables the engine to perform block alterations directly inside pre-allocated native or pooled memory spaces, ensuring zero 
    /// managed heap allocations on the hot read/write paths.
    /// </para>
    /// </remarks>
    public interface ICompressionCodec
    {
        /// <summary>
        /// Calculates the maximum possible buffer size required to store the compressed representation of a source buffer.
        /// </summary>
        /// <param name="sourceLength">The length of the uncompressed data in bytes.</param>
        /// <returns>The upper bound buffer size in bytes.</returns>
        int GetMaxCompressedLength(int sourceLength);

        /// <summary>
        /// Compresses the raw uncompressed source buffer payload and writes the compressed stream into the specified destination buffer.
        /// </summary>
        /// <param name="source">The read-only byte slice containing the uncompressed data payload.</param>
        /// <param name="destination">The mutable target memory span where the resulting compressed binary stream will be written.</param>
        /// <returns>The exact number of bytes safely written to the <paramref name="destination"/> buffer span.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="destination"/> span length is insufficient to handle the output bounds of the compression routine.
        /// </exception>
        int Compress(
            ReadOnlySpan<byte> source,
            Span<byte> destination);

        /// <summary>
        /// Decompresses a raw compressed data payload from the source buffer and writes the restored uncompressed binary sequence into the destination buffer.
        /// </summary>
        /// <param name="source">The read-only byte slice containing the previously compressed data stream.</param>
        /// <param name="destination">The mutable target memory span where the restored uncompressed data blocks will be written.</param>
        /// <returns>The exact number of bytes safely written to the <paramref name="destination"/> buffer span.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="destination"/> span bounds cannot accommodate the entire decompressed payload length.
        /// </exception>
        int Decompress(
            ReadOnlySpan<byte> source,
            Span<byte> destination);
    }
}
