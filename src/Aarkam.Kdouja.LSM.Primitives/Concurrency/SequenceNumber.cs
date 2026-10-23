namespace Aarkam.Kdouja.LSM.Primitives.Concurrency
{
    /// <summary>
    /// Encapsulates a thread-safe, monotonically increasing 64-bit sequence number utilizing CPU hardware atomic operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Storage Engine Relevance:</b> This structure acts as the global chronological clock for the entire storage pipeline. 
    /// Within Multi-Version Concurrency Control (MVCC) layers, every write, update, and deletion is stamped with an atomically incremented 
    /// sequence number. This stamp marks the exact transaction timeline boundary used to evaluate snapshot read visibility and to linearize 
    /// transactional entry sorting inside MemTables and Write-Ahead Logs (WAL).
    /// </para>
    /// <para>
    /// By utilizing <see cref="Interlocked"/> hardware instructions, this class achieves non-blocking synchronization, allowing 
    /// thousands of parallel application execution pathways to claim sequential order tokens without incurring OS thread-scheduling stalls.
    /// </para>
    /// </remarks>
    public sealed class SequenceNumber
    {
        /// <summary>
        /// The underlying 64-bit memory address value holding the active sequence identifier state.
        /// </summary>
        private long _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceNumber"/> class with a specified initial baseline coordinate.
        /// </summary>
        /// <param name="initialValue">The starting sequence integer boundary line, defaulting to <c>0</c>.</param>
        /// <remarks>
        /// Storage subsystems use non-zero initial values during system recovery phases to reconstitute the last active sequence state 
        /// discovered while parsing historical manifest records or transaction log footers during startup.
        /// </remarks>
        public SequenceNumber(long initialValue = 0)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Gets or sets the current sequence number coordinate using atomic memory barriers.
        /// </summary>
        /// <value>The current 64-bit signed sequence tracking integer.</value>
        /// <remarks>
        /// <para>
        /// <b>Read Path Integration:</b> The <see langword="get"/> accessor utilizes <c>Interlocked.Read</c> to guarantee a fully 
        /// synchronized, atomic snapshot look over 64-bit boundaries across all CPU architectures, which prevents dirty or torn reads. This is heavily 
        /// called when a client establishes a point-in-time transaction read visibility snapshot.
        /// </para>
        /// <para>
        /// <b>Write Path Integration:</b> The <see langword="set"/> accessor applies <c>Interlocked.Exchange</c> to force a hard 
        /// full-memory cache barrier, safely overwriting the track sequence when resetting bounds during administrative compaction adjustments or system restore actions.
        /// </para>
        /// </remarks>
        public long Value
        {
            get => Interlocked.Read(ref _value);
            set => Interlocked.Exchange(ref _value, value);
        }

        /// <summary>
        /// Atomically increments the internal sequence counter by exactly 1 and returns the resulting value.
        /// </summary>
        /// <returns>The newly updated, incremented 64-bit sequence marker.</returns>
        /// <remarks>
        /// <b>Hot-Path Integration:</b> This is the primary pipeline driver on the database write track. Every transactional commit or modifications batch 
        /// invokes this method to instantly capture a unique, sequentially guaranteed identity token that establishes its strict global ordering position.
        /// </remarks>
        public long Increment()
        {
            return Interlocked.Increment(ref _value);
        }

        /// <summary>
        /// Executes an atomic, lock-free Compare-And-Swap (CAS) evaluation loop on the internal sequence tracker.
        /// </summary>
        /// <param name="expected">The structural sequence state value the caller assumes the tracker currently holds.</param>
        /// <param name="update">The target updated value to write into memory if the current value matches the <paramref name="expected"/> state.</param>
        /// <returns><see langword="true"/> if the memory location matched the expected baseline and was successfully updated; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Used by optimistic concurrency controls and lock-free coordination blocks inside the engine to validate that no other parallel background 
        /// thread altered sequence states or storage ranges during intermediate evaluation intervals.
        /// </remarks>
        public bool CompareAndSwap(long expected, long update)
        {
            return Interlocked.CompareExchange(ref _value, update, expected) == expected;
        }

        /// <summary>
        /// Converts the current synchronized sequence value into its localized string representation format.
        /// </summary>
        /// <returns>A string representation of the current active numerical sequence value.</returns>
        public override string ToString() => Value.ToString();
    }
}