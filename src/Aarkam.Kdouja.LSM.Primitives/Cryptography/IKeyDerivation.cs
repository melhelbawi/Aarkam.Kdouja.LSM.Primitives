namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// Defines a zero-allocation contract for cryptographic Key Derivation Functions (KDF) 
    /// applied across the storage engine protection layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> Transactional storage engines require uniform, high-entropy binary blocks 
    /// to initialize symmetric Encryption-at-Rest (Ear) algorithms (such as AES-256-GCM). Variable-length user passphrases 
    /// or cluster node credentials cannot be applied directly to symmetric ciphers. This interface acts as the security 
    /// transformer that safely stretches raw passphrase material into predictable, mathematically hardened cryptographic key rings.
    /// </para>
    /// <para>
    /// By building the abstraction strictly around <see cref="ReadOnlySpan{T}"/> and <see cref="Span{T}"/> slices, the storage 
    /// engine can derive keys without leaking temporary sensitive byte configurations onto the managed heap, enabling developers 
    /// to wipe secret data vectors out of memory immediately following execution.
    /// </para>
    /// </remarks>
    public interface IKeyDerivation
    {
        /// <summary>
        /// Derives a mathematically secure, uniform cryptographic key vector from a source password or seed using the PBKDF2 algorithm layout.
        /// </summary>
        /// <param name="password">The read-only byte slice holding the source passphrase, seed, or input key material.</param>
        /// <param name="salt">The cryptographic salt byte slice. This random vector must be unique per partition or user scope to defeat precomputed dictionary and rainbow table attacks.</param>
        /// <param name="iterations">The physical loop iteration count. Increasing this value scales the computational work barrier required to verify the key, effectively blocking brute-force attacks.</param>
        /// <param name="destination">The pre-allocated target memory span where the resulting derived cryptographic key block will be securely deposited.</param>
        /// <exception cref="ArgumentException">Thrown if provided parameter bounds, salt profiles, or targeted destination buffer lengths fail algorithm layout checks.</exception>
        /// <remarks>
        /// <b>Lifecycle Integration:</b> This method is typically executed during database bootstrap phases, multi-tenant partition mounts, 
        /// administrative credential checks, or master key rotation procedures. It establishes the foundational binary keys passed 
        /// onward to downstream stateful components like <see cref="IAeadEncryptionFactory"/>.
        /// </remarks>
        void DeriveKey(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            Span<byte> destination);
    }
}