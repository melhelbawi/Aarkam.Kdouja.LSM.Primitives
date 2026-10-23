namespace Aarkam.Kdouja.LSM.Primitives.Concurrency
{
    /// <summary>
    /// Coordinates and tracks snapshot visibility lifetimes across concurrent execution contexts within an MVCC architecture.
    /// </summary>
    public sealed class ReadWriteVersionGuard
    {
        private readonly object _gate = new();
        private readonly SortedDictionary<long, int> _pins = new();
        private readonly SequenceNumber _committed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadWriteVersionGuard"/> class bound to a committed sequence counter.
        /// </summary>
        /// <param name="committed">Last fully-published sequence counter. Writers increment it only after changes are visible.</param>
        public ReadWriteVersionGuard(SequenceNumber committed)
        {
            _committed = committed ?? throw new ArgumentNullException(nameof(committed));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadWriteVersionGuard"/> class with an internal sequence counter starting at zero.
        /// </summary>
        public ReadWriteVersionGuard() : this(new SequenceNumber(0))
        {
        }

        /// <summary>
        /// Atomically reads the committed sequence version and pins it against background compaction reclamation.
        /// </summary>
        public Snapshot Acquire()
        {
            lock (_gate)
            {
                long v = _committed.Value;
                _pins.TryGetValue(v, out int count);
                _pins[v] = count + 1;
                return new Snapshot(this, v);
            }
        }

        /// <summary>
        /// Pins a specific explicit version number for testing or custom transaction scopes.
        /// </summary>
        public Snapshot Acquire(long version)
        {
            lock (_gate)
            {
                _pins.TryGetValue(version, out int count);
                _pins[version] = count + 1;
                return new Snapshot(this, version);
            }
        }

        /// <summary>
        /// Pins a specific explicit version number and returns an <see cref="IDisposable"/> token.
        /// </summary>
        public IDisposable AcquireReader(long version) => Acquire(version);

        /// <summary>
        /// Evaluates active reader leases to identify the lowest transaction sequence version currently being accessed.
        /// </summary>
        public long GetMinActiveVersion()
        {
            lock (_gate)
            {
                foreach (long v in _pins.Keys)
                {
                    return v; // First key in SortedDictionary is always the minimum
                }
                return _committed.Value;
            }
        }

        /// <summary>
        /// Evaluates active reader leases to identify the lowest transaction sequence version currently being accessed,
        /// or returns the provided default version if no reader leases are currently active.
        /// </summary>
        public long GetMinActiveVersion(long defaultVersion)
        {
            lock (_gate)
            {
                foreach (long v in _pins.Keys)
                {
                    return v;
                }
                return defaultVersion;
            }
        }

        private void Release(long version)
        {
            lock (_gate)
            {
                if (_pins.TryGetValue(version, out int count))
                {
                    if (count <= 1)
                    {
                        _pins.Remove(version);
                    }
                    else
                    {
                        _pins[version] = count - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Represents an active, pinned reader lease that guards against compaction reclamation until disposed.
        /// </summary>
        public sealed class Snapshot : IDisposable
        {
            private readonly ReadWriteVersionGuard _guard;
            private int _released;

            internal Snapshot(ReadWriteVersionGuard guard, long version)
            {
                _guard = guard;
                Version = version;
            }

            /// <summary>
            /// The pinned snapshot sequence version.
            /// </summary>
            public long Version { get; }

            /// <summary>
            /// Releases the snapshot pin.
            /// </summary>
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    _guard.Release(Version);
                }
            }
        }
    }
}
