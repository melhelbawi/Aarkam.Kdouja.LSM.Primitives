using System.Security.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// A high-performance, stateless concrete implementation of <see cref="IKeyDerivation"/> that leverages 
    /// the industry-standard PBKDF2 (Password-Based Key Derivation Function 2) algorithm mapped to an HMAC-SHA256 core.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> This utility acts as the primary boundary gate for database instance initialization, 
    /// cluster node peering, and administrative credential parsing. It processes sensitive key-stretching workloads completely 
    /// via low-overhead stack-allocated spans, preventing sensitive key debris from floating onto the managed .NET Garbage Collector heap.
    /// </para>
    /// <para>
    /// It implements the cryptographic rules outlined in the RFC 2898 specification, hardening low-entropy user or configuration 
    /// passwords into structurally robust 256-bit symmetric key blocks perfectly tailored for downstream consumption by 
    /// <see cref="IAeadEncryptor"/> instances.
    /// </para>
    /// </remarks>
    public sealed class KeyDerivation : IKeyDerivation
    {
        /// <summary>
        /// Derives a high-entropy cryptographic key block into the provided destination buffer using PBKDF2 with HMAC-SHA256.
        /// </summary>
        /// <param name="password">The read-only byte slice holding the source passphrase, user token, or raw seeding input data.</param>
        /// <param name="salt">The random diversification byte slice applied to alter hash results and defeat offline dictionary or rainbow table tracking attacks.</param>
        /// <param name="iterations">The total count of hashing loop passes to execute; must evaluate strictly greater than zero. Higher loop thresholds increase the computation work barrier to thwart automated brute-force scripts.</param>
        /// <param name="destination">The pre-allocated target memory span where the resulting derived cryptographic key block will be written. The size of this span dictates the requested output key dimension.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided <paramref name="iterations"/> value evaluates to zero or less.</exception>
        /// <exception cref="ArgumentException">Thrown if input span parameters break underlying native provider constraints.</exception>
        /// <remarks>
        /// <para>
        /// <b>Under-the-Hood Optimization:</b> This method bypasses managed instantiation completely by routing parameters directly into 
        /// the static, span-aware <see cref="Rfc2898DeriveBytes.Pbkdf2(ReadOnlySpan{byte}, ReadOnlySpan{byte}, Span{byte}, int, HashAlgorithmName)"/> platform API. 
        /// This ensures the intensive underlying hashing loops are offloaded directly to native OS security libraries (OpenSSL or Windows BCrypt) 
        /// utilizing native CPU instructions.
        /// </para>
        /// <para>
        /// <b>Security Warning:</b> The target <paramref name="destination"/> buffer span is completely overwritten during execution. 
        /// It is highly recommended that callers explicitly clear, scrub, or zero out both the <paramref name="password"/> memory block and the 
        /// output <paramref name="destination"/> block once they have fulfilled their lifecycle roles to maximize process-level security isolation.
        /// </para>
        /// </remarks>
        /// <inheritdoc />
        public void DeriveKey(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            Span<byte> destination)
        {
            Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                destination,
                iterations,
                HashAlgorithmName.SHA256);
        }
    }
}