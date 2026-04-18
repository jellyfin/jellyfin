using System;
using Emby.Server.Implementations.Cryptography;
using MediaBrowser.Model.Cryptography;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Cryptography;

public class CryptographyProviderTests
{
    private readonly CryptographyProvider _sut = new();

    [Fact]
    public void CreatePasswordHash_WithPassword_ReturnsHashWithIterations()
    {
        var hash = _sut.CreatePasswordHash("testpassword");

        Assert.Equal("PBKDF2-SHA512", hash.Id);
        Assert.True(hash.Parameters.ContainsKey("iterations"));
        Assert.NotEmpty(hash.Salt.ToArray());
        Assert.NotEmpty(hash.Hash.ToArray());
    }

    [Fact]
    public void Verify_WithValidPassword_ReturnsTrue()
    {
        const string password = "testpassword";
        var hash = _sut.CreatePasswordHash(password);

        Assert.True(_sut.Verify(hash, password));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.CreatePasswordHash("correctpassword");

        Assert.False(_sut.Verify(hash, "wrongpassword"));
    }

    [Fact]
    public void Verify_PBKDF2_MissingIterations_ThrowsFormatException()
    {
        var hash = PasswordHash.Parse("$PBKDF2$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D");

        var exception = Assert.Throws<FormatException>(() => _sut.Verify(hash, "password"));
        Assert.Contains("missing required 'iterations' parameter", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_PBKDF2SHA512_MissingIterations_ThrowsFormatException()
    {
        var hash = PasswordHash.Parse("$PBKDF2-SHA512$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D");

        var exception = Assert.Throws<FormatException>(() => _sut.Verify(hash, "password"));
        Assert.Contains("missing required 'iterations' parameter", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_PBKDF2_InvalidIterationsFormat_ThrowsFormatException()
    {
        var hash = PasswordHash.Parse("$PBKDF2$iterations=abc$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D");

        var exception = Assert.Throws<FormatException>(() => _sut.Verify(hash, "password"));
        Assert.Contains("invalid 'iterations' parameter", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_PBKDF2SHA512_InvalidIterationsFormat_ThrowsFormatException()
    {
        var hash = PasswordHash.Parse("$PBKDF2-SHA512$iterations=notanumber$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D");

        var exception = Assert.Throws<FormatException>(() => _sut.Verify(hash, "password"));
        Assert.Contains("invalid 'iterations' parameter", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_UnsupportedHashId_ThrowsNotSupportedException()
    {
        var hash = PasswordHash.Parse("$UNKNOWN$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D");

        Assert.Throws<NotSupportedException>(() => _sut.Verify(hash, "password"));
    }

    [Fact]
    public void GenerateSalt_ReturnsNonEmptyArray()
    {
        var salt = _sut.GenerateSalt();

        Assert.NotEmpty(salt);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void GenerateSalt_WithLength_ReturnsArrayOfSpecifiedLength(int length)
    {
        var salt = _sut.GenerateSalt(length);

        Assert.Equal(length, salt.Length);
    }
}
