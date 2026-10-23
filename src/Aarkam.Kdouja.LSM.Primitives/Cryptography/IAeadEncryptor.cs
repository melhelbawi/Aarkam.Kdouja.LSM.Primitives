using System.Security.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// Defines a stateful, reusable, and disposable contract for executing high-performance AEAD (Authenticated Encryption with Associated Data) 
    /// encryption and decryption tasks over pre-allocated memory slices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> This interface represents an active cryptographic worker instance natively bound to a pre-computed 
    /// secret key schedule. By retaining the internal native round-key states across distinct execution boundaries, it eliminates the expensive 
    /// CPU setup penalty typically incurred during standard symmetric initialization pipelines. It is optimized specifically for hot-path engine tasks, 
    /// such as continuous Write-Ahead Log (WAL) appends and intensive multi-threaded SSTable block compactions.
    /// </para>
    /// <para>
    /// All operations are executed exclusively through <see cref="ReadOnlySpan{T}"/> and <see cref="Span{T}"/> structures, ensuring completely 
    /// allocation-free, in-place cryptographic conversions directly within unmanaged page cache tiers or rented memory arenas.
    /// </para>
    /// <para>
    /// Implementations are explicitly bound to the <see cref="IDisposable"/> interface to mandate the immediate, deterministic scrubbing 
    /// and purging of unmanaged cryptographic context descriptors and secret key material from system memory.
    /// </para>
    /// </remarks>
    public interface IAeadEncryptor : IDisposable
    {
        /// <summary>
        /// Encrypts raw plaintext data utilizing the pre-configured cached key schedule, depositing the resulting ciphertext 
        /// and companion integrity token directly into pre-allocated target memory spans.
        /// </summary>
        /// <param name="plaintext">The read-only byte slice holding the raw unencrypted database payload; length must perfectly match the <paramref name="ciphertext"/> span layout.</param>
        /// <param name="nonce">The unique initialization vector slice required for this encryption sweep. This vector MUST be strictly unique per call under the active key to maintain security invariants.</param>
        /// <param name="ciphertext">The pre-allocated mutable target memory span where the generated encrypted binary payload will be deposited.</param>
        /// <param name="tag">The pre-allocated mutable target memory span where the resulting fixed-length Galois/Counter authentication integrity check token will be written.</param>
        /// <param name="associatedData">Optional unencrypted contextual metadata (such as logical file IDs, block offsets, or sequence markers) bound cryptographically to the payload to block sector-swap or block-replay attacks.</param>
        /// <exception cref="ArgumentException">Thrown if input key configurations, nonce dimensions, or tag buffer sizes violate algorithm rules, or if plaintext and ciphertext boundaries are mismatched.</exception>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Driven continuously by active background block flush queues and transactional log appenders just before scheduling a physical I/O sync to non-volatile disk.
        /// </remarks>
        void Encrypt(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> nonce,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default);

        /// <summary>
        /// Decrypts an encrypted storage block and performs an atomic integrity evaluation against the provided authentication tag and contextual data prior to exposing the plaintext bytes.
        /// </summary>
        /// <param name="ciphertext">The read-only byte slice containing the encrypted block payload retrieved from storage layers.</param>
        /// <param name="nonce">The unique initialization vector slice historically applied during the generation of this specific ciphertext block.</param>
        /// <param name="tag">The expected validation tag token paired alongside the target ciphertext asset on disk.</param>
        /// <param name="plaintext">The pre-allocated mutable target destination span where the safely decrypted raw binary sequence will be reconstituted.</param>
        /// <param name="associatedData">Optional unencrypted contextual metadata sequence historically bound to this block; must match the original encryption context exactly to validate data authenticity.</param>
        /// <exception cref="ArgumentException">Thrown if any operational buffer allocation footprints or parameter lengths fail structural validation sweeps.</exception>
        /// <exception cref="CryptographicException">Thrown if the atomic tag authentication check fails, signaling physical media bit-rot, data truncation, or unauthorized data tampering.</exception>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Triggered on the storage read pipeline whenever a page cache miss forces the engine to read an encrypted 
        /// sector from disk, validating and decoding its payload in-place before mapping the records back into structural index search tables.
        /// </remarks>
        void Decrypt(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default);
    }
}