<div align="center">

# Aarkam.Kdouja.LSM.Primitives

**Zero-Allocation, Hardware-Accelerated Storage Engine Primitives for .NET**

[![NuGet Version](https://img.shields.io/nuget/v/Aarkam.Kdouja.Primitives.svg?style=flat-square&color=0078D4&logo=nuget)](https://www.nuget.org/packages/Aarkam.Kdouja.Primitives)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Aarkam.Kdouja.Primitives.svg?style=flat-square&color=0078D4)](https://www.nuget.org/packages/Aarkam.Kdouja.Primitives)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](https://github.com/melhelbawi/Aarkam.Kdouja.LSM.Primitives/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%2010.0-512BD4.svg?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![AOT & Trimming](https://img.shields.io/badge/AOT%20%26%20Trimming-Supported-success.svg?style=flat-square)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![Zero Allocation](https://img.shields.io/badge/Memory-Zero--Allocation%20%2F%20Spans-orange.svg?style=flat-square)](https://learn.microsoft.com/en-us/dotnet/api/system.span-1)

<p align="center">
  <a href="#overview">Overview</a> •
  <a href="#key-features">Key Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#primitives-catalog">Primitives Catalog</a> •
  <a href="#code-examples">Code Examples</a> •
  <a href="#observability">Observability</a> •
  <a href="#license">License</a>
</p>

---

</div>

## Overview

**Aarkam.Kdouja.LSM.Primitives** is an ultra-high-performance suite of foundational storage primitives engineered for building log-structured merge-tree (LSM-Tree) storage engines, embedded key-value stores, transactional write-ahead logs (WAL), and distributed database storage tiers in .NET.

Originating as the low-level foundation of the **Kdouja LSM-Tree Engine** by [Aarkam](https://aarkam.io/), this library provides bare-metal memory structures, SIMD-accelerated checksums, authenticated AEAD cryptographic engines, atomic multi-version concurrency control (MVCC) structures, variable-length integer codecs, and I/O rate-limiters.

Every component is architected with a strict **zero-heap-allocation** philosophy, utilizing `Span<byte>`, `ReadOnlySpan<byte>`, `ref struct` formatters, hardware intrinsics (SSE4.2, ARMv8, AVX2, AVX-512), and modern .NET inline arrays.

---

## Key Features

- ⚡ **Zero Garbage Collection Overhead**: Hot execution paths operate exclusively over `Span<T>`, `ReadOnlySpan<T>`, stack-allocated `ref struct` serializes, and pooled memory buffers.
- 🚀 **Hardware-Accelerated CRC32-C**: Castagnoli polynomial checksum implementation utilizing hardware instructions (`SSE4.2` on x86/x64, `ARMv8` CRC on ARM64) with fallback lookup tables and frame bit-masking.
- 🔒 **AEAD Cryptography with Key Caching**: Production-grade authenticated encryption via **AES-256-GCM** and **ChaCha20-Poly1305** using pre-computed key schedules, atomic counter nonces, and HKDF (RFC 5869) key derivation.
- 📦 **Stack-Bound Inline Buffers**: Zero-allocation, fixed-size value structures (`Buffer12`, `Buffer16`, `Buffer32`, `Buffer64`) leveraging .NET 8 `[InlineArray]` with constant-time equality and secure memory zeroing.
- 🔍 **Double-Hashing Bloom Filters**: Compact bit-vector Bloom Filter applying the Kirsch-Mitzenmacher optimization with `XxHash64` for maximum false-positive suppression with minimal memory footprints.
- 🔢 **LEB128 VarInts & ZigZag Encoding**: Ultra-fast variable-length integer serialization with canonical non-overlong validation and overflow guards for compact on-disk index headers.
- ⏱️ **Atomic MVCC Version Coordination**: Hardware `Interlocked` 64-bit sequence counters and lock-free snapshot visibility pinning (`ReadWriteVersionGuard`) to enforce read-committed snapshots and safe background compaction pruning.
- 🗜️ **Extensible Compression Pipeline**: High-speed compression codec registry with zero-copy Identity, Deflate, and hooks for Snappy, Zstandard, and LZ4.
- 🚦 **GCRA Virtual-Time Rate Limiter**: High-precision token bucket based on the Generic Cell Rate Algorithm to throttle background compaction and WAL flushes without thread pool starvation.
- 📊 **Native OpenTelemetry Instrumentation**: Zero-allocation `System.Diagnostics.Metrics` counters tracking I/O byte volumes, memory rentals, and encryption throughput out-of-the-box.
- 🎯 **Native AOT & Trimmable**: Fully compatible with ahead-of-time compilation and aggressive assembly trimming for lightweight cloud containers and microservices.

---

## Target Frameworks

| Runtime | Support Level | Hardware Acceleration | Inline Arrays |
| :--- | :--- | :--- | :--- |
| **.NET 10.0** | Native Preview Features | Full (SSE4.2, AVX2, AVX-512, ARMv8) | `InlineArrayAttribute` |
| **.NET 8.0 (LTS)** | Long-Term Support | Full (SSE4.2, AVX2, ARMv8) | `InlineArrayAttribute` |
| **.NET 6.0 (LTS)** | Extended Compatibility | Hardware Intrinsics & Vector fallback | Sequential Struct Layout |

---

## Installation

Install the package via the .NET CLI:

```bash
dotnet add package Aarkam.Kdouja.Primitives
```

Or using the Package Manager Console in Visual Studio:

```powershell
Install-Package Aarkam.Kdouja.Primitives
```

Or add it directly to your `.csproj`:

```xml
<PackageReference Include="Aarkam.Kdouja.Primitives" Version="1.0.0" />
```

---

## Primitives Catalog

| Namespace | Key Types | Primary Responsibility |
| :--- | :--- | :--- |
| `Aarkam.Kdouja.LSM.Primitives.Buffers` | `Buffer12`, `Buffer16`, `Buffer32`, `Buffer64` | Stack-allocated, fixed-size inline byte buffers with zero heap allocations, constant-time equality, and secure zeroing. |
| `Aarkam.Kdouja.LSM.Primitives.Checksums` | `IChecksum`, `Crc32Algorithm` | Castagnoli CRC32-C calculation with SSE4.2 and ARMv8 hardware acceleration, incremental updates, and bit-masking. |
| `Aarkam.Kdouja.LSM.Primitives.Collections` | `BloomFilter`, `IBloomFilter`, `RingBuffer<T>` | Kirsch-Mitzenmacher double-hashing Bloom Filter (`XxHash64`) and bounded, lock-free circular ring queues. |
| `Aarkam.Kdouja.LSM.Primitives.Comparators` | `IKeyComparator`, `DefaultKeyComparator`, `SpanLexicographicalComparer` | RocksDB/LevelDB-style bytewise key ordering with prefix truncation (`FindShortestSeparator`, `FindShortSuccessor`). |
| `Aarkam.Kdouja.LSM.Primitives.Compression` | `ICompressionCodec`, `CompressionCodecFactory`, `DeflateCompressionCodec`, `IdentityCompressionCodec` | Pluggable block compression subsystem with thread-safe factory registry and bounded buffer sizing. |
| `Aarkam.Kdouja.LSM.Primitives.Concurrency` | `SequenceNumber`, `ReadWriteVersionGuard` | Monotonically increasing atomic 64-bit sequence counters and MVCC point-in-time snapshot pinning. |
| `Aarkam.Kdouja.LSM.Primitives.Cryptography` | `IAeadEncryptor`, `AeadEncryptorFactory`, `AesGcmEncryptor`, `ChaCha20Poly1305Encryptor`, `HkdfKeyDerivation`, `NonceGenerator`, `SecureRandom` | Reusable, key-cached authenticated AEAD ciphers, HKDF key derivation, and counter-hardened nonce generators. |
| `Aarkam.Kdouja.LSM.Primitives.Encoding` | `BinarySpanWriter`, `BinarySpanReader` | Stack-allocated `ref struct` binary formatters for zero-allocation Little-Endian serialization directly over spans. |
| `Aarkam.Kdouja.LSM.Primitives.Hashing` | `IHashFunction`, `XxHash32Algorithm`, `Fnv1aHash32` | High-throughput 32-bit xxHash and lightweight Fowler-Noll-Vo (FNV-1a) key hashing routines. |
| `Aarkam.Kdouja.LSM.Primitives.IO` | `SequentialFileWriter`, `SequentialFileReader`, `TokenBucketRateLimiter` | Forward-only buffered sequential disk streaming with automatic threshold bypass and GCRA virtual-time rate-limiting. |
| `Aarkam.Kdouja.LSM.Primitives.Memory` | `PooledBuffer`, `VectorMath` | `IMemoryOwner<byte>` memory leasing with auto-zeroing and SIMD hardware-vectorized block comparisons. |
| `Aarkam.Kdouja.LSM.Primitives.VarInt` | `VarIntEncoder` | Variable-length LEB128 unsigned integers and ZigZag signed 32-bit/64-bit integer codecs. |
| `Aarkam.Kdouja.LSM.Primitives.Diagnostics` | `PrimitiveMetrics` | High-performance OpenTelemetry `Meter` tracking disk I/O, array rentals, and cryptographic throughput. |

---

## Code Examples

### 1. Hardware-Accelerated Castagnoli CRC32-C Checksum

Use `Crc32Algorithm` to verify block integrity in Write-Ahead Logs (WAL) or SSTable data blocks. Includes frame masking to prevent self-collision when writing checksums into file headers:

```csharp
using Aarkam.Kdouja.LSM.Primitives.Checksums;

ReadOnlySpan<byte> recordPayload = "TRANSACTION_COMMIT_ENTRY_DATA"u8;

// Compute checksum using hardware intrinsics (SSE4.2 or ARMv8)
uint rawChecksum = Crc32Algorithm.Instance.Compute(recordPayload);

// Mask the checksum before storing in log record header (RocksDB/LevelDB standard)
uint maskedChecksum = Crc32Algorithm.Instance.Mask(rawChecksum);

// During recovery: unmask and verify
uint unmasked = Crc32Algorithm.Instance.Unmask(maskedChecksum);
bool isValid = (unmasked == Crc32Algorithm.Instance.Compute(recordPayload));
```

---

### 2. Zero-Allocation Binary Formatting with `BinarySpanWriter` and `BinarySpanReader`

Serialize and deserialize record headers on the stack with zero GC pressure:

```csharp
using Aarkam.Kdouja.LSM.Primitives.Encoding;

Span<byte> memoryBuffer = stackalloc byte[64];

// Stack-allocated writer (ref struct)
var writer = new BinarySpanWriter(memoryBuffer);
writer.WriteUInt32LittleEndian(0x4B444A41); // Magic header: "KDJA"
writer.WriteInt64LittleEndian(1048576);       // Sequence Number / Offset
writer.WriteByte(1);                         // Record Type
writer.WriteBytes("KEY_PAYLOAD"u8);          // Variable key payload

ReadOnlySpan<byte> writtenBytes = writer.WrittenSpan;

// Stack-allocated reader (ref struct)
var reader = new BinarySpanReader(writtenBytes);
uint magic = reader.ReadUInt32LittleEndian();
long sequence = reader.ReadInt64LittleEndian();
byte recordType = reader.ReadByte();
ReadOnlySpan<byte> key = reader.ReadBytes(11);
```

---

### 3. High-Throughput AEAD Encryption with Cached Key Schedules

Standard `AesGcm` implementations recreate round-keys on every instantiation. `AeadEncryptorFactory` creates long-lived, reusable encryptor workers with cached key schedules for hot-path WAL logging:

```csharp
using Aarkam.Kdouja.LSM.Primitives.Cryptography;

// 256-bit secret key (32 bytes)
ReadOnlySpan<byte> secretKey = stackalloc byte[32]; // Securely populated key
ReadOnlySpan<byte> nonce = stackalloc byte[12];     // 12-byte unique IV (e.g. from NonceGenerator)
ReadOnlySpan<byte> aad = "Block_Offset_4096"u8;     // Associated data prevents sector-swap attacks

ReadOnlySpan<byte> plaintext = "Confidential customer payload"u8;

Span<byte> ciphertext = stackalloc byte[plaintext.Length];
Span<byte> authTag = stackalloc byte[CryptographicConstants.AeadTagSize]; // 16 bytes

// Create reusable, key-cached encryptor worker
using IAeadEncryptor encryptor = AeadEncryptorFactory.CreateAesGcm(secretKey);

// Encrypt in-place with zero allocations
encryptor.Encrypt(plaintext, nonce, ciphertext, authTag, associatedData: aad);

// Decrypt and authenticate atomically
Span<byte> decrypted = stackalloc byte[ciphertext.Length];
encryptor.Decrypt(ciphertext, nonce, authTag, decrypted, associatedData: aad);
```

---

### 4. Kirsch-Mitzenmacher Double-Hashing Bloom Filter

Quickly reject negative key lookups to avoid expensive disk reads:

```csharp
using Aarkam.Kdouja.LSM.Primitives.Collections;

// Optimize for 1,000,000 keys with a 1% (0.01) false-positive probability
var bloomFilter = new BloomFilter(expectedItems: 1_000_000, falsePositiveRate: 0.01);

// Add keys
bloomFilter.Add("user_session_9281734"u8);
bloomFilter.Add("order_ref_55018"u8);

// Test membership
bool mightContain = bloomFilter.Contains("user_session_9281734"u8); // true
bool definitelyNot = bloomFilter.Contains("unknown_user_id"u8);      // false (100% certainty)

// Serialize to byte span for SSTable trailer persistence
byte[] buffer = new byte[bloomFilter.GetSerializedSize()];
bloomFilter.Serialize(buffer);

// Rehydrate Bloom Filter from storage block
var restoredFilter = BloomFilter.Deserialize(buffer);
```

---

### 5. Stack-Allocated Inline Buffers (`Buffer12`, `Buffer16`, `Buffer32`, `Buffer64`)

Avoid heap allocations when handling GUIDs, hashes, AES blocks, and nonces:

```csharp
using Aarkam.Kdouja.LSM.Primitives.Buffers;

// Stack-allocated 16-byte buffer (InlineArray in .NET 8+, StructLayout in .NET 6)
Buffer16 blockId = new Buffer16();
Guid.NewGuid().TryWriteBytes(blockId.AsSpan());

// Constant-time equality comparison prevents timing side-channel attacks
Buffer16 candidate = Buffer16.From(blockId.AsReadOnlySpan());
bool isEqual = blockId.ConstantTimeEquals(candidate);

// Cryptographically erase sensitive memory before exiting scope
blockId.SecureClear();
```

---

### 6. Variable-Length Integer Encoding (LEB128 & ZigZag)

Compress transaction identifiers, lengths, and file offsets into minimum byte widths:

```csharp
using Aarkam.Kdouja.LSM.Primitives.VarInt;

Span<byte> destination = stackalloc byte[10];

// Encode 64-bit integer using LEB128 (e.g. small number 42 occupies 1 byte instead of 8)
int bytesWritten = VarIntEncoder.WriteUInt64(destination, 42);

// Decode with overlong-canonical check and overflow safety
bool success = VarIntEncoder.TryReadUInt64(destination[..bytesWritten], out ulong decodedValue, out int bytesRead);

// ZigZag encoding for signed numbers (efficient negative representation)
int signedBytes = VarIntEncoder.WriteInt64(destination, -100);
VarIntEncoder.TryReadInt64(destination[..signedBytes], out long decodedSigned, out _);
```

---

### 7. Atomic MVCC Sequence Ordering & Snapshot Visibility Pinning

Coordinate multi-version concurrency control across concurrent transactions:

```csharp
using Aarkam.Kdouja.LSM.Primitives.Concurrency;

var committedSeq = new SequenceNumber(initialValue: 0);
var guard = new ReadWriteVersionGuard(committedSeq);

// Writer pipeline: atomically increment sequence number
long writeVersion = committedSeq.Increment();

// Reader pipeline: acquire snapshot pinned against background compaction
using (var snapshot = guard.Acquire())
{
    long readVersion = snapshot.Version;
    // Execute point-in-time query where record.Sequence <= readVersion
}

// Background compaction worker: query lowest active pinned snapshot
long safePruneThreshold = guard.GetEarliestPinnedVersion();
// All obsolete versions < safePruneThreshold can now be safely garbage-collected
```

---

### 8. Bytewise Lexicographical Key Comparator

RocksDB and LevelDB compatible key comparator supporting index boundary compression:

```csharp
using Aarkam.Kdouja.LSM.Primitives.Comparators;

IKeyComparator comparator = DefaultKeyComparator.Instance;

ReadOnlySpan<byte> keyA = "sensor/region-east/temp"u8;
ReadOnlySpan<byte> keyB = "sensor/region-west/temp"u8;

int order = comparator.Compare(keyA, keyB); // < 0

// Compress index blocks by finding shortest separator between two boundary keys
Span<byte> separator = "sensor/region-east/temp"u8.ToArray();
int shortenedLength = comparator.FindShortestSeparator(separator, limit: keyB);
// Shortened separator key occupies fewer bytes in the SSTable index block!
```

---

### 9. Buffered Sequential I/O with Direct Payload Bypass

High-throughput streaming writer designed for Write-Ahead Logs and SSTables:

```csharp
using Aarkam.Kdouja.LSM.Primitives.IO;

using var stream = File.OpenWrite("active_wal.log");
using var writer = new SequentialFileWriter(stream, bufferSize: 64 * 1024);

// Small writes are batched in the 64 KB memory staging buffer
writer.Write("RECORD_HEADER"u8);
writer.Write("RECORD_BODY"u8);

// Large writes exceeding buffer capacity bypass the buffer and flush directly
ReadOnlySpan<byte> largePage = new byte[128 * 1024];
writer.Write(largePage);

// Ensure all staged content is committed to disk
writer.Flush();
```

---

### 10. Bandwidth Rate Limiter (Generic Cell Rate Algorithm)

Prevent compaction background threads from saturating disk I/O and starving read requests:

```csharp
using Aarkam.Kdouja.LSM.Primitives.IO;

// Throttle compaction writes to a maximum of 50 MB/second
var limiter = new TokenBucketRateLimiter(bytesPerSecond: 50 * 1024 * 1024);

// In background compaction loop:
long batchSize = 1024 * 1024; // 1 MB
await limiter.AcquireAsync(batchSize);

// Proceed with writing batch to disk
```

---

## Observability

`Aarkam.Kdouja.LSM.Primitives` exposes zero-allocation telemetry out-of-the-box using the standard .NET `System.Diagnostics.Metrics` API (`Meter` name: `Aarkam.Kdouja.LSM.Primitives`).

### Available Instruments

| Instrument Name | Type | Unit | Description |
| :--- | :--- | :--- | :--- |
| `Aarkam.Kdouja.LSM.Primitives.io.bytes_read` | Counter (`long`) | bytes | Cumulative volume of bytes read via sequential file readers. |
| `Aarkam.Kdouja.LSM.Primitives.io.bytes_written` | Counter (`long`) | bytes | Cumulative volume of bytes committed via sequential file writers. |
| `Aarkam.Kdouja.LSM.Primitives.memory.buffers_rented` | Counter (`long`) | operations | Count of memory buffers rented from the shared array pool. |
| `Aarkam.Kdouja.LSM.Primitives.memory.buffers_returned` | Counter (`long`) | operations | Count of memory buffers returned to the shared array pool. |
| `Aarkam.Kdouja.LSM.Primitives.crypto.bytes_encrypted` | Counter (`long`) | bytes | Cumulative plaintext volume encrypted via AEAD workers. |
| `Aarkam.Kdouja.LSM.Primitives.crypto.bytes_decrypted` | Counter (`long`) | bytes | Cumulative ciphertext volume decrypted and verified. |

### Connecting to OpenTelemetry

```csharp
using OpenTelemetry.Metrics;

var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Aarkam.Kdouja.LSM.Primitives")
    .AddPrometheusExporter()
    .AddConsoleExporter()
    .Build();
```

---

## Performance & Design Philosophy

1. **Zero Garbage Collection (GC) Pressure**: In high-throughput storage engines, Garbage Collection pauses cause severe P99 latency spikes. All primitives avoid heap allocations during record parsing, checksumming, encryption, and variable-length encoding.
2. **Hardware Vectorization (SIMD)**: Memory comparisons, vector hashing, and CRC calculations delegate to hardware intrinsics (`SSE4.2`, `ARMv8`, `AVX2`, `AVX-512`).
3. **Cache Line Alignment**: Fixed-size structures (`Buffer12`, `Buffer16`, `Buffer32`, `Buffer64`) fit neatly into CPU L1/L2 cache lines to minimize cache misses.
4. **Security Hardening**: Constant-time comparisons and deterministic memory wiping (`CryptographicOperations.ZeroMemory`) protect encryption keys and plaintexts in memory.

---

## Building from Source

Clone the repository and build the solution:

```bash
git clone https://github.com/melhelbawi/Aarkam.Kdouja.LSM.Primitives.git
cd Aarkam.Kdouja.LSM.Primitives

# Build the solution
dotnet build Aarkam.Kdouja.LSM.Primitives.slnx --configuration Release

# Run tests
dotnet test test/Aarkam.Kdouja.LSM.Primitives.Tests/Aarkam.Kdouja.LSM.Primitives.Tests.csproj --configuration Release

# Pack NuGet package
dotnet pack src/Aarkam.Kdouja.LSM.Primitives/Aarkam.Kdouja.LSM.Primitives.csproj --configuration Release
```

---

## Contributing

Contributions, feedback, and pull requests are warmly welcome!
1. Fork the repository.
2. Create your feature branch (`git checkout -b feature/amazing-primitive`).
3. Commit your changes (`git commit -m 'Add amazing new primitive'`).
4. Push to the branch (`git push origin feature/amazing-primitive`).
5. Open a Pull Request.

Please ensure all tests pass and any new primitive maintains zero-heap-allocation semantics.

---

## License

This project is licensed under the [MIT License](https://github.com/melhelbawi/Aarkam.Kdouja.LSM.Primitives/blob/main/LICENSE).

Copyright &copy; 2026 [Aarkam](https://aarkam.io/) & [Mohamed Elhelbawi](https://github.com/melhelbawi). All rights reserved.
