using System.Security.Cryptography;
using Aarkam.Kdouja.LSM.Primitives.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Cryptography;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Cryptography)]
public sealed class ChaCha20Poly1305CryptographyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32)]
    [InlineData(255)]
    [InlineData(1024)]
    public void ChaCha20Poly1305Encryption_EncryptAndDecrypt_RoundtripSucceeds(int payloadLength)
    {
        var encryption = new ChaCha20Poly1305Encryption();
        byte[] key = TestDataGenerator.CreateDeterministicBytes(CryptographicConstants.ChaCha20KeySize, seed: 11);
        byte[] nonce = TestDataGenerator.CreateDeterministicBytes(CryptographicConstants.ChaCha20Poly1305NonceSize, seed: 22);
        byte[] aad = "chacha-aad-header"u8.ToArray();
        byte[] plaintext = TestDataGenerator.CreateDeterministicBytes(payloadLength, seed: 33 + payloadLength);
        byte[] ciphertext = new byte[payloadLength];
        byte[] tag = new byte[CryptographicConstants.ChaCha20Poly1305TagSize];
        byte[] decrypted = new byte[payloadLength];

        encryption.Encrypt(plaintext, key, nonce, ciphertext, tag, aad);
        encryption.Decrypt(ciphertext, key, nonce, tag, decrypted, aad);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void ChaCha20Poly1305Encryption_TamperedCiphertextTagOrAssociatedData_ThrowsCryptographicException()
    {
        var encryption = new ChaCha20Poly1305Encryption();
        byte[] key = TestDataGenerator.CreateDeterministicBytes(CryptographicConstants.ChaCha20KeySize, seed: 4);
        byte[] nonce = TestDataGenerator.CreateDeterministicBytes(CryptographicConstants.ChaCha20Poly1305NonceSize, seed: 5);
        byte[] aad = "context-1"u8.ToArray();
        byte[] plaintext = TestDataGenerator.CreateDeterministicBytes(64, seed: 6);
        byte[] ciphertext = new byte[64];
        byte[] tag = new byte[CryptographicConstants.ChaCha20Poly1305TagSize];
        byte[] decrypted = new byte[64];

        encryption.Encrypt(plaintext, key, nonce, ciphertext, tag, aad);

        byte[] badCiphertext = (byte[])ciphertext.Clone();
        badCiphertext[10] ^= 0x80;
        Assert.ThrowsAny<CryptographicException>(() =>
            encryption.Decrypt(badCiphertext, key, nonce, tag, decrypted, aad));

        byte[] badTag = (byte[])tag.Clone();
        badTag[15] ^= 0x01;
        Assert.ThrowsAny<CryptographicException>(() =>
            encryption.Decrypt(ciphertext, key, nonce, badTag, decrypted, aad));

        Assert.ThrowsAny<CryptographicException>(() =>
            encryption.Decrypt(ciphertext, key, nonce, tag, decrypted, "context-2"u8));
    }

    [Theory]
    [InlineData(0, 12, 16)]
    [InlineData(16, 12, 16)]
    [InlineData(31, 12, 16)]
    [InlineData(33, 12, 16)]
    [InlineData(32, 0, 16)]
    [InlineData(32, 11, 16)]
    [InlineData(32, 13, 16)]
    [InlineData(32, 12, 0)]
    [InlineData(32, 12, 15)]
    [InlineData(32, 12, 17)]
    public void ChaCha20Poly1305Encryption_InvalidParameterLengths_ThrowsArgumentException(int keyLen, int nonceLen, int tagLen)
    {
        var encryption = new ChaCha20Poly1305Encryption();
        byte[] key = new byte[keyLen];
        byte[] nonce = new byte[nonceLen];
        byte[] tag = new byte[tagLen];
        byte[] data = new byte[16];
        byte[] output = new byte[16];

        Assert.Throws<ArgumentException>(() => encryption.Encrypt(data, key, nonce, output, tag));
        Assert.Throws<ArgumentException>(() => encryption.Decrypt(data, key, nonce, tag, output));
    }

    [Fact]
    public void ChaCha20Poly1305EncryptionFactory_And_Encryptor_RoundtripSucceeds()
    {
        var factory = new ChaCha20Poly1305EncryptionFactory();
        byte[] key = TestDataGenerator.CreateDeterministicBytes(CryptographicConstants.ChaCha20KeySize, seed: 91);

        using IAeadEncryptor encryptor = factory.CreateEncryptor(key);
        Assert.IsType<ChaCha20Poly1305Encryptor>(encryptor);

        for (int i = 0; i < 4; i++)
        {
            byte[] nonce = TestDataGenerator.CreateDeterministicBytes(CryptographicConstants.ChaCha20Poly1305NonceSize, seed: 400 + i);
            byte[] plaintext = TestDataGenerator.CreateDeterministicBytes(96, seed: 500 + i);
            byte[] ciphertext = new byte[96];
            byte[] tag = new byte[CryptographicConstants.ChaCha20Poly1305TagSize];
            byte[] decrypted = new byte[96];

            encryptor.Encrypt(plaintext, nonce, ciphertext, tag);
            encryptor.Decrypt(ciphertext, nonce, tag, decrypted);

            Assert.Equal(plaintext, decrypted);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    [InlineData(31)]
    [InlineData(64)]
    public void ChaCha20Poly1305Encryptor_InvalidKeySize_ThrowsArgumentException(int invalidKeySize)
    {
        byte[] badKey = new byte[invalidKeySize];
        Assert.Throws<ArgumentException>(() => new ChaCha20Poly1305Encryptor(badKey));
        Assert.Throws<ArgumentException>(() => new ChaCha20Poly1305EncryptionFactory().CreateEncryptor(badKey));
    }

    [Fact]
    public void ChaCha20Poly1305Encryptor_InvalidNonceOrTagSize_ThrowsArgumentException()
    {
        byte[] key = TestDataGenerator.CreateDeterministicBytes(CryptographicConstants.ChaCha20KeySize, seed: 92);
        using var encryptor = new ChaCha20Poly1305Encryptor(key);
        byte[] data = new byte[16];

        Assert.Throws<ArgumentException>(() =>
            encryptor.Encrypt(data, new byte[10], new byte[16], new byte[16]));
        Assert.Throws<ArgumentException>(() =>
            encryptor.Decrypt(data, new byte[12], new byte[10], new byte[16]));
    }
}
