using Aarkam.Kdouja.LSM.Primitives.Compression;
using System.Collections.Concurrent;

namespace Aarkam.Kdouja.LSM.Primitives.Compression
{
    /// <summary>
    /// A thread-safe, centralized service locator and registry for managing and resolving data compression algorithms within the storage subsystem.
    /// </summary>
    public static class CompressionCodecFactory
    {
        private static readonly ConcurrentDictionary<CompressionType, ICompressionCodec> Codecs = new();

        static CompressionCodecFactory()
        {
            Codecs[CompressionType.None] = new IdentityCompressionCodec();
            Codecs[CompressionType.Deflate] = new DeflateCompressionCodec();
        }

        /// <summary>
        /// Resolves the globally registered compression codec corresponding to the provided <see cref="CompressionType"/> flag.
        /// </summary>
        /// <param name="type">The targeted compression format enum flag to resolve.</param>
        /// <returns>The registered <see cref="ICompressionCodec"/> instance associated with the provided flag format.</returns>
        /// <exception cref="NotSupportedException">Thrown when no codec is registered for the specified <paramref name="type"/>.</exception>
        public static ICompressionCodec GetCodec(CompressionType type)
        {
            if (Codecs.TryGetValue(type, out var codec))
            {
                return codec;
            }
            throw new NotSupportedException($"No compression codec registered for '{type}'.");
        }

        /// <summary>
        /// Checks whether a compression codec is registered and available for the specified type.
        /// </summary>
        public static bool IsAvailable(CompressionType type) => Codecs.ContainsKey(type);

        /// <summary>
        /// Registers a concrete compression codec instance for a specific <see cref="CompressionType"/> format signature.
        /// </summary>
        public static void RegisterCodec(CompressionType type, ICompressionCodec codec)
        {
            Codecs[type] = codec ?? throw new ArgumentNullException(nameof(codec));
        }

        /// <summary>
        /// Registers a concrete compression codec instance for the specified type flag.
        /// </summary>
        public static void Register(CompressionType type, ICompressionCodec codec)
        {
            RegisterCodec(type, codec);
        }
    }
}
