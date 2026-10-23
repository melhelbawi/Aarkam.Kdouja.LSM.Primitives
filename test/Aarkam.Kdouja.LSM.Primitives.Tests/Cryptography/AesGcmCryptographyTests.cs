using System.Security.Cryptography;
using Aarkam.Kdouja.LSM.Primitives.Cryptography;

namespace Aarkam.Kdouja.LSM.Primitives.Tests.Cryptography;

[Trait(TestCategories.Category, TestCategories.Unit)]
[Trait(TestCategories.Subsystem, TestCategories.Cryptography)]
public sealed class AesGcmCryptographyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(127)]
    [InlineData(1024)]
    public void AesGcmEncryption_EncryptAndDecrypt_RoundtripSucceeds(int payloadLength)
    {
        var encryption = new AesGcmEncryption();
        byte[] key = TestDataGenerator.CreateDeterministicBytes(AesGcmEncryption.KeySize, seed: 10);
        byte[] nonce = TestDataGenerator.CreateDeterministicBytes(AesGcmEncryption.NonceSize, seed: 20);
        byte[] aad = "block-header-aad"u8.ToArray();
        byte[] plaintext = TestDataGenerator.CreateDeterministicBytes(payloadLength, seed: 30 + payloadLength);
        byte[] ciphertext = new byte[payloadLength];
        byte[] tag = new byte[AesGcmEncryption.TagSize];
        byte[] decrypted = new byte[payloadLength];

        encryption.Encrypt(plaintext, key, nonce, ciphertext, tag, aad);
        encryption.Decrypt(ciphertext, key, nonce, tag, decrypted, aad);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void AesGcmEncryption_TamperedCiphertextTagOrAssociatedData_ThrowsCryptographicException()
    {
        var encryption = new AesGcmEncryption();
        byte[] key = TestDataGenerator.CreateDeterministicBytes(AesGcmEncryption.KeySize, seed: 1);
        byte[] nonce = TestDataGenerator.CreateDeterministicBytes(AesGcmEncryption.NonceSize, seed: 2);
        byte[] aad = "aad-v1"u8.ToArray();
        byte[] plaintext = TestDataGenerator.CreateDeterministicBytes(64, seed: 3);
        byte[] ciphertext = new byte[64];
        byte[] tag = new byte[AesGcmEncryption.TagSize];
        byte[] decrypted = new byte[64];

        encryption.Encrypt(plaintext, key, nonce, ciphertext, tag, aad);

        byte[] badCiphertext = (byte[])ciphertext.Clone();
        badCiphertext[0] ^= 0x01;
        Assert.ThrowsAny<CryptographicException>(() =>
            encryption.Decrypt(badCiphertext, key, nonce, tag, decrypted, aad));

        byte[] badTag = (byte[])tag.Clone();
        badTag[0] ^= 0x01;
        Assert.ThrowsAny<CryptographicException>(() =>
            encryption.Decrypt(ciphertext, key, nonce, badTag, decrypted, aad));

        Assert.ThrowsAny<CryptographicException>(() =>
            encryption.Decrypt(ciphertext, key, nonce, tag, decrypted, "aad-v2"u8));
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
    public void AesGcmEncryption_InvalidParameterLengths_ThrowsArgumentException(int keyLen, int nonceLen, int tagLen)
    {
        var encryption = new AesGcmEncryption();
        byte[] key = new byte[keyLen];
        byte[] nonce = new byte[nonceLen];
        byte[] tag = new byte[tagLen];
        byte[] data = new byte[16];
        byte[] output = new byte[16];

        Assert.Throws<ArgumentException>(() => encryption.Encrypt(data, key, nonce, output, tag));
        Assert.Throws<ArgumentException>(() => encryption.Decrypt(data, key, nonce, tag, output));
    }

    [Fact]
    public void AesGcmEncryptionFactory_And_AesGcmEncryptor_RoundtripAcrossMultipleBlocks()
    {
        var factory = new AesGcmEncryptionFactory();
        byte[] key = TestDataGenerator.CreateDeterministicBytes(AesGcmEncryption.KeySize, seed: 77);

        using IAeadEncryptor encryptor = factory.CreateEncryptor(key);
        Assert.IsType<AesGcmEncryptor>(encryptor);

        for (int blockIndex = 0; blockIndex < 5; blockIndex++)
        {
            byte[] nonce = TestDataGenerator.CreateDeterministicBytes(AesGcmEncryption.NonceSize, seed: 200 + blockIndex);
            byte[] plaintext = TestDataGenerator.CreateDeterministicBytes(128, seed: 300 + blockIndex);
            byte[] ciphertext = new byte[128];
            byte[] tag = new byte[AesGcmEncryption.TagSize];
            byte[] decrypted = new byte[128];

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
    public void AesGcmEncryptor_InvalidKeySize_ThrowsArgumentException(int invalidKeySize)
    {
        byte[] badKey = new byte[invalidKeySize];
        Assert.Throws<ArgumentException>(() => new AesGcmEncryptor(badKey));
        Assert.Throws<ArgumentException>(() => new AesGcmEncryptionFactory().CreateEncryptor(badKey));
    }

    [Fact]
    public void AesGcmEncryptor_InvalidNonceOrTagSize_ThrowsArgumentException()
    {
        byte[] key = TestDataGenerator.CreateDeterministicBytes(AesGcmEncryption.KeySize, seed: 88);
        using var encryptor = new AesGcmEncryptor(key);
        byte[] data = new byte[16];

        Assert.Throws<ArgumentException>(() =>
            encryptor.Encrypt(data, new byte[8], new byte[16], new byte[16]));
        Assert.Throws<ArgumentException>(() =>
            encryptor.Decrypt(data, new byte[12], new byte[8], new byte[16]));
    }
}
