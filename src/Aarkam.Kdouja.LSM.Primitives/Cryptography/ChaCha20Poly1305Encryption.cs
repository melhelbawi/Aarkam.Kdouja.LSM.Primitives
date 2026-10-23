using System.Security.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// A stateless, high-performance concrete implementation of the AEAD encryption contract utilizing 
    /// the native ChaCha20-Poly1305 authenticated encryption algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Provides native <i>Encryption-at-Rest (Ear)</i> capabilities directly within the storage pipeline. 
    /// ChaCha20-Poly1305 guarantees both data confidentiality and cryptographic data integrity. By enforcing structural tag verification, 
    /// it prevents malicious bit-flipping, physical storage block manipulation, or unauthorized background tampering of cold database assets.
    /// </para>
    /// <para>
    /// <b>Performance Profile:</b> ChaCha20-Poly1305 is particularly well-suited for systems without dedicated hardware AES acceleration 
    /// (such as many mobile/IoT ARM chips or virtualized server instances), running faster in software than non-accelerated AES.
    /// </para>
    /// <para>
    /// This implementation relies entirely on stack-allocated or pooled memory spans, ensuring completely allocation-free, in-place 
    /// cryptographic conversions directly within unmanaged page caches.
    /// </para>
    /// </remarks>
    public sealed class ChaCha20Poly1305Encryption : IAeadEncryption
    {
        /// <summary>
        /// Encrypts an uncompressed byte stream directly into a destination memory segment and generates a companion authentication integrity tag.
        /// </summary>
        /// <param name="plaintext">The read-only byte slice containing the raw, sensitive unencrypted data block payload.</param>
        /// <param name="key">The secret cryptographic key vector used to initialize the ChaCha20-Poly1305 encryption execution space.</param>
        /// <param name="nonce">The unique initialization vector slice. This MUST be absolutely unique per invocation under the specified key to preserve security bounds.</param>
        /// <param name="ciphertext">The destination buffer memory span where the resulting encrypted binary payload will be written.</param>
        /// <param name="tag">The destination buffer memory span where the computed 16-byte authentication check tag will be written.</param>
        /// <param name="associatedData">Optional unencrypted metadata bytes (such as a block index or file offset marker) bound cryptographically to this payload to block block-replay attacks.</param>
        /// <exception cref="ArgumentException">Thrown if input key size, nonce length, or tag size bounds break cryptographic specifications.</exception>
        /// <exception cref="PlatformNotSupportedException">Thrown if the underlying operating system does not support ChaCha20-Poly1305.</exception>
        /// <inheritdoc />
        public void Encrypt(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            ValidateParameters(key, nonce, tag);

            using var chacha = new ChaCha20Poly1305(key);
            chacha.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        }

        /// <summary>
        /// Decrypts an encrypted ciphertext payload and performs an atomic tag integrity check to validate data authenticity.
        /// </summary>
        /// <param name="ciphertext">The read-only byte slice holding the encrypted database block payload retrieved from storage layers.</param>
        /// <param name="key">The secret cryptographic key vector associated with the storage block partition configuration.</param>
        /// <param name="nonce">The initialization vector slice applied during the original generation of this specific ciphertext block.</param>
        /// <param name="tag">The expected validation tag payload stored alongside the target ciphertext asset.</param>
        /// <param name="plaintext">The destination buffer memory span where the safely decrypted raw byte stream will be reconstituted.</param>
        /// <param name="associatedData">The unencrypted metadata sequence historically bound to this block; must match the original encryption context exactly to succeed.</param>
        /// <exception cref="ArgumentException">Thrown if key, nonce, or tag dimensions are malformed.</exception>
        /// <exception cref="CryptographicException">Thrown if the authentication tag check fails, signaling malicious modification or physical disk sector corruption.</exception>
        /// <exception cref="PlatformNotSupportedException">Thrown if the underlying operating system does not support ChaCha20-Poly1305.</exception>
        /// <inheritdoc />
        public void Decrypt(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default)
        {
            ValidateParameters(key, nonce, tag);

            using var chacha = new ChaCha20Poly1305(key);
            chacha.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        }

        /// <summary>
        /// Performs low-level length verification checks against the provided initialization vector (nonce) and validation tag buffer footprints.
        /// </summary>
        /// <param name="nonceLength">The physical byte length of the incoming nonce sequence.</param>
        /// <param name="tagLength">The physical byte length of the target validation tag allocation space.</param>
        /// <exception cref="ArgumentException">Thrown if parameters deviate from standardized ChaCha20-Poly1305 sizes.</exception>
        internal static void ValidateBufferSizes(int nonceLength, int tagLength)
        {
            if (nonceLength != CryptographicConstants.ChaCha20Poly1305NonceSize)
            {
                throw new ArgumentException($"ChaCha20-Poly1305 nonce must be {CryptographicConstants.ChaCha20Poly1305NonceSize} bytes.", nameof(nonceLength));
            }

            if (tagLength != CryptographicConstants.ChaCha20Poly1305TagSize)
            {
                throw new ArgumentException($"ChaCha20-Poly1305 tag must be {CryptographicConstants.ChaCha20Poly1305TagSize} bytes.", nameof(tagLength));
            }
        }

        /// <summary>
        /// Consolidates parameter constraints checks, evaluating secret key alignment along with buffer size tolerances.
        /// </summary>
        /// <param name="key">The key span to evaluate.</param>
        /// <param name="nonce">The nonce span to evaluate.</param>
        /// <param name="tag">The verification tag span to evaluate.</param>
        private static void ValidateParameters(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> tag)
        {
            if (key.Length != CryptographicConstants.ChaCha20KeySize)
            {
                throw new ArgumentException($"ChaCha20-Poly1305 key must be {CryptographicConstants.ChaCha20KeySize} bytes.", nameof(key));
            }

            ValidateBufferSizes(nonce.Length, tag.Length);
        }
    }
}
