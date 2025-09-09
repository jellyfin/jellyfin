using System;
using System.Globalization;
using Xunit;

namespace Jellyfin.Extensions.Tests;

public class GuidExtensionsTests
{
    [Fact]
    public void ToUpper_DefaultFormat_ReturnsUppercaseString()
    {
        var guid = new Guid("a852a27afe324084ae66db579ee3ee18");

        var result = guid.ToUpper(formatProvider: CultureInfo.InvariantCulture);

        Assert.Equal("A852A27AFE324084AE66DB579EE3EE18", result);
    }

    [Theory]
    [InlineData("N", "A852A27AFE324084AE66DB579EE3EE18")] // No dashes
    [InlineData("D", "A852A27A-FE32-4084-AE66-DB579EE3EE18")] // Dashes
    [InlineData("B", "{A852A27A-FE32-4084-AE66-DB579EE3EE18}")] // Braces and dashes
    [InlineData("P", "(A852A27A-FE32-4084-AE66-DB579EE3EE18)")] // Parentheses and dashes
    [InlineData("X", "{0XA852A27A,0XFE32,0X4084,{0XAE,0X66,0XDB,0X57,0X9E,0XE3,0XEE,0X18}}")] // Hexadecimal format
    public void ToUpper_DifferentFormats_ReturnsUppercaseString(string format, string expected)
    {
        var guid = new Guid("a852a27afe324084ae66db579ee3ee18");

        var result = guid.ToUpper(format, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToUpper_WithNullFormatProvider_ReturnsUppercaseString()
    {
        var guid = new Guid("a852a27afe324084ae66db579ee3ee18");

        var result = guid.ToUpper("N", null);

        Assert.Equal("A852A27AFE324084AE66DB579EE3EE18", result);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")] // All zeros
    [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")] // All F's
    [InlineData("12345678-1234-1234-1234-123456789ABC")] // Mixed values
    [InlineData("ABCDEF01-2345-6789-ABCD-EF0123456789")] // Mixed case input
    public void ToUpper_VariousGuidValues_ReturnsUppercaseString(string guidString)
    {
        var guid = new Guid(guidString);

        var result = guid.ToUpper(formatProvider: CultureInfo.InvariantCulture);

        Assert.Equal(guidString.ToUpperInvariant().Replace("-", string.Empty, StringComparison.Ordinal), result);
    }

    [Fact]
    public void ToUpper_ConsistencyWithToString_MatchesExpectedBehavior()
    {
        var guid = new Guid("a852a27afe324084ae66db579ee3ee18");

        var toUpperResult = guid.ToUpper("N", CultureInfo.InvariantCulture);
        var toStringResult = guid.ToString("N").ToUpperInvariant();

        Assert.Equal(toStringResult, toUpperResult);
    }

    [Theory]
    [InlineData("N")]
    [InlineData("D")]
    [InlineData("B")]
    [InlineData("P")]
    [InlineData("X")]
    public void ToUpper_AllFormats_ConsistencyWithToString(string format)
    {
        var guid = new Guid("a852a27afe324084ae66db579ee3ee18");

        var toUpperResult = guid.ToUpper(format, CultureInfo.InvariantCulture);
        var toStringResult = guid.ToString(format).ToUpperInvariant();

        Assert.Equal(toStringResult, toUpperResult);
    }
}
