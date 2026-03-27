using System;
using MediaBrowser.Common.StreamTokens;
using Xunit;

namespace Jellyfin.Common.Tests.StreamTokens;

public static class StreamTokenTests
{
    private const string Secret = "test-secret";
    private const string ItemId = "abc123";

    [Fact]
    public static void Generate_SameInputs_ReturnsSameToken()
    {
        var expires = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t1 = StreamToken.Generate(Secret, ItemId, expires);
        var t2 = StreamToken.Generate(Secret, ItemId, expires);
        Assert.Equal(t1, t2);
    }

    [Fact]
    public static void Generate_DifferentItem_ReturnsDifferentToken()
    {
        var expires = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t1 = StreamToken.Generate(Secret, ItemId, expires);
        var t2 = StreamToken.Generate(Secret, "other-item", expires);
        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public static void Generate_DifferentExpiry_ReturnsDifferentToken()
    {
        var t1 = StreamToken.Generate(Secret, ItemId, new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var t2 = StreamToken.Generate(Secret, ItemId, new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero));
        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public static void Generate_DifferentSecret_ReturnsDifferentToken()
    {
        var expires = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t1 = StreamToken.Generate(Secret, ItemId, expires);
        var t2 = StreamToken.Generate("other-secret", ItemId, expires);
        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public static void Validate_CorrectToken_ReturnsTrue()
    {
        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var token = StreamToken.Generate(Secret, ItemId, expires);
        Assert.True(StreamToken.Validate(Secret, token, ItemId, expires.ToUnixTimeSeconds(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public static void Validate_ExpiredToken_ReturnsFalse()
    {
        var expires = DateTimeOffset.UtcNow.AddHours(-1);
        var token = StreamToken.Generate(Secret, ItemId, expires);
        Assert.False(StreamToken.Validate(Secret, token, ItemId, expires.ToUnixTimeSeconds(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public static void Validate_WrongSecret_ReturnsFalse()
    {
        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var token = StreamToken.Generate(Secret, ItemId, expires);
        Assert.False(StreamToken.Validate("wrong-secret", token, ItemId, expires.ToUnixTimeSeconds(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public static void Validate_WrongItemId_ReturnsFalse()
    {
        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var token = StreamToken.Generate(Secret, ItemId, expires);
        Assert.False(StreamToken.Validate(Secret, token, "different-item", expires.ToUnixTimeSeconds(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public static void Validate_TamperedToken_ReturnsFalse()
    {
        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var token = StreamToken.Generate(Secret, ItemId, expires);
        var tampered = token[..^4] + "0000";
        Assert.False(StreamToken.Validate(Secret, tampered, ItemId, expires.ToUnixTimeSeconds(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public static void Validate_UppercaseToken_ReturnsTrue()
    {
        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var token = StreamToken.Generate(Secret, ItemId, expires).ToUpperInvariant();
        Assert.True(StreamToken.Validate(Secret, token, ItemId, expires.ToUnixTimeSeconds(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public static void Generate_ProducesLowercaseHex()
    {
        var expires = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var token = StreamToken.Generate(Secret, ItemId, expires);
        Assert.Equal(token, token.ToLowerInvariant());
        Assert.Equal(64, token.Length); // SHA-256 = 32 bytes = 64 hex chars
    }
}
