namespace Aarkam.Kdouja.LSM.Primitives.Tests;

/// <summary>
/// Standardized trait constants for organizing and filtering the test suite across CI/CD pipelines.
/// </summary>
public static class TestCategories
{
    public const string Category = "Category";
    public const string Subsystem = "Subsystem";

    // Test Pyramid Layers
    public const string Unit = "Unit";

    // Subsystems
    public const string Buffers = "Buffers";
    public const string Checksums = "Checksums";
    public const string Collections = "Collections";
    public const string Comparators = "Comparators";
    public const string Compression = "Compression";
    public const string Concurrency = "Concurrency";
    public const string Cryptography = "Cryptography";
    public const string Diagnostics = "Diagnostics";
    public const string Encoding = "Encoding";
    public const string Hashing = "Hashing";
    public const string IO = "IO";
    public const string Memory = "Memory";
    public const string VarInt = "VarInt";
}
