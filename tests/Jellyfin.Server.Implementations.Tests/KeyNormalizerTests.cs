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

    [Theory]
    [InlineData("", 3, "")]
    [InlineData("abc", 3, "abc")]
    [InlineData("abc", 4, "abc")]
    [InlineData("abcd", 3, "abc")]
    [InlineData("abcd", 0, "")]
    public void Cut_Ascii_KeepsTheBeginning(string value, int maxLength, string expected)
    {
        Assert.Equal(expected, KeyNormalizer.Cut(value, maxLength));
    }

    [Fact]
    public void Cut_StringThatFits_IsReturnedUnchanged()
    {
        var value = new string('a', 10);

        Assert.Same(value, KeyNormalizer.Cut(value, 10));
    }

    [Fact]
    public void Cut_CutBetweenSurrogates_DropsTheWholePair()
    {
        Assert.Equal("ab", KeyNormalizer.Cut("ab\U0001F600cd", 3));
    }

    [Fact]
    public void Cut_CutAfterSurrogatePair_KeepsThePair()
    {
        Assert.Equal("ab\U0001F600", KeyNormalizer.Cut("ab\U0001F600cd", 4));
    }

    [Fact]
    public void Cut_PairAtTheStartWithRoomForOneCharacter_IsEmpty()
    {
        Assert.Equal(string.Empty, KeyNormalizer.Cut("\U0001F600", 1));
    }

    [Fact]
    public void Cut_LoneHighSurrogateAtTheCut_IsKept()
    {
        Assert.Equal("ab\uD83D", KeyNormalizer.Cut("ab\uD83Dcd", 3));
    }

    [Fact]
    public void Cut_ResultIsNeverLongerThanTheBound()
    {
        const string Value = "a\U0001F600b\U0001F600c\U0001F600d";

        for (var maxLength = 0; maxLength <= Value.Length; maxLength++)
        {
            var result = KeyNormalizer.Cut(Value, maxLength);

            Assert.True(result.Length <= maxLength);
            Assert.StartsWith(result, Value, StringComparison.Ordinal);
            Assert.False(result.Length > 0 && char.IsHighSurrogate(result[^1]));
        }
    }

    [Fact]
    public void CutPersonRole_RoleAtTheBound_IsUnchanged()
    {
        var role = new string('r', KeyNormalizer.PersonRoleMaxLength);

        Assert.Same(role, KeyNormalizer.CutPersonRole(role));
    }

    [Fact]
    public void CutPersonRole_RoleOverTheBound_IsCutAndTrimmed()
    {
        var role = new string('r', KeyNormalizer.PersonRoleMaxLength - 1) + " tail";

        Assert.Equal(new string('r', KeyNormalizer.PersonRoleMaxLength - 1), KeyNormalizer.CutPersonRole(role));
    }

    [Fact]
    public void CutItemValue_ValueOverTheBound_IsCutWithoutTrimming()
    {
        var value = new string('v', KeyNormalizer.ItemValueMaxLength - 1) + " tail";

        Assert.Equal(new string('v', KeyNormalizer.ItemValueMaxLength - 1) + " ", KeyNormalizer.CutItemValue(value));
    }
}
