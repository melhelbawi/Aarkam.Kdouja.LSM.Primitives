using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aarkam.Kdouja.LSM.Primitives.Buffers
{
#if NET8_0_OR_GREATER
    /// <summary>
    /// Represents a stack-allocated, fixed-size 12-byte contiguous memory buffer optimized for high-throughput storage engines.
    /// Primarily utilized for holding AES-GCM initialization vectors (IVs) / nonces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In database and transactional storage file architectures, data pages or blocks 
    /// are encrypted prior to being flushed to disk. AES-GCM requires a unique 12-byte initialization vector per operation. 
    /// Using standard managed arrays (<c>byte[]</c>) for this purpose forces frequent allocations on the managed heap, leading to 
    /// severe performance degradation due to Garbage Collection (GC) pauses during intense disk I/O.
    /// </para>
    /// <para>
    /// This structure leverages the .NET 8 <see cref="InlineArrayAttribute"/> to embed the 12 bytes directly inline within the 
    /// value type container, guaranteeing zero-allocation, cache-localized, and stack-allocatable memory semantics.
    /// </para>
    /// </remarks>
    [InlineArray(12)]
    public struct Buffer12
    {
        /// <summary>
        /// The anchor element of the contiguous inline array. 
        /// Used by runtime mechanisms to map physical offsets inside the value type container.
        /// </summary>
        private byte _element0;

        /// <summary>
        /// Provides a writable, high-performance window over the internal 12-byte inline buffer without allocating heap memory.
        /// </summary>
        /// <returns>
        /// A direct, mutable <see cref="Span{T}"/> mapped to the underlying 12 bytes.
        /// </returns>
        /// <remarks>
        /// Crucial for storage pipelines requiring zero-copy mutations, such as populating cryptographically random nonces directly 
        /// via hardware random number generators or copying disk streams.
        /// </remarks>
        public Span<byte> AsSpan()
            => MemoryMarshal.CreateSpan(ref _element0, 12);

        /// <summary>
        /// Provides a read-only window over the internal 12-byte inline buffer without allocating heap memory.
        /// </summary>
        /// <returns>
        /// A thread-safe, immutable <see cref="ReadOnlySpan{T}"/> mapped to the underlying 12 bytes.
        /// </returns>
        /// <remarks>
        /// Essential for streaming operations, such as passing encryption metadata blocks down into hardware-accelerated 
        /// crypto routines (e.g., <c>System.Security.Cryptography.AesGcm</c>) cleanly and safely.
        /// </remarks>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
            => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.AsRef(in _element0),
                12);

        /// <summary>
        /// Copies raw input bytes into the internal 12-byte block storage.
        /// </summary>
        /// <param name="source">The source payload data span, which must measure exactly 12 bytes in length.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="source"/> length does not exactly match the static 12-byte layout constraint of this container.
        /// </exception>
        /// <remarks>
        /// Designed for processing sector reads where encryption IVs extracted from raw page or block headers need 
        /// to be transferred into the type-safe engine stack.
        /// </remarks>
        public void CopyFrom(ReadOnlySpan<byte> source)
        {
            if (source.Length != 12)
            {
                throw new ArgumentException(
                    "Buffer12 requires exactly 12 bytes.",
                    nameof(source));
            }

            source.CopyTo(AsSpan());
        }

        /// <summary>
        /// Direct factory instantiation path that maps a 12-byte payload memory segment directly into a new <see cref="Buffer12"/> value type.
        /// </summary>
        /// <param name="source">The raw 12-byte contiguous memory segment containing the payload.</param>
        /// <returns>A populated, value-copied <see cref="Buffer12"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="source"/> length does not exactly match the static 12-byte layout constraint.
        /// </exception>
        public static Buffer12 From(ReadOnlySpan<byte> source)
        {
            var buffer = new Buffer12();
            buffer.CopyFrom(source);
            return buffer;
        }

        /// <summary>
        /// Securely zeroes out the 12-byte memory contents using a non-optimizable memory clear operation.
        /// </summary>
        public void Clear()
            => System.Security.Cryptography.CryptographicOperations.ZeroMemory(AsSpan());

        /// <summary>
        /// Compares two 12-byte buffers for equality in constant time to prevent timing side-channel attacks.
        /// </summary>
        /// <param name="other">The target 12-byte buffer container to evaluate against.</param>
        /// <returns><see langword="true"/> if both buffers match byte-for-byte; otherwise, <see langword="false"/>.</returns>
        public readonly bool ConstantTimeEquals(Buffer12 other)
            => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(AsReadOnlySpan(), other.AsReadOnlySpan());

        /// <summary>
        /// Performs a bitwise, sequential equality comparison between the current buffer and a target <see cref="Buffer12"/> instance.
        /// </summary>
        /// <param name="other">The target 12-byte buffer container to evaluate against.</param>
        /// <returns>
        /// <see langword="true"/> if both 12-byte memory spaces contain exact matching byte values; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Optimized for file structures requiring validation checks against specific initialization vectors or transaction state keys.
        /// </remarks>
        public readonly bool Equals(Buffer12 other)
            => AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

        /// <summary>
        /// Formats the raw 12-byte memory payload into an unpunctuated, uppercase hexadecimal string representation.
        /// </summary>
        /// <returns>An uppercase hexadecimal string representing the exact byte contents of the buffer.</returns>
        /// <remarks>
        /// Useful for engine debugging, generating trace telemetry logs, or printing explicit hex strings for sector dump diagnostics.
        /// </remarks>
        public override readonly string ToString()
            => Convert.ToHexString(AsReadOnlySpan());
    }

#else
    /// <summary>
    /// Represents a stack-allocated, fixed-size 12-byte contiguous memory buffer optimized for high-throughput storage engines.
    /// Primarily utilized for holding AES-GCM initialization vectors (IVs) / nonces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In database and transactional storage file architectures, data pages or blocks 
    /// are encrypted prior to being flushed to disk. AES-GCM requires a unique 12-byte initialization vector per operation. 
    /// Using standard managed arrays (<c>byte[]</c>) for this purpose forces frequent allocations on the managed heap, leading to 
    /// severe performance degradation due to Garbage Collection (GC) pauses during intense disk I/O.
    /// </para>
    /// <para>
    /// <b>.NET 6 Fallback Mechanism:</b> Because the native <c>InlineArray</c> feature is unavailable in older runtimes, 
    /// this fallback implementation leverages explicit sequential structure layouts (<see cref="LayoutKind.Sequential"/>) with 
    /// explicit packing and an exact structural size declaration of 12 bytes to guarantee identical zero-allocation, inline storage characteristics.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12)]
    public struct Buffer12
    {
        /// <summary>
        /// The anchor element pinning the start offset of the unmanaged sequential byte block in memory.
        /// </summary>
        private byte _element0;

        /// <summary>
        /// Provides a writable, high-performance window over the internal 12-byte inline buffer without allocating heap memory.
        /// </summary>
        /// <returns>
        /// A direct, mutable <see cref="Span{T}"/> mapped to the underlying 12 bytes.
        /// </returns>
        /// <remarks>
        /// Crucial for storage pipelines requiring zero-copy mutations, such as populating cryptographically random nonces directly 
        /// via hardware random number generators or copying disk streams.
        /// </remarks>
        public Span<byte> AsSpan()
            => MemoryMarshal.CreateSpan(ref _element0, 12);

        /// <summary>
        /// Provides a read-only window over the internal 12-byte inline buffer without allocating heap memory.
        /// </summary>
        /// <returns>
        /// A thread-safe, immutable <see cref="ReadOnlySpan{T}"/> mapped to the underlying 12 bytes.
        /// </returns>
        /// <remarks>
        /// Essential for streaming operations, such as passing encryption metadata blocks down into hardware-accelerated 
        /// crypto routines (e.g., <c>System.Security.Cryptography.AesGcm</c>) cleanly and safely.
        /// </remarks>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
            => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.AsRef(in _element0),
                12);

        /// <summary>
        /// Copies raw input bytes into the internal 12-byte block storage.
        /// </summary>
        /// <param name="source">The source payload data span, which must measure exactly 12 bytes in length.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="source"/> length does not exactly match the static 12-byte layout constraint of this container.
        /// </exception>
        /// <remarks>
        /// Designed for processing sector reads where encryption IVs extracted from raw page or block headers need 
        /// to be transferred into the type-safe engine stack.
        /// </remarks>
        public void CopyFrom(ReadOnlySpan<byte> source)
        {
            if (source.Length != 12)
            {
                throw new ArgumentException(
                    "Buffer12 requires exactly 12 bytes.",
                    nameof(source));
            }

            source.CopyTo(AsSpan());
        }

        /// <summary>
        /// Direct factory instantiation path that maps a 12-byte payload memory segment directly into a new <see cref="Buffer12"/> value type.
        /// </summary>
        /// <param name="source">The raw 12-byte contiguous memory segment containing the payload.</param>
        /// <returns>A populated, value-copied <see cref="Buffer12"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="source"/> length does not exactly match the static 12-byte layout constraint.
        /// </exception>
        public static Buffer12 From(ReadOnlySpan<byte> source)
        {
            var buffer = new Buffer12();
            buffer.CopyFrom(source);
            return buffer;
        }

        /// <summary>
        /// Securely zeroes out the 12-byte memory contents using a non-optimizable memory clear operation.
        /// </summary>
        public void Clear()
            => System.Security.Cryptography.CryptographicOperations.ZeroMemory(AsSpan());

        /// <summary>
        /// Compares two 12-byte buffers for equality in constant time to prevent timing side-channel attacks.
        /// </summary>
        /// <param name="other">The target 12-byte buffer container to evaluate against.</param>
        /// <returns><see langword="true"/> if both buffers match byte-for-byte; otherwise, <see langword="false"/>.</returns>
        public readonly bool ConstantTimeEquals(Buffer12 other)
            => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(AsReadOnlySpan(), other.AsReadOnlySpan());

        /// <summary>
        /// Performs a bitwise, sequential equality comparison between the current buffer and a target <see cref="Buffer12"/> instance.
        /// </summary>
        /// <param name="other">The target 12-byte buffer container to evaluate against.</param>
        /// <returns>
        /// <see langword="true"/> if both 12-byte memory spaces contain exact matching byte values; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Optimized for file structures requiring validation checks against specific initialization vectors or transaction state keys.
        /// </remarks>
        public readonly bool Equals(Buffer12 other)
            => AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

        /// <summary>
        /// Formats the raw 12-byte memory payload into an unpunctuated, uppercase hexadecimal string representation.
        /// </summary>
        /// <returns>An uppercase hexadecimal string representing the exact byte contents of the buffer.</returns>
        /// <remarks>
        /// Useful for engine debugging, generating trace telemetry logs, or printing explicit hex strings for sector dump diagnostics.
        /// </remarks>
        public override readonly string ToString()
            => Convert.ToHexString(AsReadOnlySpan());
    }
#endif
}