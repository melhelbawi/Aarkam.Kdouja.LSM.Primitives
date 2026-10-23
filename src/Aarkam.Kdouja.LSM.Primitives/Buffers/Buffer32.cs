using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aarkam.Kdouja.LSM.Primitives.Buffers
{
#if NET8_0_OR_GREATER
    /// <summary>
    /// Represents a stack-allocated, fixed-size 32-byte contiguous memory buffer optimized for high-throughput storage engines.
    /// Primarily utilized for handling cryptographic security parameters, data integrity validation blocks, or tree structures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In low-latency transaction processing, databases use 32-byte buffers to hold cryptographic hashes 
    /// (such as SHA-256 for page payload validation), secure symmetric keys (AES-256 encryption keys), or index pointers (B+ Tree or LSM-Tree node identifiers). 
    /// Instantiating standard managed heap arrays (<c>byte[]</c>) for millions of records creates massive heap fragmentation and forces intense Garbage Collection (GC) pressure.
    /// </para>
    /// <para>
    /// This structure utilizes the .NET 8 <see cref="InlineArrayAttribute"/> to map exactly 32 bytes sequentially inside the value type itself. 
    /// This guarantees zero heap allocations, fast CPU stack execution, and deterministic, inline physical layout parameters.
    /// </para>
    /// </remarks>
    [InlineArray(32)]
    public struct Buffer32
    {
        /// <summary>
        /// The anchor element of the contiguous inline array. 
        /// Used by the runtime to map physical offsets inside the value type container.
        /// </summary>
        private byte _element0;

        /// <summary>
        /// Provides a writable, zero-allocation window over the internal 32-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A direct, mutable <see cref="Span{T}"/> mapped to the underlying 32 bytes.
        /// </returns>
        /// <remarks>
        /// Crucial for zero-copy mutations, such as reading file system segments from disk, filling an encryption key 
        /// from an internal vault, or computing a SHA-256 block hash inline.
        /// </remarks>
        public Span<byte> AsSpan()
            => MemoryMarshal.CreateSpan(ref _element0, 32);

        /// <summary>
        /// Provides a read-only, zero-allocation window over the internal 32-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A thread-safe, immutable <see cref="ReadOnlySpan{T}"/> mapped to the underlying 32 bytes.
        /// </returns>
        /// <remarks>
        /// Essential for high-performance read loops, such as comparing node lookup IDs, passing static AES keys 
        /// to crypto contexts, or writing integrity block hashes directly out to disk storage buffers.
        /// </remarks>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
            => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.AsRef(in _element0),
                32);

        /// <summary>
        /// Copies raw input bytes into the internal 32-byte block storage.
        /// </summary>
        /// <param name="source">The source payload data span, which must measure exactly 32 bytes in length.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not exactly match the static 32-byte layout constraint.
        /// </exception>
        /// <remarks>
        /// Designed for parsing block metadata tables where a SHA-256 hash or secure sector context signature 
        /// needs to be pulled safely into the current execution thread stack.
        /// </remarks>
        public void CopyFrom(ReadOnlySpan<byte> source)
        {
            if (source.Length != 32)
            {
                throw new ArgumentException(
                    "Buffer32 requires exactly 32 bytes.",
                    nameof(source));
            }

            source.CopyTo(AsSpan());
        }

        /// <summary>
        /// Direct factory instantiation path that copies a 32-byte memory payload directly into a new stack-bound <see cref="Buffer32"/> value type.
        /// </summary>
        /// <param name="source">The raw 32-byte contiguous memory segment containing the source data.</param>
        /// <returns>A populated, value-copied <see cref="Buffer32"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not match the strict 32-byte size layout requirement.
        /// </exception>
        public static Buffer32 From(ReadOnlySpan<byte> source)
        {
            var buffer = new Buffer32();
            buffer.CopyFrom(source);
            return buffer;
        }

        /// <summary>
        /// Securely zeroes out the 32-byte memory contents using a non-optimizable memory clear operation.
        /// </summary>
        public void Clear()
            => System.Security.Cryptography.CryptographicOperations.ZeroMemory(AsSpan());

        /// <summary>
        /// Compares two 32-byte buffers for equality in constant time to prevent timing side-channel attacks.
        /// </summary>
        /// <param name="other">The target 32-byte buffer container to evaluate against.</param>
        /// <returns><see langword="true"/> if both buffers match byte-for-byte; otherwise, <see langword="false"/>.</returns>
        public readonly bool ConstantTimeEquals(Buffer32 other)
            => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(AsReadOnlySpan(), other.AsReadOnlySpan());

        /// <summary>
        /// Performs an optimized, sequential bitwise equality check between the current buffer and a target <see cref="Buffer32"/> instance.
        /// </summary>
        /// <param name="other">The target 32-byte buffer container to evaluate against.</param>
        /// <returns>
        /// <see langword="true"/> if both 32-byte memory spaces contain identical values; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Used continuously in indexing lookups to confirm storage node identity, validate block checksum matches, or evaluate encrypted payload signatures.
        /// </remarks>
        public readonly bool Equals(Buffer32 other)
            => AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

        /// <summary>
        /// Formats the raw 32-byte memory payload into an unpunctuated, uppercase hexadecimal string representation.
        /// </summary>
        /// <returns>An uppercase hexadecimal string representing the exact byte contents of the buffer.</returns>
        /// <remarks>
        /// Useful for high-level storage tracing, writing debug logs for tracking index corrupted hashes, and generating engine state dumps.
        /// </remarks>
        public override readonly string ToString()
            => Convert.ToHexString(AsReadOnlySpan());
    }

#else
    /// <summary>
    /// Represents a stack-allocated, fixed-size 32-byte contiguous memory buffer optimized for high-throughput storage engines.
    /// Primarily utilized for handling cryptographic security parameters, data integrity validation blocks, or tree structures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In low-latency transaction processing, databases use 32-byte buffers to hold cryptographic hashes 
    /// (such as SHA-256 for page payload validation), secure symmetric keys (AES-256 encryption keys), or index pointers (B+ Tree or LSM-Tree node identifiers). 
    /// Instantiating standard managed heap arrays (<c>byte[]</c>) for millions of records creates massive heap fragmentation and forces intense Garbage Collection (GC) pressure.
    /// </para>
    /// <para>
    /// <b>.NET 6 Fallback Mechanism:</b> In legacy runtime environments missing native inline array attributes, this fallback implementation 
    /// enforces an explicit sequential structure layout (<see cref="LayoutKind.Sequential"/>) packed tightly to 32 bytes. This layout guarantees identical, 
    /// stack-localized, zero-allocation behavior and unmanaged structure parsing.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32)]
    public struct Buffer32
    {
        /// <summary>
        /// The anchor element pinning the start offset of the unmanaged sequential byte block in memory.
        /// </summary>
        private byte _element0;

        /// <summary>
        /// Provides a writable, zero-allocation window over the internal 32-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A direct, mutable <see cref="Span{T}"/> mapped to the underlying 32 bytes.
        /// </returns>
        /// <remarks>
        /// Crucial for zero-copy mutations, such as reading file system segments from disk, filling an encryption key 
        /// from an internal vault, or computing a SHA-256 block hash inline.
        /// </remarks>
        public Span<byte> AsSpan()
            => MemoryMarshal.CreateSpan(ref _element0, 32);

        /// <summary>
        /// Provides a read-only, zero-allocation window over the internal 32-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A thread-safe, immutable <see cref="ReadOnlySpan{T}"/> mapped to the underlying 32 bytes.
        /// </returns>
        /// <remarks>
        /// Essential for high-performance read loops, such as comparing node lookup IDs, passing static AES keys 
        /// to crypto contexts, or writing integrity block hashes directly out to disk storage buffers.
        /// </remarks>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
            => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.AsRef(in _element0),
                32);

        /// <summary>
        /// Copies raw input bytes into the internal 32-byte block storage.
        /// </summary>
        /// <param name="source">The source payload data span, which must measure exactly 32 bytes in length.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not exactly match the static 32-byte layout constraint.
        /// </exception>
        /// <remarks>
        /// Designed for parsing block metadata tables where a SHA-256 hash or secure sector context signature 
        /// needs to be pulled safely into the current execution thread stack.
        /// </remarks>
        public void CopyFrom(ReadOnlySpan<byte> source)
        {
            if (source.Length != 32)
            {
                throw new ArgumentException(
                    "Buffer32 requires exactly 32 bytes.",
                    nameof(source));
            }

            source.CopyTo(AsSpan());
        }

        /// <summary>
        /// Direct factory instantiation path that copies a 32-byte memory payload directly into a new stack-bound <see cref="Buffer32"/> value type.
        /// </summary>
        /// <param name="source">The raw 32-byte contiguous memory segment containing the source data.</param>
        /// <returns>A populated, value-copied <see cref="Buffer32"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not match the strict 32-byte size layout requirement.
        /// </exception>
        public static Buffer32 From(ReadOnlySpan<byte> source)
        {
            var buffer = new Buffer32();
            buffer.CopyFrom(source);
            return buffer;
        }

        /// <summary>
        /// Securely zeroes out the 32-byte memory contents using a non-optimizable memory clear operation.
        /// </summary>
        public void Clear()
            => System.Security.Cryptography.CryptographicOperations.ZeroMemory(AsSpan());

        /// <summary>
        /// Compares two 32-byte buffers for equality in constant time to prevent timing side-channel attacks.
        /// </summary>
        /// <param name="other">The target 32-byte buffer container to evaluate against.</param>
        /// <returns><see langword="true"/> if both buffers match byte-for-byte; otherwise, <see langword="false"/>.</returns>
        public readonly bool ConstantTimeEquals(Buffer32 other)
            => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(AsReadOnlySpan(), other.AsReadOnlySpan());

        /// <summary>
        /// Performs an optimized, sequential bitwise equality check between the current buffer and a target <see cref="Buffer32"/> instance.
        /// </summary>
        /// <param name="other">The target 32-byte buffer container to evaluate against.</param>
        /// <returns>
        /// <see langword="true"/> if both 32-byte memory spaces contain identical values; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Used continuously in indexing lookups to confirm storage node identity, validate block checksum matches, or evaluate encrypted payload signatures.
        /// </remarks>
        public readonly bool Equals(Buffer32 other)
            => AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

        /// <summary>
        /// Formats the raw 32-byte memory payload into an unpunctuated, uppercase hexadecimal string representation.
        /// </summary>
        /// <returns>An uppercase hexadecimal string representing the exact byte contents of the buffer.</returns>
        /// <remarks>
        /// Useful for high-level storage tracing, writing debug logs for tracking index corrupted hashes, and generating engine state dumps.
        /// </remarks>
        public override readonly string ToString()
            => Convert.ToHexString(AsReadOnlySpan());
    }
#endif
}