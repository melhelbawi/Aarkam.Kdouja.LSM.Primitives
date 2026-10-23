using System;
using System.Runtime.CompilerServices;

namespace Aarkam.Kdouja.LSM.Primitives.Memory
{
    /// <summary>
    /// Provides low-level, SIMD hardware-accelerated vector mathematics and alignment scanner utilities 
    /// optimized for high-velocity memory block comparison and record matching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> When resolving complex memory indexes, executing binary sorting passes across a MemTable, 
    /// or validating block structures, sequential byte-by-byte comparisons underutilize modern execution pipelines. 
    /// This utility forwards operations directly to BCL hardware-vectorized intrinsics (AVX2, AVX-512, AdvSIMD).
    /// </para>
    /// </remarks>
    public static class VectorMath
    {
        /// <summary>
        /// Compares two read-only byte memory spans for exact structural identity using parallel SIMD vector pipelines.
        /// </summary>
        /// <param name="x">The primary read-only byte sequence block (e.g., a candidate lookup key or target record pointer window).</param>
        /// <param name="y">The secondary read-only byte sequence block to evaluate against the primary source sequence.</param>
        /// <returns><see langword="true"/> if both byte memory spans share identical lengths and match exactly across all byte coordinates; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CompareBlocks(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
            => x.SequenceEqual(y);

        /// <summary>
        /// Scans a read-only memory byte sequence block to locate the absolute index offset of the first occurrence of a specific target byte.
        /// </summary>
        /// <param name="data">The read-only target byte memory span sequence to scan.</param>
        /// <param name="value">The explicit search token value byte target to isolate within the sequence bounds.</param>
        /// <returns>The zero-based numerical offset index location tracking the isolated element match; if no match is discovered, returns <c>-1</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(ReadOnlySpan<byte> data, byte value)
            => data.IndexOf(value);
    }
}
