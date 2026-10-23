namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// Defines a zero-allocation contract for Authenticated Encryption with Associated Data (AEAD) operations 
    /// within the storage engine security pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Enforces native <i>Encryption-at-Rest (Ear)</i> security policies. 
    /// Unlike standard encryption algorithms that only hide data data, AEAD engines mathematically guarantee both data confidentiality 
    /// and cryptographic data integrity. It produces a trailing verification tag that flags storage bit-rot, physical sector tampering, 
    /// or unauthorized block manipulation before data maps back into the engine's active memory spaces.
    /// </para>
    /// <para>
    /// This contract relies entirely on stack-allocated or pooled <see cref="ReadOnlySpan{T}"/> and <see cref="Span{T}"/> slices, 
    /// empowering the engine to execute highly intensive cryptographic operations directly on raw page buffers with zero managed heap overhead.
    /// </para>
    /// </remarks>
    public interface IAeadEncryption
    {
        /// <summary>
        /// Encrypts raw plaintext data and computes an atomic authentication integrity tag using an AEAD cryptographic cipher.
        /// </summary>
        /// <param name="plaintext">The read-only byte slice holding the sensitive, unencrypted database block payload.</param>
        /// <param name="key">The secret cryptographic master key slice associated with the target storage partition context.</param>
        /// <param name="nonce">A strictly unique initialization vector slice. Under AEAD standards, this vector MUST never be reused with the same key to prevent cipher breakdown.</param>
        /// <param name="ciphertext">The pre-allocated destination buffer span where the resulting encrypted binary stream will be written.</param>
        /// <param name="tag">The pre-allocated destination buffer span where the calculated Galois/Counter authentication integrity token will be deposited.</param>
        /// <param name="associatedData">Optional unencrypted contextual metadata (e.g., file index, block offset coordinates) bound cryptographically to this stream to prevent sector replay attacks.</param>
        /// <exception cref="ArgumentException">Thrown if provided key sizes, nonce boundaries, or pre-allocated target buffer lengths violate cryptographic constraints.</exception>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Invoked on the storage write pipeline just before an uncompressed data page, 
        /// Write-Ahead Log (WAL) segment, or global manifest snapshot frame is flushed to the physical disk system handle.
        /// </remarks>
        void Encrypt(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default);

        /// <summary>
        /// Decrypts an encrypted ciphertext block and runs an atomic authentication pass against the companion verification tag.
        /// </summary>
        /// <param name="ciphertext">The read-only byte slice holding the encrypted database block payload retrieved from storage.</param>
        /// <param name="key">The secret cryptographic master key slice linked to the source storage partition context.</param>
        /// <param name="nonce">The unique initialization vector slice historically applied during the generation of this specific ciphertext block.</param>
        /// <param name="tag">The expected validation tag payload stored alongside the target ciphertext asset.</param>
        /// <param name="plaintext">The pre-allocated destination buffer span where the safely decrypted, raw byte stream will be reconstituted.</param>
        /// <param name="associatedData">The unencrypted metadata sequence historically bound to this block; must match the original encryption context exactly to validate authenticity.</param>
        /// <exception cref="ArgumentException">Thrown if any parameter bounds or target buffer layouts fail validation rules.</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// Thrown if the validation tag verification fails, signaling malicious modification, data truncation, or physical hardware sector corruption.
        /// </exception>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> Executed on the storage read pipeline whenever a cache miss forces the engine to pull an encrypted block 
        /// from disk, validating and decoding its payload in-place before loading records back into working memory index tables.
        /// </remarks>
        void Decrypt(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default);
    }
}