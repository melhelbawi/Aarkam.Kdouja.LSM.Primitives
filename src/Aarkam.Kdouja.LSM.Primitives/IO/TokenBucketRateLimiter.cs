using System.Diagnostics;

namespace Aarkam.Kdouja.LSM.Primitives.IO
{
    /// <summary>
    /// A high-performance, thread-safe rate-limiting utility implementing a virtual-time (GCRA) token bucket
    /// designed to balance and throttle internal I/O bandwidth consumption without concurrency debt erasure.
    /// </summary>
    public sealed class TokenBucketRateLimiter
    {
        private readonly double _ticksPerByte;
        private readonly long _burstTicks;
        private readonly object _lock = new();
        private long _tat; // Theoretical Arrival Time in Stopwatch ticks

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenBucketRateLimiter"/> class.
        /// </summary>
        /// <param name="bytesPerSecond">The total volume of bytes allocated per running second window.</param>
        public TokenBucketRateLimiter(long bytesPerSecond)
        {
            if (bytesPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(bytesPerSecond), "Rate must be greater than zero.");

            _ticksPerByte = (double)Stopwatch.Frequency / bytesPerSecond;
            _burstTicks = Stopwatch.Frequency; // 1 second burst capacity
        }

        /// <summary>
        /// Requests a designated token allocation count asynchronously, evaluating and applying necessary execution delays 
        /// if current resource requests outpace available bandwidth.
        /// </summary>
        public async ValueTask AcquireAsync(long bytes, CancellationToken ct = default)
        {
            if (bytes <= 0)
                return;

            ct.ThrowIfCancellationRequested();

            long cost = (long)Math.Ceiling(bytes * _ticksPerByte);
            long waitTicks;

            lock (_lock)
            {
                long now = Stopwatch.GetTimestamp();
                long newTat = Math.Max(_tat, now) + cost;
                _tat = newTat;
                waitTicks = newTat - now - _burstTicks;
            }

            if (waitTicks <= 0)
                return;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds((double)waitTicks / Stopwatch.Frequency), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                lock (_lock)
                {
                    _tat -= cost; // Refund tokens on cancellation
                }
                throw;
            }
        }
    }
}
