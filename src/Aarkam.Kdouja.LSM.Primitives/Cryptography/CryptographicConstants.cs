namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// Serves as the centralized registry for immutable, compile-time cryptographic dimensions and parameter constants 
    /// enforced throughout the storage engine architecture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Key-Value engines handle binary payloads that require strict layout precision. 
    /// Hardcoding buffer lengths across separate modules (such as WAL appenders, block encoders, and network transport layers) 
    /// risks structural value drifting, which inevitably leads to corrupted file indexing and read-path failures. 
    /// </para>
    /// <para>
    /// This class centralizes sizing definitions, enabling the engine to execute fast compile-time optimizations and 
    /// clean boundary verification sweeps across unmanaged memory blocks.
    /// </para>
    /// </remarks>
    public static class CryptographicConstants
    {
        /// <summary>
        /// The baseline number of hash iteration rounds enforced during PBKDF2 (Password-Based Key Derivation Function 2) execution.
        /// </summary>
        /// <remarks>
        /// <b>Security Profile:</b> Configured to <c>100,000</c> loops to provide a computationally intensive workload barrier. 
        /// This baseline intentionally slows down brute-force attacks on administrative client login portals or cluster node authentication passes.
        /// </remarks>
        public const int DefaultPbkdf2Iterations = 100_000;

        /// <summary>
        /// The required physical length constraint in bytes for an advanced AES-256 symmetric cipher key ($32$ bytes / $256$ bits).
        /// </summary>
        /// <remarks>
        /// Used continuously by multi-tenant partition routers and encryption factories to validate that incoming security tokens 
        /// match structural enterprise specifications prior to mounting active table namespaces.
        /// </remarks>
        public const int Aes256KeySize = 32;

        /// <summary>
        /// The standardized cryptographic initialization vector (nonce) dimension in bytes required by the AES-GCM encryption pipeline ($12$ bytes / $96$ bits).
        /// </summary>
        /// <remarks>
        /// <b>Layout Relevance:</b> When writing an encrypted data block, the engine appends this 12-byte sequence alongside the ciphertext 
        /// payload so that future file readers can securely reconstitute the initialization state vector.
        /// </remarks>
        public const int AesGcmNonceSize = 12;

        /// <summary>
        /// The absolute length constraint in bytes of the validation tag produced by the Galois/Counter authentication layer ($16$ bytes / $128$ bits).
        /// </summary>
        /// <remarks>
        /// <b>Data Integrity Profile:</b> This constant defines the precise signature block padding size written at the end of encrypted 
        /// SSTable pages or WAL records. The engine extracts this exact 16-byte slice to check for bit-flipping attacks or hardware disk corruption.
        /// </remarks>
        public const int AesGcmTagSize = 16;

        /// <summary>
        /// The required physical length constraint in bytes for a ChaCha20 symmetric cipher key ($32$ bytes / $256$ bits).
        /// </summary>
        /// <remarks>
        /// <b>Sizing Integrity:</b> This constant guarantees that derived or generated keys used for ChaCha20-Poly1305 operations
        /// match standard 256-bit cryptographic strength.
        /// </remarks>
        public const int ChaCha20KeySize = 32;

        /// <summary>
        /// The standardized cryptographic initialization vector (nonce) dimension in bytes required by the ChaCha20-Poly1305 encryption pipeline ($12$ bytes / $96$ bits).
        /// </summary>
        /// <remarks>
        /// <b>Nonce Uniqueness:</b> Nonces used in ChaCha20-Poly1305 must be strictly unique under the same key. A 12-byte nonce
        /// provides sufficient entropy when randomly generated or sequenced.
        /// </remarks>
        public const int ChaCha20Poly1305NonceSize = 12;

        /// <summary>
        /// The absolute length constraint in bytes of the validation tag produced by the ChaCha20-Poly1305 authentication layer ($16$ bytes / $128$ bits).
        /// </summary>
        /// <remarks>
        /// <b>Integrity Check:</b> A 16-byte (128-bit) tag guarantees authenticated encryption with associated data, protecting
        /// uncompressed database records from tampering or offline hardware sector swapping.
        /// </remarks>
        public const int ChaCha20Poly1305TagSize = 16;
    }
}