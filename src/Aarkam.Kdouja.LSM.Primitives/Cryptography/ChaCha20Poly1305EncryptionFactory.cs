
namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// A high-performance, stateless factory responsible for generating stateful, pre-keyed ChaCha20-Poly1305 AEAD cryptographic engines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In a secure, multi-tenant Key-Value or database engine, data tracks are partitioned 
    /// using separate cryptographic boundaries or distinct tenant keys. This factory serves as the entry coordinator that translates 
    /// raw data keys into long-lived, highly optimized, stateful <see cref="IAeadEncryptor"/> worker contexts.
    /// </para>
    /// </remarks>
    public sealed class ChaCha20Poly1305EncryptionFactory : IAeadEncryptionFactory
    {
        /// <summary>
        /// Instantiates a stateful, pre-keyed <see cref="IAeadEncryptor"/> instance utilizing the specified raw cryptographic key vector.
        /// </summary>
        /// <param name="key">The read-only byte span representing the secret encryption key vector (typically $32$ bytes for ChaCha20-Poly1305 configurations).</param>
        /// <returns>A stateful, execution-ready <see cref="IAeadEncryptor"/> instance optimized for repeated low-latency block encryption pipelines.</returns>
        /// <remarks>
        /// <b>Performance Integration:</b> This method is executed on database partition mounts, transaction channel initializations, 
        /// or when rotating internal master keys. It builds the cryptographic worker component once, computing and caching the underlying 
        /// native key scheduling states to eliminate CPU initialization spikes on subsequent hot-path database page read/write operations.
        /// </remarks>
        /// <inheritdoc />
        public IAeadEncryptor CreateEncryptor(ReadOnlySpan<byte> key)
        {
            return new ChaCha20Poly1305Encryptor(key);
        }
    }
}
