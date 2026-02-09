using FluentAssertions;
using Microsoft.Extensions.Options;
using PaymentGateway.Application.Encryption;
using Xunit;

namespace PaymentGateway.Application.Tests;

public class AesEncryptionServiceTests
{
    private readonly AesEncryptionService _sut;

    public AesEncryptionServiceTests()
    {
        var options = Options.Create(new EncryptionOptions { Key = "TestEncryptionKey123" });
        _sut = new AesEncryptionService(options);
    }

    #region Round-Trip

    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginal()
    {
        var plaintext = "4111111111111111";

        var encrypted = _sut.Encrypt(plaintext);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_WithCvv_ReturnsOriginal()
    {
        var cvv = "123";

        var encrypted = _sut.Encrypt(cvv);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(cvv);
    }

    [Fact]
    public void EncryptDecrypt_WithSpecialCharacters_ReturnsOriginal()
    {
        var plaintext = "Hello! @#$%^&*() 日本語 émojis 🔒";

        var encrypted = _sut.Encrypt(plaintext);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_WithLongString_ReturnsOriginal()
    {
        var plaintext = new string('A', 10000);

        var encrypted = _sut.Encrypt(plaintext);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(plaintext);
    }

    #endregion

    #region Encryption Behavior

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachTime()
    {
        var plaintext = "4111111111111111";

        var encrypted1 = _sut.Encrypt(plaintext);
        var encrypted2 = _sut.Encrypt(plaintext);

        // Different IVs should produce different ciphertext
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void Encrypt_ProducesBase64Output()
    {
        var plaintext = "4111111111111111";

        var encrypted = _sut.Encrypt(plaintext);

        var act = () => Convert.FromBase64String(encrypted);
        act.Should().NotThrow();
    }

    [Fact]
    public void Encrypt_ProducesCiphertextDifferentFromPlaintext()
    {
        var plaintext = "4111111111111111";

        var encrypted = _sut.Encrypt(plaintext);

        encrypted.Should().NotBe(plaintext);
    }

    #endregion

    #region Empty/Null Handling

    [Fact]
    public void Encrypt_WithEmptyString_ReturnsEmpty()
    {
        var result = _sut.Encrypt("");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_WithNull_ReturnsNull()
    {
        var result = _sut.Encrypt(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void Decrypt_WithEmptyString_ReturnsEmpty()
    {
        var result = _sut.Decrypt("");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_WithNull_ReturnsNull()
    {
        var result = _sut.Decrypt(null!);

        result.Should().BeNull();
    }

    #endregion

    #region Key Handling

    [Fact]
    public void Constructor_WithMissingKey_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new EncryptionOptions { Key = "" });

        var act = () => new AesEncryptionService(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Encryption key*not configured*");
    }

    [Fact]
    public void DifferentKeys_ProduceIncompatibleCiphertext()
    {
        var options1 = Options.Create(new EncryptionOptions { Key = "Key1" });
        var options2 = Options.Create(new EncryptionOptions { Key = "Key2" });
        var service1 = new AesEncryptionService(options1);
        var service2 = new AesEncryptionService(options2);

        var encrypted = service1.Encrypt("secret");

        var act = () => service2.Decrypt(encrypted);
        // Different key should fail decryption (padding error or wrong data)
        act.Should().Throw<Exception>();
    }

    #endregion

    #region Invalid Input

    [Fact]
    public void Decrypt_WithInvalidBase64_Throws()
    {
        var act = () => _sut.Decrypt("not-valid-base64!!!");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decrypt_WithTruncatedCiphertext_Throws()
    {
        var encrypted = _sut.Encrypt("test data");
        var truncated = encrypted[..10]; // Truncate to break the IV + ciphertext structure

        var act = () => _sut.Decrypt(truncated);

        act.Should().Throw<Exception>();
    }

    #endregion
}