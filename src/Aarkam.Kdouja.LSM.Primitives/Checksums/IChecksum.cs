namespace Aarkam.Kdouja.LSM.Primitives.Checksums
{
    /// <summary>
    /// Defines a standardized, high-performance contract for computing cyclic redundancy checks (CRC) 
    /// or error-detecting verification codes over contiguous binary memory spans.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are expected to be thread-safe and optimized for zero-allocation 
    /// overhead by utilizing <see cref="ReadOnlySpan{T}"/>. This contract is ideal for data integrity 
    /// validation in networking protocols, file serialization, and caching layers.
    /// </remarks>
    public interface IChecksum
    {
        /// <summary>
        /// Computes the 32-bit unsigned checksum value for the specified contiguous span of binary data.
        /// </summary>
        /// <param name="data">The read-only byte span representing the memory payload to be evaluated.</param>
        /// <returns>A <see cref="uint"/> containing the calculated checksum or CRC verification code.</returns>
        /// <remarks>
        /// Consumer implementations should optimize this method for low-latency processing, leveraging 
        /// hardware acceleration (SIMD/Intrinsics) or precomputed lookup tables where applicable.
        /// </remarks>
        uint Compute(ReadOnlySpan<byte> data);

        /// <summary>
        /// Continues computing a checksum over subsequent data: Extend(Compute(a), b) == Compute(a ++ b).
        /// </summary>
        /// <param name="crc">The checksum computed from previous data segments.</param>
        /// <param name="data">The next data slice to append to the calculation.</param>
        /// <returns>The combined 32-bit unsigned checksum value.</returns>
        uint Extend(uint crc, ReadOnlySpan<byte> data);

        /// <summary>
        /// Gets the human-readable, unique alphanumeric name of the specific checksum algorithm implementation.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> representing the algorithm identifier (e.g., <c>"CRC32-C"</c>, <c>"Adler32"</c>).
        /// </value>
        /// <remarks>
        /// This property is typically used for telemetry, cryptographic logging, dependency injection keys, 
        /// or conditional factory routing.
        /// </remarks>
        string Name { get; }
    }
}