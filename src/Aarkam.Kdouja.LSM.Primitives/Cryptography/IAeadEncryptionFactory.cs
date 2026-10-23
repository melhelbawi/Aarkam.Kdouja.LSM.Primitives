namespace Aarkam.Kdouja.LSM.Primitives.Cryptography
{
    /// <summary>
    /// Defines a factory contract for instantiating stateful, pre-keyed AEAD (Authenticated Encryption with Associated Data) 
    /// cryptographic engines optimized for highly repetitive read and write hot paths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> In a secure database engine utilizing <i>Encryption-at-Rest (Ear)</i>, data payloads 
    /// are partitioned across diverse isolation zones, such as individual table spaces, multi-tenant file shards, or separate transactional logs. 
    /// This factory abstracts the creation of stateful workers, ensuring that the heavy CPU overhead of native key expansion schedules 
    /// is isolated to creation time rather than hot-path execution cycles.
    /// </para>
    /// <para>
    /// <b>Concurrency Warning:</b> The <see cref="IAeadEncryptor"/> instances produced by implementations of this factory 
    /// preserve internal native cryptographic state registers and are typically <b>not thread-safe</b>. Callers must consult the specific 
    /// implementation documentation to verify thread-safety guarantees or coordinate dedicated instances per execution worker thread.
    /// </para>
    /// <para>
    /// <b>Security Material Management:</b> Keys are passed strictly via low-latency <see cref="ReadOnlySpan{T}"/> allocations. 
    /// Callers retain absolute ownership of the underlying memory allocations and are responsible for securely zeroing out or clearing 
    /// sensitive cryptographic material in physical memory as soon as the factory operation completes.
    /// </para>
    /// </remarks>
    public interface IAeadEncryptionFactory
    {
        /// <summary>
        /// Instantiates a new stateful cryptographic encryptor/decryptor instance natively bound to the provided secret master key vector.
        /// </summary>
        /// <param name="key">The read-only byte span representing the secret encryption key material (typically $32$ bytes / $256$ bits for standard enterprise configurations).</param>
        /// <returns>A stateful, execution-ready <see cref="IAeadEncryptor"/> instance pre-configured for low-latency block-level operations.</returns>
        /// <exception cref="ArgumentException">Thrown if the provided <paramref name="key"/> span dimensions fail to align with the underlying cipher algorithm requirements.</exception>
        /// <remarks>
        /// <b>Lifecycle Management:</b> This method is typically invoked during database subsystem startup, multi-tenant partition mounts, 
        /// or transaction channel initializations. The resulting stateful object caches the expanded round-key table internally, eliminating 
        /// CPU performance spikes on subsequent hot-path database page reads, writes, or background file compactions.
        /// </remarks>
        IAeadEncryptor CreateEncryptor(ReadOnlySpan<byte> key);
    }
}