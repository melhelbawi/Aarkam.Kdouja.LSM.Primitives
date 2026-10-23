using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aarkam.Kdouja.LSM.Primitives.Buffers
{
#if NET8_0_OR_GREATER
    /// <summary>
    /// Represents a stack-allocated, fixed-size 16-byte contiguous memory buffer optimized for high-throughput storage engines.
    /// Primarily utilized for storing globally unique identifiers (GUIDs), transaction IDs, MD5 integrity hashes, or standard AES block data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> High-performance database engines split files into structured pages. Every page, slots metadata, 
    /// or individual data record requires identification via unique IDs (like 16-byte <see cref="Guid"/> structures) or quick verification via 
    /// 16-byte MD5 checksums. Using traditional managed byte arrays (<c>byte[]</c>) causes massive heap churn and triggers frequent Garbage 
    /// Collection (GC) pauses during aggressive storage I/O cycles.
    /// </para>
    /// <para>
    /// This structure uses the .NET 8 <see cref="InlineArrayAttribute"/> to map exactly 16 bytes sequentially inside the value type itself, 
    /// ensuring zero heap allocations, strict memory alignment, and minimal CPU cache-line overhead.
    /// </para>
    /// </remarks>
    [InlineArray(16)]
    public struct Buffer16
    {
        /// <summary>
        /// The anchor element of the contiguous inline array. 
        /// Used by the runtime to map physical offsets inside the value type container.
        /// </summary>
        private byte _element0;

        /// <summary>
        /// Provides a writable, zero-allocation window over the internal 16-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A direct, mutable <see cref="Span{T}"/> mapped to the underlying 16 bytes.
        /// </returns>
        /// <remarks>
        /// Crucial for zero-copy binary mutations, such as copying data directly out of an I/O subsystem buffer, 
        /// unpacking file-system headers, or translating a <see cref="Guid"/> into raw record metadata slots.
        /// </remarks>
        public Span<byte> AsSpan()
            => MemoryMarshal.CreateSpan(ref _element0, 16);

        /// <summary>
        /// Provides a read-only, zero-allocation window over the internal 16-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A thread-safe, immutable <see cref="ReadOnlySpan{T}"/> mapped to the underlying 16 bytes.
        /// </returns>
        /// <remarks>
        /// Essential for critical read paths, such as passing unique block identifiers to indexing algorithms, 
        /// writing headers to disk streams, or passing 16-byte cipher segments into cryptographic verification routines.
        /// </remarks>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
            => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.AsRef(in _element0),
                16);

        /// <summary>
        /// Copies raw input bytes into the internal 16-byte block storage.
        /// </summary>
        /// <param name="source">The source payload data span, which must measure exactly 16 bytes in length.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not exactly match the static 16-byte layout constraint.
        /// </exception>
        /// <remarks>
        /// Designed for parsing file headers where the system needs to safely load transactional boundaries, tracking IDs, or hashes 
        /// into the engine execution stack.
        /// </remarks>
        public void CopyFrom(ReadOnlySpan<byte> source)
        {
            if (source.Length != 16)
            {
                throw new ArgumentException(
                    "Buffer16 requires exactly 16 bytes.",
                    nameof(source));
            }

            source.CopyTo(AsSpan());
        }

        /// <summary>
        /// Direct factory instantiation path that copies a 16-byte memory payload directly into a new stack-bound <see cref="Buffer16"/> value type.
        /// </summary>
        /// <param name="source">The raw 16-byte contiguous memory segment containing the source data.</param>
        /// <returns>A populated, value-copied <see cref="Buffer16"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not match the strict 16-byte size layout requirement.
        /// </exception>
        public static Buffer16 From(ReadOnlySpan<byte> source)
        {
            var buffer = new Buffer16();
            buffer.CopyFrom(source);
            return buffer;
        }

        /// <summary>
        /// Securely zeroes out the 16-byte memory contents using a non-optimizable memory clear operation.
        /// </summary>
        public void Clear()
            => System.Security.Cryptography.CryptographicOperations.ZeroMemory(AsSpan());

        /// <summary>
        /// Compares two 16-byte buffers for equality in constant time to prevent timing side-channel attacks.
        /// </summary>
        /// <param name="other">The target 16-byte buffer container to evaluate against.</param>
        /// <returns><see langword="true"/> if both buffers match byte-for-byte; otherwise, <see langword="false"/>.</returns>
        public readonly bool ConstantTimeEquals(Buffer16 other)
            => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(AsReadOnlySpan(), other.AsReadOnlySpan());

        /// <summary>
        /// Performs an optimized, sequential bitwise equality check between the current buffer and a target <see cref="Buffer16"/> instance.
        /// </summary>
        /// <param name="other">The target 16-byte buffer container to evaluate against.</param>
        /// <returns>
        /// <see langword="true"/> if both 16-byte memory spaces contain identical values; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Extensively used in fast-path lookups to match transaction states, validate checksum signatures, or check index record key IDs.
        /// </remarks>
        public readonly bool Equals(Buffer16 other)
            => AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

        /// <summary>
        /// Formats the raw 16-byte memory payload into an unpunctuated, uppercase hexadecimal string representation.
        /// </summary>
        /// <returns>An uppercase hexadecimal string representing the exact byte contents of the buffer.</returns>
        /// <remarks>
        /// Useful for high-level application tracing, debugging storage engine corruptions, and generating database page diagnostics.
        /// </remarks>
        public override readonly string ToString()
            => Convert.ToHexString(AsReadOnlySpan());
    }

#else
    /// <summary>
    /// Represents a stack-allocated, fixed-size 16-byte contiguous memory buffer optimized for high-throughput storage engines.
    /// Primarily utilized for storing globally unique identifiers (GUIDs), transaction IDs, MD5 integrity hashes, or standard AES block data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> High-performance database engines split files into structured pages. Every page, slots metadata, 
    /// or individual data record requires identification via unique IDs (like 16-byte <see cref="Guid"/> structures) or quick verification via 
    /// 16-byte MD5 checksums. Using traditional managed byte arrays (<c>byte[]</c>) causes massive heap churn and triggers frequent Garbage 
    /// Collection (GC) pauses during aggressive storage I/O cycles.
    /// </para>
    /// <para>
    /// <b>.NET 6 Fallback Mechanism:</b> In environments where native inline array capabilities are unavailable, this fallback implementation 
    /// enforces an explicit sequential structure layout (<see cref="LayoutKind.Sequential"/>) packed tightly to 16 bytes. This enforces 
    /// identical, stack-localized, zero-allocation memory alignment characteristics on legacy runtimes.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public struct Buffer16
    {
        /// <summary>
        /// The anchor element pinning the start offset of the unmanaged sequential byte block in memory.
        /// </summary>
        private byte _element0;

        /// <summary>
        /// Provides a writable, zero-allocation window over the internal 16-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A direct, mutable <see cref="Span{T}"/> mapped to the underlying 16 bytes.
        /// </returns>
        /// <remarks>
        /// Crucial for zero-copy binary mutations, such as copying data directly out of an I/O subsystem buffer, 
        /// unpacking file-system headers, or translating a <see cref="Guid"/> into raw record metadata slots.
        /// </remarks>
        public Span<byte> AsSpan()
            => MemoryMarshal.CreateSpan(ref _element0, 16);

        /// <summary>
        /// Provides a read-only, zero-allocation window over the internal 16-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A thread-safe, immutable <see cref="ReadOnlySpan{T}"/> mapped to the underlying 16 bytes.
        /// </returns>
        /// <remarks>
        /// Essential for critical read paths, such as passing unique block identifiers to indexing algorithms, 
        /// writing headers to disk streams, or passing 16-byte cipher segments into cryptographic verification routines.
        /// </remarks>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
            => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.AsRef(in _element0),
                16);

        /// <summary>
        /// Copies raw input bytes into the internal 16-byte block storage.
        /// </summary>
        /// <param name="source">The source payload data span, which must measure exactly 16 bytes in length.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not exactly match the static 16-byte layout constraint.
        /// </exception>
        /// <remarks>
        /// Designed for parsing file headers where the system needs to safely load transactional boundaries, tracking IDs, or hashes 
        /// into the engine execution stack.
        /// </remarks>
        public void CopyFrom(ReadOnlySpan<byte> source)
        {
            if (source.Length != 16)
            {
                throw new ArgumentException(
                    "Buffer16 requires exactly 16 bytes.",
                    nameof(source));
            }

            source.CopyTo(AsSpan());
        }

        /// <summary>
        /// Direct factory instantiation path that copies a 16-byte memory payload directly into a new stack-bound <see cref="Buffer16"/> value type.
        /// </summary>
        /// <param name="source">The raw 16-byte contiguous memory segment containing the source data.</param>
        /// <returns>A populated, value-copied <see cref="Buffer16"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not match the strict 16-byte size layout requirement.
        /// </exception>
        public static Buffer16 From(ReadOnlySpan<byte> source)
        {
            var buffer = new Buffer16();
            buffer.CopyFrom(source);
            return buffer;
        }

        /// <summary>
        /// Securely zeroes out the 16-byte memory contents using a non-optimizable memory clear operation.
        /// </summary>
        public void Clear()
            => System.Security.Cryptography.CryptographicOperations.ZeroMemory(AsSpan());

        /// <summary>
        /// Compares two 16-byte buffers for equality in constant time to prevent timing side-channel attacks.
        /// </summary>
        /// <param name="other">The target 16-byte buffer container to evaluate against.</param>
        /// <returns><see langword="true"/> if both buffers match byte-for-byte; otherwise, <see langword="false"/>.</returns>
        public readonly bool ConstantTimeEquals(Buffer16 other)
            => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(AsReadOnlySpan(), other.AsReadOnlySpan());

        /// <summary>
        /// Performs an optimized, sequential bitwise equality check between the current buffer and a target <see cref="Buffer16"/> instance.
        /// </summary>
        /// <param name="other">The target 16-byte buffer container to evaluate against.</param>
        /// <returns>
        /// <see langword="true"/> if both 16-byte memory spaces contain identical values; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Extensively used in fast-path lookups to match transaction states, validate checksum signatures, or check index record key IDs.
        /// </remarks>
        public readonly bool Equals(Buffer16 other)
            => AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

        /// <summary>
        /// Formats the raw 16-byte memory payload into an unpunctuated, uppercase hexadecimal string representation.
        /// </summary>
        /// <returns>An uppercase hexadecimal string representing the exact byte contents of the buffer.</returns>
        /// <remarks>
        /// Useful for high-level application tracing, debugging storage engine corruptions, and generating database page diagnostics.
        /// </remarks>
        public override readonly string ToString()
            => Convert.ToHexString(AsReadOnlySpan());
    }
#endif
}