namespace Aarkam.Kdouja.LSM.Primitives.VarInt
{
    /// <summary>
    /// A high-performance, stateless utility providing zero-allocation encoding and decoding routines 
    /// for variable-length integers (LEB128 VarInts) and ZigZag-mapped signed integers.
    /// </summary>
    public static class VarIntEncoder
    {
        /// <summary>
        /// Encodes a 32-bit unsigned integer into the target destination byte span using variable-length LEB128 rules.
        /// </summary>
        public static int WriteUInt32(Span<byte> destination, uint value)
        {
            int bytesWritten = 0;
            while (value >= 0x80)
            {
                if (bytesWritten >= destination.Length)
                    throw new ArgumentException("Destination buffer is too small.", nameof(destination));

                destination[bytesWritten++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            }

            if (bytesWritten >= destination.Length)
                throw new ArgumentException("Destination buffer is too small.", nameof(destination));

            destination[bytesWritten++] = (byte)(value & 0x7F);
            return bytesWritten;
        }

        /// <summary>
        /// Attempts to decode a 32-bit unsigned integer out of a variable-length LEB128 encoded read-only byte source span.
        /// Rejects truncated buffers, 5th-byte overflows, and non-canonical overlong encodings.
        /// </summary>
        public static bool TryReadUInt32(ReadOnlySpan<byte> source, out uint value, out int bytesRead)
        {
            value = 0;
            bytesRead = 0;
            int shift = 0;

            for (int i = 0; i < source.Length && i < 5; i++, shift += 7)
            {
                byte b = source[i];
                if (i == 4 && b > 0x0F)
                {
                    return false; // Overflow beyond 32 bits or continuation bit set on 5th byte
                }

                value |= (uint)(b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                {
                    if (b == 0 && i > 0)
                    {
                        value = 0;
                        return false; // Non-canonical / overlong encoding
                    }
                    bytesRead = i + 1;
                    return true;
                }
            }

            value = 0;
            return false; // Truncated
        }

        /// <summary>
        /// Decodes a 32-bit unsigned integer out of a variable-length LEB128 encoded read-only byte source span.
        /// </summary>
        public static int ReadUInt32(ReadOnlySpan<byte> source, out uint value)
        {
            if (TryReadUInt32(source, out value, out int bytesRead))
            {
                return bytesRead;
            }
            throw new FormatException("Variable-length integer is truncated, malformed, non-canonical, or exceeds 32 bits.");
        }

        /// <summary>
        /// Encodes a 64-bit unsigned long integer into the target destination byte span using variable-length LEB128 rules.
        /// </summary>
        public static int WriteUInt64(Span<byte> destination, ulong value)
        {
            int bytesWritten = 0;
            while (value >= 0x80)
            {
                if (bytesWritten >= destination.Length)
                    throw new ArgumentException("Destination buffer is too small.", nameof(destination));

                destination[bytesWritten++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            }

            if (bytesWritten >= destination.Length)
                throw new ArgumentException("Destination buffer is too small.", nameof(destination));

            destination[bytesWritten++] = (byte)(value & 0x7F);
            return bytesWritten;
        }

        /// <summary>
        /// Attempts to decode a 64-bit unsigned integer out of a variable-length LEB128 encoded read-only byte source span.
        /// Rejects truncated buffers, 10th-byte overflows, and non-canonical overlong encodings.
        /// </summary>
        public static bool TryReadUInt64(ReadOnlySpan<byte> source, out ulong value, out int bytesRead)
        {
            value = 0;
            bytesRead = 0;
            int shift = 0;

            for (int i = 0; i < source.Length && i < 10; i++, shift += 7)
            {
                byte b = source[i];
                if (i == 9 && b > 0x01)
                {
                    return false; // Overflow beyond 64 bits or continuation bit set on 10th byte
                }

                value |= (ulong)(b & 0x7F) << shift;

                if ((b & 0x80) == 0)
                {
                    if (b == 0 && i > 0)
                    {
                        value = 0;
                        return false; // Non-canonical / overlong encoding
                    }
                    bytesRead = i + 1;
                    return true;
                }
            }

            value = 0;
            return false; // Truncated
        }

        /// <summary>
        /// Decodes a 64-bit unsigned long integer out of a variable-length LEB128 encoded read-only byte source span.
        /// </summary>
        public static int ReadUInt64(ReadOnlySpan<byte> source, out ulong value)
        {
            if (TryReadUInt64(source, out value, out int bytesRead))
            {
                return bytesRead;
            }
            throw new FormatException("Variable-length integer is truncated, malformed, non-canonical, or exceeds 64 bits.");
        }

        /// <summary>
        /// Encodes a signed 32-bit integer by combining standard ZigZag bit folding with LEB128 variable-length packing.
        /// </summary>
        public static int WriteInt32(Span<byte> destination, int value)
        {
            uint zigZagValue = (uint)((value << 1) ^ (value >> 31));
            return WriteUInt32(destination, zigZagValue);
        }

        /// <summary>
        /// Decodes a signed 32-bit integer from a ZigZag and LEB128 combined bit configuration sequence.
        /// </summary>
        public static int ReadInt32(ReadOnlySpan<byte> source, out int value)
        {
            int bytesRead = ReadUInt32(source, out uint zigZagValue);
            value = (int)(zigZagValue >> 1) ^ -(int)(zigZagValue & 1);
            return bytesRead;
        }

        /// <summary>
        /// Encodes a signed 64-bit integer by combining standard ZigZag bit folding with LEB128 variable-length packing.
        /// </summary>
        public static int WriteInt64(Span<byte> destination, long value)
        {
            ulong zigZagValue = (ulong)((value << 1) ^ (value >> 63));
            return WriteUInt64(destination, zigZagValue);
        }

        /// <summary>
        /// Decodes a signed 64-bit integer from a ZigZag and LEB128 combined bit configuration sequence.
        /// </summary>
        public static int ReadInt64(ReadOnlySpan<byte> source, out long value)
        {
            int bytesRead = ReadUInt64(source, out ulong zigZagValue);
            value = (long)(zigZagValue >> 1) ^ -(long)(zigZagValue & 1);
            return bytesRead;
        }

        /// <summary>
        /// Computes the exact number of bytes required to serialize a 32-bit unsigned integer using LEB128 rules.
        /// </summary>
        public static int GetUInt32Size(uint value)
        {
            int size = 1;
            while (value >= 0x80)
            {
                size++;
                value >>= 7;
            }
            return size;
        }

        /// <summary>
        /// Computes the exact number of bytes required to serialize a 64-bit unsigned long integer using LEB128 rules.
        /// </summary>
        public static int GetUInt64Size(ulong value)
        {
            int size = 1;
            while (value >= 0x80)
            {
                size++;
                value >>= 7;
            }
            return size;
        }

        /// <summary>
        /// Computes the exact number of bytes required to serialize a 32-bit signed integer using combined ZigZag and LEB128 workflows.
        /// </summary>
        public static int GetInt32Size(int value)
        {
            uint zigZagValue = (uint)((value << 1) ^ (value >> 31));
            return GetUInt32Size(zigZagValue);
        }

        /// <summary>
        /// Computes the exact number of bytes required to serialize a 64-bit signed long integer using combined ZigZag and LEB128 workflows.
        /// </summary>
        public static int GetInt64Size(long value)
        {
            ulong zigZagValue = (ulong)((value << 1) ^ (value >> 63));
            return GetUInt64Size(zigZagValue);
        }
    }
}
