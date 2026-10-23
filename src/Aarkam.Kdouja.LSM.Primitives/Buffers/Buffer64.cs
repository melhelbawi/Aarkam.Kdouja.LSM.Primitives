using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aarkam.Kdouja.LSM.Primitives.Buffers
{
#if NET8_0_OR_GREATER
    /// <summary>
    /// Represents a stack-allocated, fixed-size 64-byte contiguous memory buffer optimized for hardware-aligned storage engines.
    /// Primarily utilized for holding high-strength cryptographic hashes, digital signatures, or CPU cache-aligned metadata headers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In low-latency storage architectures, 64 bytes represents a critical geometric milestone. 
    /// First, it matches the exact size of a standard modern hardware **CPU Cache Line** (x86 and ARM64). Aligning core file structures or indexing 
    /// headers to 64-byte boundaries prevents "false sharing" and allows the CPU to fetch data from RAM in a single cycle. 
    /// Second, it natively holds large validation primitives such as SHA-512 integrity blocks or 64-byte asymmetric signatures (e.g., Ed25519) 
    /// used to sign transactional page headers.
    /// </para>
    /// <para>
    /// This structure uses the .NET 8 <see cref="InlineArrayAttribute"/> to map exactly 64 bytes sequentially inside the value type container, 
    /// completely avoiding heap allocations and preventing Garbage Collection (GC) latency during high-frequency disk I/O routines.
    /// </para>
    /// </remarks>
    [InlineArray(64)]
    public struct Buffer64
    {
        /// <summary>
        /// The anchor element of the contiguous inline array. 
        /// Used by the runtime to map physical offsets inside the value type container.
        /// </summary>
        private byte _element0;

        /// <summary>
        /// Provides a writable, zero-allocation window over the internal 64-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A direct, mutable <see cref="Span{T}"/> mapped to the underlying 64 bytes.
        /// </returns>
        /// <remarks>
        /// Crucial for zero-copy file mutations, such as copying data directly out of OS kernel buffers, 
        /// computing SHA-512 signatures inline, or populating aligned sector metadata.
        /// </remarks>
        public Span<byte> AsSpan()
            => MemoryMarshal.CreateSpan(ref _element0, 64);

        /// <summary>
        /// Provides a read-only, zero-allocation window over the internal 64-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A thread-safe, immutable <see cref="ReadOnlySpan{T}"/> mapped to the underlying 64 bytes.
        /// </returns>
        /// <remarks>
        /// Essential for critical read paths, such as passing static cryptographic keys to unmanaged blocks, 
        /// verifying file-header authenticity signatures, or writing raw layout blocks to storage streams.
        /// </remarks>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
            => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.AsRef(in _element0),
                64);

        /// <summary>
        /// Copies raw input bytes into the internal 64-byte block storage.
        /// </summary>
        /// <param name="source">The source payload data span, which must measure exactly 64 bytes in length.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not exactly match the static 64-byte layout constraint.
        /// </exception>
        /// <remarks>
        /// Designed for parsing block index boundaries where a SHA-512 hash or asymmetric page verification identity 
        /// needs to be pulled safely into the executing thread's stack frame.
        /// </remarks>
        public void CopyFrom(ReadOnlySpan<byte> source)
        {
            if (source.Length != 64)
            {
                throw new ArgumentException(
                    "Buffer64 requires exactly 64 bytes.",
                    nameof(source));
            }

            source.CopyTo(AsSpan());
        }

        /// <summary>
        /// Direct factory instantiation path that copies a 64-byte memory payload directly into a new stack-bound <see cref="Buffer64"/> value type.
        /// </summary>
        /// <param name="source">The raw 64-byte contiguous memory segment containing the source data.</param>
        /// <returns>A populated, value-copied <see cref="Buffer64"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not match the strict 64-byte size layout requirement.
        /// </exception>
        public static Buffer64 From(ReadOnlySpan<byte> source)
        {
            var buffer = new Buffer64();
            buffer.CopyFrom(source);
            return buffer;
        }

        /// <summary>
        /// Securely zeroes out the 64-byte memory contents using a non-optimizable memory clear operation.
        /// </summary>
        public void Clear()
            => System.Security.Cryptography.CryptographicOperations.ZeroMemory(AsSpan());

        /// <summary>
        /// Compares two 64-byte buffers for equality in constant time to prevent timing side-channel attacks.
        /// </summary>
        /// <param name="other">The target 64-byte buffer container to evaluate against.</param>
        /// <returns><see langword="true"/> if both buffers match byte-for-byte; otherwise, <see langword="false"/>.</returns>
        public readonly bool ConstantTimeEquals(Buffer64 other)
            => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(AsReadOnlySpan(), other.AsReadOnlySpan());

        /// <summary>
        /// Performs an optimized, sequential bitwise equality check between the current buffer and a target <see cref="Buffer64"/> instance.
        /// </summary>
        /// <param name="other">The target 64-byte buffer container to evaluate against.</param>
        /// <returns>
        /// <see langword="true"/> if both 64-byte memory spaces contain identical values; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Widely used in storage engines to match transaction block validation structures or verify long-form cryptographical identity tags.
        /// </remarks>
        public readonly bool Equals(Buffer64 other)
            => AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

        /// <summary>
        /// Formats the raw 64-byte memory payload into an unpunctuated, uppercase hexadecimal string representation.
        /// </summary>
        /// <returns>An uppercase hexadecimal string representing the exact byte contents of the buffer.</returns>
        /// <remarks>
        /// Useful for engine debugging, formatting crash logs during sector layout corruptions, and creating database diagnostic dumps.
        /// </remarks>
        public override readonly string ToString()
            => Convert.ToHexString(AsReadOnlySpan());
    }

#else
    /// <summary>
    /// Represents a stack-allocated, fixed-size 64-byte contiguous memory buffer optimized for hardware-aligned storage engines.
    /// Primarily utilized for holding high-strength cryptographic hashes, digital signatures, or CPU cache-aligned metadata headers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In low-latency storage architectures, 64 bytes represents a critical geometric milestone. 
    /// First, it matches the exact size of a standard modern hardware **CPU Cache Line** (x86 and ARM64). Aligning core file structures or indexing 
    /// headers to 64-byte boundaries prevents "false sharing" and allows the CPU to fetch data from RAM in a single cycle. 
    /// Second, it natively holds large validation primitives such as SHA-512 integrity blocks or 64-byte asymmetric signatures (e.g., Ed25519) 
    /// used to sign transactional page headers.
    /// </para>
    /// <para>
    /// <b>.NET 6 Fallback Mechanism:</b> In runtime frameworks missing native inline arrays, this fallback implementation 
    /// enforces an explicit sequential structure layout (<see cref="LayoutKind.Sequential"/>) packed tightly to 64 bytes. This guarantees 
    /// identical stack-localized, zero-allocation memory alignment characteristics on older target platforms.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
    public struct Buffer64
    {
        /// <summary>
        /// The anchor element pinning the start offset of the unmanaged sequential byte block in memory.
        /// </summary>
        private byte _element0;

        /// <summary>
        /// Provides a writable, zero-allocation window over the internal 64-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A direct, mutable <see cref="Span{T}"/> mapped to the underlying 64 bytes.
        /// </returns>
        /// <remarks>
        /// Crucial for zero-copy file mutations, such as copying data directly out of OS kernel buffers, 
        /// computing SHA-512 signatures inline, or populating aligned sector metadata.
        /// </remarks>
        public Span<byte> AsSpan()
            => MemoryMarshal.CreateSpan(ref _element0, 64);

        /// <summary>
        /// Provides a read-only, zero-allocation window over the internal 64-byte inline buffer.
        /// </summary>
        /// <returns>
        /// A thread-safe, immutable <see cref="ReadOnlySpan{T}"/> mapped to the underlying 64 bytes.
        /// </returns>
        /// <remarks>
        /// Essential for critical read paths, such as passing static cryptographic keys to unmanaged blocks, 
        /// verifying file-header authenticity signatures, or writing raw layout blocks to storage streams.
        /// </remarks>
        public readonly ReadOnlySpan<byte> AsReadOnlySpan()
            => MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.AsRef(in _element0),
                64);

        /// <summary>
        /// Copies raw input bytes into the internal 64-byte block storage.
        /// </summary>
        /// <param name="source">The source payload data span, which must measure exactly 64 bytes in length.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not exactly match the static 64-byte layout constraint.
        /// </exception>
        /// <remarks>
        /// Designed for parsing block index boundaries where a SHA-512 hash or asymmetric page verification identity 
        /// needs to be pulled safely into the executing thread's stack frame.
        /// </remarks>
        public void CopyFrom(ReadOnlySpan<byte> source)
        {
            if (source.Length != 64)
            {
                throw new ArgumentException(
                    "Buffer64 requires exactly 64 bytes.",
                    nameof(source));
            }

            source.CopyTo(AsSpan());
        }

        /// <summary>
        /// Direct factory instantiation path that copies a 64-byte memory payload directly into a new stack-bound <see cref="Buffer64"/> value type.
        /// </summary>
        /// <param name="source">The raw 64-byte contiguous memory segment containing the source data.</param>
        /// <returns>A populated, value-copied <see cref="Buffer64"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="source"/> length does not match the strict 64-byte size layout requirement.
        /// </exception>
        public static Buffer64 From(ReadOnlySpan<byte> source)
        {
            var buffer = new Buffer64();
            buffer.CopyFrom(source);
            return buffer;
        }

        /// <summary>
        /// Securely zeroes out the 64-byte memory contents using a non-optimizable memory clear operation.
        /// </summary>
        public void Clear()
            => System.Security.Cryptography.CryptographicOperations.ZeroMemory(AsSpan());

        /// <summary>
        /// Compares two 64-byte buffers for equality in constant time to prevent timing side-channel attacks.
        /// </summary>
        /// <param name="other">The target 64-byte buffer container to evaluate against.</param>
        /// <returns><see langword="true"/> if both buffers match byte-for-byte; otherwise, <see langword="false"/>.</returns>
        public readonly bool ConstantTimeEquals(Buffer64 other)
            => System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(AsReadOnlySpan(), other.AsReadOnlySpan());

        /// <summary>
        /// Performs an optimized, sequential bitwise equality check between the current buffer and a target <see cref="Buffer64"/> instance.
        /// </summary>
        /// <param name="other">The target 64-byte buffer container to evaluate against.</param>
        /// <returns>
        /// <see langword="true"/> if both 64-byte memory spaces contain identical values; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Widely used in storage engines to match transaction block validation structures or verify long-form cryptographical identity tags.
        /// </remarks>
        public readonly bool Equals(Buffer64 other)
            => AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());

        /// <summary>
        /// Formats the raw 64-byte memory payload into an unpunctuated, uppercase hexadecimal string representation.
        /// </summary>
        /// <returns>An uppercase hexadecimal string representing the exact byte contents of the buffer.</returns>
        /// <remarks>
        /// Useful for engine debugging, formatting crash logs during sector layout corruptions, and creating database diagnostic dumps.
        /// </remarks>
        public override readonly string ToString()
            => Convert.ToHexString(AsReadOnlySpan());
    }
#endif
}