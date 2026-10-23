namespace Aarkam.Kdouja.LSM.Primitives.Tests;

/// <summary>
/// Deterministic data generator for reproducible byte arrays, keys, values, and payloads.
/// </summary>
public static class TestDataGenerator
{
    public static byte[] CreateDeterministicBytes(int length, int seed = 42)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (length == 0)
        {
            return [];
        }

        var rng = new Random(seed);
        byte[] buffer = new byte[length];
        rng.NextBytes(buffer);
        return buffer;
    }

    public static byte[] CreateKeyBytes(string prefix, int index, int padWidth = 8)
    {
        string formatted = $"{prefix}:{index.ToString($"D{padWidth}")}";
        return System.Text.Encoding.UTF8.GetBytes(formatted);
    }

    public static byte[] CreateUtf8Bytes(string value) => System.Text.Encoding.UTF8.GetBytes(value);

    public static byte[] CreateRepeatingPattern(int length, ReadOnlySpan<byte> pattern)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (length == 0 || pattern.IsEmpty)
        {
            return [];
        }

        byte[] result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = pattern[i % pattern.Length];
        }

        return result;
    }
}
