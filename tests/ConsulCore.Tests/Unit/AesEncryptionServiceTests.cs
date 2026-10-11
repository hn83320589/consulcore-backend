using System.Security.Cryptography;
using consulcore_backend.Data.Encryption;
using Microsoft.Extensions.Configuration;

namespace ConsulCore.Tests.Unit;

public class AesEncryptionServiceTests
{
    private static AesEncryptionService CreateService(string? key) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Encryption:Key"] = key })
            .Build());

    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Theory]
    [InlineData("王小明")]
    [InlineData("")]
    [InlineData("自殺意念：無；用藥：Sertraline 50mg")]
    public void Encrypt_then_decrypt_returns_original_text(string plainText)
    {
        var service = CreateService(NewKey());

        Assert.Equal(plainText, service.Decrypt(service.Encrypt(plainText)));
    }

    [Fact]
    public void Same_plaintext_encrypts_differently_each_time()
    {
        var service = CreateService(NewKey());

        Assert.NotEqual(service.Encrypt("王小明"), service.Encrypt("王小明"));
    }

    [Fact]
    public void Tampered_ciphertext_is_rejected()
    {
        var service = CreateService(NewKey());
        var bytes = Convert.FromBase64String(service.Encrypt("王小明"));
        bytes[^1] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void Ciphertext_from_another_key_is_rejected()
    {
        var cipher = CreateService(NewKey()).Encrypt("王小明");

        Assert.ThrowsAny<CryptographicException>(() => CreateService(NewKey()).Decrypt(cipher));
    }

    [Fact]
    public void Missing_key_fails_at_construction()
    {
        Assert.Throws<InvalidOperationException>(() => CreateService(null));
    }

    [Fact]
    public void Key_that_is_not_256_bits_fails_at_construction()
    {
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

        Assert.Throws<InvalidOperationException>(() => CreateService(shortKey));
    }
}
