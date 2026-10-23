using System.Security.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// Factory for creating platform-appropriate <see cref="IAeadEncryptor"/> instances based on runtime hardware acceleration.
    /// </summary>
    public static class AeadEncryptorFactory
    {
        /// <summary>
        /// Creates an <see cref="IAeadEncryptor"/> targeting hardware-accelerated AES-GCM or ChaCha20-Poly1305.
        /// </summary>
        /// <param name="key">The secret encryption key span (typically 32 bytes for AES-256 or ChaCha20).</param>
        /// <param name="preferChaCha"><see langword="true"/> to prefer ChaCha20-Poly1305 over AES-GCM when both are supported.</param>
        /// <returns>A new pre-keyed <see cref="IAeadEncryptor"/> instance.</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown when neither AES-GCM nor ChaCha20-Poly1305 is supported on the current platform.</exception>
        public static IAeadEncryptor Create(ReadOnlySpan<byte> key, bool preferChaCha = false)
        {
            bool aes = AesGcm.IsSupported;
            bool chacha = ChaCha20Poly1305.IsSupported;

            if (preferChaCha && chacha)
            {
                return new ChaCha20Poly1305Encryptor(key);
            }

            if (aes)
            {
                return new AesGcmEncryptor(key);
            }

            if (chacha)
            {
                return new ChaCha20Poly1305Encryptor(key);
            }

            throw new PlatformNotSupportedException("Neither AES-GCM nor ChaCha20-Poly1305 is available on this platform.");
        }
    }
}
