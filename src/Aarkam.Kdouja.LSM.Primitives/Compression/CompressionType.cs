namespace Aarkam.Kdouja.LSM.Primitives.Compression
{
    /// <summary>
    /// Supported compression algorithms for SSTable blocks.
    /// </summary>
    public enum CompressionType : byte
    {
        /// <summary>
        /// Pass-through uncompressed identity format.
        /// </summary>
        None = 0,

        /// <summary>
        /// Reserved: LZ4 real codec (register via CompressionCodecFactory).
        /// </summary>
        Lz4 = 1,

        /// <summary>
        /// Reserved: Zstandard real codec (register via CompressionCodecFactory).
        /// </summary>
        Zstd = 2,

        /// <summary>
        /// Reserved: Snappy real codec (register via CompressionCodecFactory).
        /// </summary>
        Snappy = 3,

        /// <summary>
        /// Built-in portable DEFLATE compression algorithm fallback.
        /// </summary>
        Deflate = 4
    }
}
