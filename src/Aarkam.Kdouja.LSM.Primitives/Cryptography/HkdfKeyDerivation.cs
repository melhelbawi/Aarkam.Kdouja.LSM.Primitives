using System.Security.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// Derives independent cryptographic subkeys (per file, segment, or purpose) from a high-entropy master key
    /// using HMAC-based Extract-and-Expand Key Derivation Function (HKDF, RFC 5869).
    /// </summary>
    public static class HkdfKeyDerivation
    {
        /// <summary>
        /// Derives a subkey into <paramref name="destination"/> using HKDF-SHA256.
        /// </summary>
        /// <param name="masterKey">The high-entropy master key (minimum 32 bytes recommended).</param>
        /// <param name="salt">Optional cryptographic salt bytes.</param>
        /// <param name="info">Context/application specific info (e.g., fileId or chunkIndex).</param>
        /// <param name="destination">The destination span for the derived subkey bytes.</param>
        public static void DeriveKey(
            ReadOnlySpan<byte> masterKey,
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> info,
            Span<byte> destination)
        {
            if (masterKey.Length < 32)
                throw new ArgumentException("Master key must be at least 32 bytes.", nameof(masterKey));

            HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, destination, salt, info);
        }
    }
}
