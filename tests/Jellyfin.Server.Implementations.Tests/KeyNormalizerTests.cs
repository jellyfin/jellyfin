using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests;

public class KeyNormalizerTests
{
    private const int Bound = KeyNormalizer.UserDataKeyMaxLength;

    [Fact]
    public void Normalize_ValueAtBound_IsUnchanged()
    {
        var value = new string('a', Bound);

        Assert.Same(value, KeyNormalizer.NormalizeUserDataKey(value));
    }

    [Fact]
    public void Normalize_EmptyValue_IsUnchanged()
    {
        Assert.Equal(string.Empty, KeyNormalizer.NormalizeUserDataKey(string.Empty));
    }

    [Fact]
    public void Normalize_ValueOneOverBound_IsReplacedByPrefixAndHash()
    {
        var value = new string('a', Bound + 1);

        var result = KeyNormalizer.NormalizeUserDataKey(value);

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        Assert.Equal(Bound, result.Length);
        Assert.Equal(new string('a', Bound - 65) + "-" + hash, result);
    }

    [Fact]
    public void Normalize_ValueWithSurrogatePairAtTheCut_DoesNotSplitThePair()
    {
        // The prefix is 191 characters, so the cut falls between the two halves of the pair at 190 and 191.
        var value = new string('a', Bound - 66) + "\U0001F600" + new string('b', 100);

        var result = KeyNormalizer.NormalizeUserDataKey(value);

        Assert.Equal(Bound - 1, result.Length);
        Assert.False(char.IsHighSurrogate(result[Bound - 66]));
        Assert.StartsWith(new string('a', Bound - 66) + "-", result, StringComparison.Ordinal);
        Assert.DoesNotContain(result, c => char.IsSurrogate(c));
    }

    [Fact]
    public void Normalize_ValueWithSurrogatePairInsideThePrefix_KeepsThePair()
    {
        var value = new string('a', Bound - 67) + "\U0001F600" + new string('b', 100);

        var result = KeyNormalizer.NormalizeUserDataKey(value);

        Assert.Equal(Bound, result.Length);
        Assert.Contains("\U0001F600", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(5000)]
    public void Normalize_IsIdempotent(int extra)
    {
        var once = KeyNormalizer.NormalizeUserDataKey(new string('x', Bound + extra));
        var twice = KeyNormalizer.NormalizeUserDataKey(once);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Normalize_SurrogateShortenedResult_IsIdempotent()
    {
        var once = KeyNormalizer.NormalizeUserDataKey(new string('a', Bound - 66) + "\U0001F600" + new string('b', 100));

        Assert.Equal(once, KeyNormalizer.NormalizeUserDataKey(once));
    }

    [Fact]
    public void Normalize_IsDeterministic()
    {
        var value = new string('k', Bound + 10);

        Assert.Equal(KeyNormalizer.NormalizeUserDataKey(value), KeyNormalizer.NormalizeUserDataKey(new string('k', Bound + 10)));
    }

    [Fact]
    public void Normalize_LongValuesSharingTheirFirstCharacters_MapToDifferentKeys()
    {
        var common = new string('c', Bound);

        var first = KeyNormalizer.NormalizeUserDataKey(common + "one");
        var second = KeyNormalizer.NormalizeUserDataKey(common + "two");

        Assert.NotEqual(first, second);
        Assert.Equal(first[..(Bound - 65)], second[..(Bound - 65)]);
    }

    [Fact]
    public void Normalize_ValuesThatDifferOnlyInCase_MapToDifferentKeys()
    {
        var upper = KeyNormalizer.NormalizeUserDataKey(new string('A', Bound + 1));
        var lower = KeyNormalizer.NormalizeUserDataKey(new string('a', Bound + 1));

        Assert.NotEqual(upper, lower, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalizeDeviceId_UsesTheDeviceIdBound()
    {
        var atBound = new string('d', KeyNormalizer.DeviceIdMaxLength);
        var over = atBound + "d";

        Assert.Same(atBound, KeyNormalizer.NormalizeDeviceId(atBound));
        Assert.Equal(KeyNormalizer.DeviceIdMaxLength, KeyNormalizer.NormalizeDeviceId(over).Length);
    }

    [Fact]
    public void Normalize_BoundTooSmallForTheHash_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KeyNormalizer.Normalize("a", 64));
    }
}
