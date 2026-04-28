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
        Assert.Equal(
            StreamToken.Generate(Secret, ItemId, expires),
            StreamToken.Generate(Secret, ItemId, expires));
    }

    [Fact]
    public static void Generate_DifferentItem_ReturnsDifferentToken()
    {
        var expires = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.NotEqual(
            StreamToken.Generate(Secret, ItemId, expires),
            StreamToken.Generate(Secret, "other-item", expires));
    }

    [Fact]
    public static void Generate_DifferentExpiry_ReturnsDifferentToken()
    {
        Assert.NotEqual(
            StreamToken.Generate(Secret, ItemId, new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            StreamToken.Generate(Secret, ItemId, new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public static void Generate_DifferentSecret_ReturnsDifferentToken()
    {
        var expires = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.NotEqual(
            StreamToken.Generate(Secret, ItemId, expires),
            StreamToken.Generate("other-secret", ItemId, expires));
    }

    [Fact]
    public static void TryParse_ValidToken_ExtractsItemIdAndExpiry()
    {
        var expires = new DateTimeOffset(2030, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var token = StreamToken.Generate(Secret, ItemId, expires);

        Assert.True(StreamToken.TryParse(token, out var parsedItemId, out var parsedExpires));
        Assert.Equal(ItemId, parsedItemId);
        Assert.Equal(expires.ToUnixTimeSeconds(), parsedExpires);
    }

    [Fact]
    public static void TryParse_Garbage_ReturnsFalse()
    {
        Assert.False(StreamToken.TryParse("notavalidtoken", out _, out _));
    }

    [Fact]
    public static void TryValidate_ValidToken_ReturnsTrueWithItemId()
    {
        var expires = DateTimeOffset.UtcNow.AddHours(1);
        var token = StreamToken.Generate(Secret, ItemId, expires);

        Assert.True(StreamToken.TryValidate(Secret, token, DateTimeOffset.UtcNow, out var itemId));
        Assert.Equal(ItemId, itemId);
    }

    [Fact]
    public static void TryValidate_ExpiredToken_ReturnsFalse()
    {
        var expires = DateTimeOffset.UtcNow.AddHours(-1);
        var token = StreamToken.Generate(Secret, ItemId, expires);

        Assert.False(StreamToken.TryValidate(Secret, token, DateTimeOffset.UtcNow, out _));
    }

    [Fact]
    public static void TryValidate_WrongSecret_ReturnsFalse()
    {
        var token = StreamToken.Generate(Secret, ItemId, DateTimeOffset.UtcNow.AddHours(1));
        Assert.False(StreamToken.TryValidate("wrong-secret", token, DateTimeOffset.UtcNow, out _));
    }

    [Fact]
    public static void TryValidate_TamperedSignature_ReturnsFalse()
    {
        var token = StreamToken.Generate(Secret, ItemId, DateTimeOffset.UtcNow.AddHours(1));
        var dot = token.IndexOf('.', StringComparison.Ordinal);
        var tampered = token[..(dot + 1)] + "0000" + token[(dot + 5)..];

        Assert.False(StreamToken.TryValidate(Secret, tampered, DateTimeOffset.UtcNow, out _));
    }

    [Fact]
    public static void TryValidate_TamperedPayload_ReturnsFalse()
    {
        var token = StreamToken.Generate(Secret, ItemId, DateTimeOffset.UtcNow.AddHours(1));
        var dot = token.IndexOf('.', StringComparison.Ordinal);
        // corrupt a character in the payload
        var payloadChars = token[..dot].ToCharArray();
        payloadChars[2] = payloadChars[2] == 'A' ? 'B' : 'A';
        var tampered = new string(payloadChars) + token[dot..];

        Assert.False(StreamToken.TryValidate(Secret, tampered, DateTimeOffset.UtcNow, out _));
    }

    [Fact]
    public static void TryValidate_UppercaseSignature_ReturnsTrue()
    {
        var token = StreamToken.Generate(Secret, ItemId, DateTimeOffset.UtcNow.AddHours(1));
        var dot = token.IndexOf('.', StringComparison.Ordinal);
        var uppercased = token[..dot] + "." + token[(dot + 1)..].ToUpperInvariant();
        Assert.True(StreamToken.TryValidate(Secret, uppercased, DateTimeOffset.UtcNow, out var itemId));
        Assert.Equal(ItemId, itemId);
    }
}
