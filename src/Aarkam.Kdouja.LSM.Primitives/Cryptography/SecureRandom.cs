using System.Security.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// Provides stateless, zero-allocation, cryptographically secure random number and byte generation utilities 
    /// optimized for hot-path database metadata operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Key-Value architectures depend heavily on high-entropy randomness to handle lock-free slot selection, 
    /// generate unique cluster transaction tokens, and calculate randomized backoff jitter times for thread synchronization loops. 
    /// Traditional pseudo-random generators (like <see cref="Random"/>) are structurally insecure and suffer from predictable value grouping.
    /// </para>
    /// <para>
    /// This utility maps directly to OS-level kernel entropy streams through the platform-optimized <see cref="RandomNumberGenerator"/> framework. 
    /// By utilizing modern C# stack allocations (<c>stackalloc</c>) alongside <see cref="Span{T}"/> slices, it processes random generations 
    /// completely allocation-free, protecting hot read/write code execution paths from Garbage Collection (GC) pauses.
    /// </para>
    /// </remarks>
    public static class SecureRandom
    {
        /// <summary>
        /// Populates a target destination memory span with cryptographically secure, unguessable random bytes.
        /// </summary>
        /// <param name="destination">The mutable target memory span receiving the generated cryptographic byte sequence.</param>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Continuously used across the storage layer to seed custom block cryptographic initializations, 
        /// populate file format identifier arrays, or generate unique partition salt sequences.
        /// </remarks>
        public static void Fill(Span<byte> destination)
        {
            RandomNumberGenerator.Fill(destination);
        }

        /// <summary>
        /// Generates a cryptographically secure random 32-bit signed integer (<see cref="int"/>) bounded between $0$ (inclusive) and <see cref="int.MaxValue"/> (exclusive).
        /// </summary>
        /// <returns>A cryptographically secure, uniformly distributed 32-bit signed integer.</returns>
        /// <remarks>
        /// <b>Storage Optimization Profile:</b> Heavily called by network transport layers and thread orchestrators to dynamically compute 
        /// randomized slot offsets, index partition keys, or load-balancing shards across available execution rings.
        /// </remarks>
        public static int NextInt32()
        {
            return RandomNumberGenerator.GetInt32(int.MaxValue);
        }

        /// <summary>
        /// Generates a cryptographically secure random 64-bit signed integer (<see cref="long"/>) spanning across the full spectrum of possible 64-bit values.
        /// </summary>
        /// <returns>A cryptographically secure, unguessable 64-bit signed integer value.</returns>
        /// <remarks>
        /// <para>
        /// <b>Under-the-Hood Optimization:</b> To completely circumvent managed heap allocations, this method claims an 8-byte buffer directly 
        /// out of the active thread's stack frame using <c>stackalloc</c>. The stack-allocated window is filled via native kernel calls and transformed 
        /// into an intrinsic integer value instantaneously.
        /// </para>
        /// <para>
        /// <b>Distributed Engine Relevance:</b> Ideal for generating highly secure global transaction identifiers (TxID), session tracker coordinates, 
        /// or tracing correlation tokens across separate nodes in a clustered database deployment.
        /// </para>
        /// </remarks>
        public static long NextInt64()
        {
            Span<byte> buffer = stackalloc byte[8];

            RandomNumberGenerator.Fill(buffer);

            return BitConverter.ToInt64(buffer);
        }
    }
}