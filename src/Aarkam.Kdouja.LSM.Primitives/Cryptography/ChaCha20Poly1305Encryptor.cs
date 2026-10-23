using System.Security.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// A stateful, reusable, and disposable implementation of <see cref="IAeadEncryptor"/> that caches and preserves 
    /// the native ChaCha20-Poly1305 key schedule across repetitive execution boundaries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> This class serves as a high-frequency database execution worker for operations 
    /// such as active Write-Ahead Log (WAL) record serialization and background page block compactions. 
    /// By instantiating this class once per active file writer or partition session, the database engine avoids re-executing the 
    /// native CPU key expansion schedule on every block write.
    /// </para>
    /// <para>
    /// Enforces the strict lifetime management contract of <see cref="IDisposable"/> to ensure that unmanaged cryptographic handles 
    /// and secure keys are aggressively wiped and returned to the OS upon pipeline shutdown.
    /// </para>
    /// </remarks>
    public sealed class ChaCha20Poly1305Encryptor : IAeadEncryptor
    {
        /// <summary>
        /// The underlying native cryptographic execution context caching the expanded key table.
        /// </summary>
        private readonly ChaCha20Poly1305 _chacha;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChaCha20Poly1305Encryptor"/> class, validating parameters and performing 
        /// the native key schedule setup.
        /// </summary>
        /// <param name="key">The secret cryptographic master key slice (must measure exactly $32$ bytes for validation bounds).</param>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="key"/> span length deviates from the required size boundary.</exception>
        /// <remarks>
        /// This constructor invokes the underlying provider to bake the master key into an internal expanded block structure, 
        /// preparing it for execution sweeps on incoming data streams.
        /// </remarks>
        public ChaCha20Poly1305Encryptor(ReadOnlySpan<byte> key)
        {
            if (key.Length != CryptographicConstants.ChaCha20KeySize)
            {
                throw new ArgumentException($"ChaCha20 key must be {CryptographicConstants.ChaCha20KeySize} bytes.", nameof(key));
            }

            _chacha = new ChaCha20Poly1305(key);
        }

        /// <summary>
        /// Encrypts an uncompressed plaintext data slice using the cached key schedule, writing the outputs directly into pre-allocated target spans.
        /// </summary>
        /// <param name="plaintext">The read-only byte slice holding the raw input block data; length must perfectly match the <paramref name="ciphertext"/> span alignment.</param>
        /// <param name="nonce">The unique initialization vector slice. Under ChaCha20-Poly1305 guidelines, this vector MUST be strictly unique per call to block cryptographic breakages.</param>
        /// <param name="ciphertext">The pre-allocated target destination span where the encrypted binary payload will be deposited in-place.</param>
        /// <param name="tag">The pre-allocated target destination span where the computed 16-byte integrity verification tag will be written.</param>
        /// <param name="associatedData">Optional unencrypted metadata bytes (e.g., block offsets, file IDs) cryptographically tied to the payload context to prevent page-swap or replay attacks.</param>
        /// <exception cref="ArgumentException">Thrown if nonce or tag allocations violate standardized ChaCha20-Poly1305 sizing parameters, or if plaintext and ciphertext boundaries do not align.</exception>
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
            ChaCha20Poly1305Encryption.ValidateBufferSizes(nonce.Length, tag.Length);
            _chacha.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
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
            ChaCha20Poly1305Encryption.ValidateBufferSizes(nonce.Length, tag.Length);
            _chacha.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        }

        /// <summary>
        /// Deterministically releases and purges all unmanaged resources and cached cryptographic schedules held by the underlying provider.
        /// </summary>
        public void Dispose()
        {
            _chacha.Dispose();
        }
    }
}
