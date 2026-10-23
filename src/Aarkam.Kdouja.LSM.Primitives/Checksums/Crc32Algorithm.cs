using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

#if NET8_0_OR_GREATER
using ArmCrc = System.Runtime.Intrinsics.Arm.Crc32;
#endif

namespace Aarkam.Kdouja.LSM.Primitives.Checksums
{
    /// <summary>
    /// Provides a high-performance, thread-safe implementation of the CRC32-C (Castagnoli) checksum algorithm.
    /// This class automatically utilizes hardware-accelerated CPU instructions (SSE4.2 for x86/x64 and ARMv8 intrinsics for ARM64) 
    /// with a fallback to a software-based lookup table when hardware support is unavailable.
    /// </summary>
    /// <remarks>
    /// The Castagnoli polynomial used is <c>0x1EDC6F41</c> (bit-reflected as <c>0x82F63B78</c>). 
    /// This specific variant offers superior error-detection properties compared to the standard IEEE 802.3 CRC-32 polynomial.
    /// </remarks>
    public sealed class Crc32Algorithm : IChecksum
    {
        /// <summary>
        /// Gets the thread-safe, globally accessible singleton instance of the <see cref="Crc32Algorithm"/> class.
        /// </summary>
        public static readonly Crc32Algorithm Instance = new();

        private const uint Poly = 0x82F63B78;
        private const uint MaskDelta = 0xA282EAD8;

        /// <summary>
        /// A precomputed 256-element lookup table used as a high-speed fallback mechanism 
        /// when hardware acceleration instructions are not supported by the host CPU.
        /// </summary>
        private static readonly uint[] _table;

        static Crc32Algorithm()
        {
            _table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint res = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((res & 1) == 1)
                        res = (res >> 1) ^ Poly;
                    else
                        res >>= 1;
                }
                _table[i] = res;
            }
        }

        private Crc32Algorithm() { }

        /// <inheritdoc />
        public string Name => "CRC32-C";

        private static readonly bool _isSse42Supported = Sse42.IsSupported;

#if NET8_0_OR_GREATER
        private static readonly bool _isArmCrc32Supported = ArmCrc.Arm64.IsSupported;
#endif

        /// <inheritdoc />
        public uint Compute(ReadOnlySpan<byte> data)
        {
            Diagnostics.PrimitiveMetrics.ChecksumVerified.Add(1);
            return Extend(0, data);
        }

        /// <inheritdoc />
        public uint Extend(uint crc, ReadOnlySpan<byte> data)
        {
            return ~Update(~crc, data);
        }

        /// <summary>
        /// Masks a CRC value before persisting it in storage formats where the data being protected itself includes a CRC field.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Mask(uint crc) => ((crc >> 15) | (crc << 17)) + MaskDelta;

        /// <summary>
        /// Unmasks a previously masked CRC value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Unmask(uint masked)
        {
            uint rot = masked - MaskDelta;
            return (rot >> 17) | (rot << 15);
        }

        private static uint Update(uint crc, ReadOnlySpan<byte> data)
        {
            ref byte r = ref MemoryMarshal.GetReference(data);
            int len = data.Length, i = 0;

            // Path A: x86/x64 SSE4.2 Hardware Acceleration
            if (_isSse42Supported)
            {
                if (Sse42.X64.IsSupported && len >= 8)
                {
                    for (; i <= len - 8; i += 8)
                    {
                        crc = (uint)Sse42.X64.Crc32(crc, Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref r, i)));
                    }
                }

                for (; i <= len - 4; i += 4)
                {
                    crc = Sse42.Crc32(crc, Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref r, i)));
                }

                if (i <= len - 2)
                {
                    crc = Sse42.Crc32(crc, Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref r, i)));
                    i += 2;
                }

                for (; i < len; i++)
                {
                    crc = Sse42.Crc32(crc, Unsafe.Add(ref r, i));
                }

                return crc;
            }
#if NET8_0_OR_GREATER
            // Path B: ARM64 Hardware Acceleration (.NET 8+)
            else if (_isArmCrc32Supported)
            {
                for (; i <= len - 8; i += 8)
                {
                    crc = ArmCrc.Arm64.ComputeCrc32C(crc, Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref r, i)));
                }

                // Sub-8-byte tails MUST use parent class ArmCrc (uint/ushort/byte) overloads, not Arm64's ulong overload
                for (; i <= len - 4; i += 4)
                {
                    crc = ArmCrc.ComputeCrc32C(crc, Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref r, i)));
                }

                if (i <= len - 2)
                {
                    crc = ArmCrc.ComputeCrc32C(crc, Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref r, i)));
                    i += 2;
                }

                for (; i < len; i++)
                {
                    crc = ArmCrc.ComputeCrc32C(crc, Unsafe.Add(ref r, i));
                }

                return crc;
            }
#endif

            // Path C: Managed Software Fallback
            for (; i < len; i++)
            {
                crc = _table[(byte)(crc ^ Unsafe.Add(ref r, i))] ^ (crc >> 8);
            }
            return crc;
        }
    }
}
