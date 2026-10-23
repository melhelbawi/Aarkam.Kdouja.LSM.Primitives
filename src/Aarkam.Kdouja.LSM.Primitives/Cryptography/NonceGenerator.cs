using System.Security.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// Provides stateless, zero-allocation, cryptographically secure random number and nonce generation utilities 
    /// optimized for high-throughput storage pipelines.
    /// </summary>
    public static class NonceGenerator
    {
        /// <summary>
        /// Standard AES-GCM initialization vector footprint in bytes ($12$ bytes / $96$ bits).
        /// </summary>
        public const int AesGcmNonceSize = 12;

        /// <summary>
        /// Populates a target destination buffer span with cryptographically secure, unguessable random bytes without allocating heap memory.
        /// </summary>
        public static void Fill(Span<byte> destination)
        {
            RandomNumberGenerator.Fill(destination);
        }

        /// <summary>
        /// Generates a cryptographically secure uniform random 64-bit signed integer (<see cref="long"/>) within a specified interval boundary line.
        /// </summary>
        public static long GetInt64(long fromInclusive, long toExclusive)
        {
            if (fromInclusive >= toExclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(fromInclusive), "Lower bound must be less than upper bound.");
            }

            // Calculate the range magnitude
            ulong range = (ulong)(toExclusive - fromInclusive);

            // If the range fits inside a 32-bit signed integer, defer to the optimized native .NET API
            if (range <= int.MaxValue)
            {
                return fromInclusive + RandomNumberGenerator.GetInt32(0, (int)range);
            }

            // For full 64-bit ranges, use an allocation-free mask-and-retry algorithm to prevent modulo bias
            ulong mask = range - 1;
            mask |= mask >> 1;
            mask |= mask >> 2;
            mask |= mask >> 4;
            mask |= mask >> 8;
            mask |= mask >> 16;
            mask |= mask >> 32;

            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            ulong result;

            do
            {
                RandomNumberGenerator.Fill(bytes);
                result = BitConverter.ToUInt64(bytes) & mask;
            }
            while (result >= range);

            return fromInclusive + (long)result;
        }

        #region Obsolete Legacy APIs

        /// <summary>
        /// Generates a new AES-GCM nonce into a heap-allocated array.
        /// </summary>
        [Obsolete("Use Fill(Span<byte>) instead to avoid heap allocations. E.g., Span<byte> nonce = stackalloc byte[NonceGenerator.AesGcmNonceSize]; NonceGenerator.Fill(nonce);", false)]
        public static byte[] GenerateAesGcmNonce()
        {
            var nonce = new byte[AesGcmNonceSize];
            RandomNumberGenerator.Fill(nonce);
            return nonce;
        }

        /// <summary>
        /// Legacy 64-bit integer range tracking wrapper mechanism.
        /// </summary>
        [Obsolete("Use NonceGenerator.GetInt64(from, to) instead for a zero-allocation, cryptographically secure alternative.", false)]
        public static long SecureRandomNextInt64(long fromInclusive, long toExclusive)
        {
            return GetInt64(fromInclusive, toExclusive);
        }

        #endregion
    }
}
