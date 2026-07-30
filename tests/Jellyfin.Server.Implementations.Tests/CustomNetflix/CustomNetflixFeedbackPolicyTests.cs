using System;
using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixFeedbackPolicyTests
{
    [Theory]
    [InlineData(" LIKE ", CustomNetflixFeedbackPolicy.Like)]
    [InlineData("dislike", CustomNetflixFeedbackPolicy.Dislike)]
    [InlineData("not_interested", CustomNetflixFeedbackPolicy.NotInterested)]
    [InlineData("not-interested", CustomNetflixFeedbackPolicy.NotInterested)]
    public void Normalize_ReturnsStableApiValue(string value, string expected)
        => Assert.Equal(expected, CustomNetflixFeedbackPolicy.Normalize(value));

    [Theory]
    [InlineData("")]
    [InlineData("favorite")]
    public void Normalize_RejectsUnknownValues(string value)
        => Assert.Throws<ArgumentException>(() => CustomNetflixFeedbackPolicy.Normalize(value));
}
