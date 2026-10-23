using System.Security.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// A stateful, reusable, and disposable implementation of <see cref="IAeadEncryptor"/> that caches and preserves 
    /// the native AES-GCM key schedule across repetitive execution boundaries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> This class serves as the ultra-fast performance workhorse for high-frequency database operations 
    /// such as active Write-Ahead Log (WAL) record serialization and intensive background SSTable page block compactions. 
    /// By instantiating this class once per active file writer or partition session, the database engine avoids re-executing the 
    /// expensive native CPU key expansion schedule on every block write.
    /// </para>
    /// <para>
    /// Enforces the strict lifetime management contract of <see cref="IDisposable"/> to ensure that unmanaged cryptographic handles, 
    /// native page structures, and secure keys are aggressively wiped and returned to the OS upon pipeline shutdown.
    /// </para>
    /// </remarks>
    public sealed class AesGcmEncryptor : IAeadEncryptor
    {
        /// <summary>
        /// The underlying native cryptographic execution context caching the expanded round-key table.
        /// </summary>
        private readonly AesGcm _aes;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesGcmEncryptor"/> class, validating parameters and performing 
        /// the native key schedule expansion loop.
        /// </summary>
        /// <param name="key">The secret cryptographic master key slice (must measure exactly $32$ bytes for AES-256 validation bounds).</param>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="key"/> span length deviates from the required size boundary.</exception>
        /// <remarks>
        /// This constructor invokes the underlying hardware provider to bake the master key into an internal expanded block structure, 
        /// preparing it for instantaneous execution sweeps on incoming data streams.
        /// </remarks>
        public AesGcmEncryptor(ReadOnlySpan<byte> key)
        {
            if (key.Length != AesGcmEncryption.KeySize)
            {
                throw new ArgumentException($"AES-256-GCM key must be {AesGcmEncryption.KeySize} bytes.", nameof(key));
            }

#if NET8_0_OR_GREATER
            _aes = new AesGcm(key, AesGcmEncryption.TagSize);
#else
            _aes = new AesGcm(key);
#endif
        }

        /// <summary>
        /// Encrypts an uncompressed plaintext data slice using the cached key schedule, writing the outputs directly into pre-allocated target spans.
        /// </summary>
        /// <param name="plaintext">The read-only byte slice holding the raw input block data; length must perfectly match the <paramref name="ciphertext"/> span alignment.</param>
        /// <param name="nonce">The unique initialization vector slice. Under AES-GCM guidelines, this vector MUST be strictly unique per call to block cryptographic breakages.</param>
        /// <param name="ciphertext">The pre-allocated target destination span where the encrypted binary payload will be deposited in-place.</param>
        /// <param name="tag">The pre-allocated target destination span where the computed 16-byte Galois integrity verification tag will be written.</param>
        /// <param name="associatedData">Optional unencrypted metadata bytes (e.g., block offsets, file IDs) cryptographically tied to the payload context to prevent page-swap or replay attacks.</param>
        /// <exception cref="ArgumentException">Thrown if nonce or tag allocations violate standardized AES-GCM sizing parameters, or if plaintext and ciphertext boundaries do not align.</exception>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Continuously triggered by active block writing queues or log appenders. Operates entirely without 
        /// heap allocations, utilizing stack or pooled buffers to guarantee low latency under intense database workloads.
        /// </remarks>
        public void Encrypt(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> nonce,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            AesGcmEncryption.ValidateBufferSizes(nonce.Length, tag.Length);
            _aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        }

        /// <summary>
        /// Decrypts a ciphertext database block using the cached key schedule, executing an atomic integrity check via the provided authentication tag.
        /// </summary>
        /// <param name="ciphertext">The read-only byte slice holding the encrypted database block payload fetched from storage layers.</param>
        /// <param name="nonce">The initialization vector slice applied during the original generation of this specific ciphertext block.</param>
        /// <param name="tag">The expected validation tag payload stored alongside the target ciphertext asset.</param>
        /// <param name="plaintext">The pre-allocated target destination span where the safely decrypted raw byte stream will be reconstituted.</param>
        /// <param name="associatedData">The unencrypted metadata sequence historically bound to this block; must match the original encryption context exactly to succeed.</param>
        /// <exception cref="ArgumentException">Thrown if any structural payload boundaries or lengths are malformed.</exception>
        /// <exception cref="CryptographicException">Thrown if the authentication tag check fails, signaling malicious modification or physical disk sector corruption.</exception>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Executed on the storage read path whenever a cache miss forces the engine to read an encrypted 
        /// data block from disk, decoding it in-place before mapping its structural rows back into working cache pools.
        /// </remarks>
        public void Decrypt(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default)
        {
            AesGcmEncryption.ValidateBufferSizes(nonce.Length, tag.Length);
            _aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        }

        /// <summary>
        /// Deterministically releases and purges all unmanaged resources, handles, and cached cryptographic schedules held by the underlying provider.
        /// </summary>
        /// <remarks>
        /// This instance is completely invalidated and rendered unusable following a call to this method. Explicit disposal prevents reference leakage 
        /// of native cryptographic tokens back into OS process blocks.
        /// </remarks>
        public void Dispose()
        {
            _aes.Dispose();
        }
    }
}