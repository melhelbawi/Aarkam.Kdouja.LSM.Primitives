using System.Buffers.Binary;
using System.IO.Hashing;

namespace Aarkam.Kdouja.LSM.Primitives.Collections
{
    /// <summary>
    /// A high-performance, compact bit-array backed concrete implementation of a Bloom Filter.
    /// Utilizes Kirsch-Mitzenmacher double hashing with <see cref="XxHash64"/> for ultra-low latency execution loops.
    /// </summary>
    /// <remarks>
    /// Space complexity is minimized by packing individual boolean states directly into bits inside a flat <see cref="byte"/> array, keeping 
    /// memory overhead within highly restricted parameters even when tracking millions of keys.
    /// </remarks>
    public sealed class BloomFilter : IBloomFilter
    {
        /// <summary>
        /// Current on-disk serialization format version identifier.
        /// </summary>
        public const byte FormatVersion = 1;

        /// <summary>
        /// Identifier for the XxHash64 hashing algorithm used in double-hashing probes.
        /// </summary>
        public const byte HashAlgorithmXxHash64 = 1;

        /// <summary>
        /// Size in bytes of the serialized filter metadata header: [version:1][hashId:1][k:2 LE].
        /// </summary>
        public const int HeaderSize = 4;

        private const int MaxHashCount = 30;

        private readonly byte[] _bits;
        private readonly ulong _bitCount;
        private readonly int _hashCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter"/> class mathematically optimized for a targeted capacity and false-positive margin.
        /// </summary>
        /// <param name="expectedItems">The maximum expected total cardinality of unique items/keys to write to this filter segment before it gets flushed.</param>
        /// <param name="falsePositiveRate">The target acceptable error boundary represented as a decimal fraction (e.g., <c>0.01</c> equals a 1% chance of a false positive lookup).</param>
        public BloomFilter(int expectedItems, double falsePositiveRate = 0.01)
        {
            if (expectedItems <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedItems), "Expected items must be greater than zero.");
            if (double.IsNaN(falsePositiveRate) || falsePositiveRate <= 0 || falsePositiveRate >= 1)
                throw new ArgumentOutOfRangeException(nameof(falsePositiveRate), "False positive rate must be between 0 and 1.");

            double m = Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2)));
            if (m > int.MaxValue - 8)
                throw new ArgumentOutOfRangeException(nameof(expectedItems), "Filter size exceeds allowable limits.");

            int byteCount = Math.Max(8, ((int)m + 7) / 8);
            _bits = new byte[byteCount];
            _bitCount = (ulong)byteCount * 8;
            _hashCount = Math.Clamp((int)Math.Round((double)_bitCount / expectedItems * Math.Log(2)), 1, MaxHashCount);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BloomFilter"/> class directly from raw, hydrated state parameters.
        /// </summary>
        /// <param name="bits">The raw bit vector state payload array extracted from storage components.</param>
        /// <param name="hashCount">The exact quantity of hash functions associated with the configuration of this data array.</param>
        public BloomFilter(byte[] bits, int hashCount)
        {
            if (bits == null)
                throw new ArgumentNullException(nameof(bits));
            if (bits.Length == 0)
                throw new ArgumentException("Bits buffer cannot be empty.", nameof(bits));
            if (hashCount < 1 || hashCount > MaxHashCount)
                throw new ArgumentOutOfRangeException(nameof(hashCount), $"Hash count must be between 1 and {MaxHashCount}.");

            _bits = bits;
            _bitCount = (ulong)bits.Length * 8;
            _hashCount = hashCount;
        }

        /// <inheritdoc/>
        public void Add(ReadOnlySpan<byte> key)
        {
            ulong h = XxHash64.HashToUInt64(key);
            ulong x = (uint)h;
            ulong delta = (uint)(h >> 32) | 1UL;
            for (int i = 0; i < _hashCount; i++, x += delta)
            {
                ulong bit = x % _bitCount;
                _bits[bit >> 3] |= (byte)(1 << (int)(bit & 7));
            }
        }

        /// <inheritdoc/>
        public bool MayContain(ReadOnlySpan<byte> key)
        {
            ulong h = XxHash64.HashToUInt64(key);
            ulong x = (uint)h;
            ulong delta = (uint)(h >> 32) | 1UL;
            for (int i = 0; i < _hashCount; i++, x += delta)
            {
                ulong bit = x % _bitCount;
                if ((_bits[bit >> 3] & (1 << (int)(bit & 7))) == 0)
                    return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public byte[] Serialize()
        {
            var data = new byte[HeaderSize + _bits.Length];
            data[0] = FormatVersion;
            data[1] = HashAlgorithmXxHash64;
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(2, 2), (ushort)_hashCount);
            _bits.CopyTo(data, HeaderSize);
            return data;
        }

        /// <summary>
        /// De-serializes a contiguous binary segment containing previously serialized metadata parameters back into a valid execution <see cref="BloomFilter"/> object.
        /// </summary>
        public static BloomFilter Deserialize(ReadOnlySpan<byte> data)
        {
            if (data.Length <= HeaderSize)
                throw new InvalidDataException("Serialized bloom filter data is truncated or missing payload.");
            if (data[0] != FormatVersion || data[1] != HashAlgorithmXxHash64)
                throw new InvalidDataException($"Unsupported bloom filter format v{data[0]} / hash {data[1]}.");

            int hashCount = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(2, 2));
            if (hashCount < 1 || hashCount > MaxHashCount)
                throw new InvalidDataException($"Invalid bloom filter hash count: {hashCount}.");

            var bits = data[HeaderSize..].ToArray();
            return new BloomFilter(bits, hashCount);
        }
    }
}
