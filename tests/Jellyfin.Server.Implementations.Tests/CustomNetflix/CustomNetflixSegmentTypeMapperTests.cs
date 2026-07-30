using System;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixSegmentTypeMapperTests
{
    [Fact]
    public void NormalizeRequestedTypes_MapsIntroRecapAndCredits()
    {
        var types = CustomNetflixSegmentTypeMapper.NormalizeRequestedTypes(new[] { "intro, outro", "Recap", "credits", "unknown" });

        Assert.Equal(new[] { "credits", "intro", "recap" }, types);
    }

    [Fact]
    public void NormalizeRequestedTypes_RejectsUnknownOnly()
    {
        Assert.Throws<ArgumentException>(() => CustomNetflixSegmentTypeMapper.NormalizeRequestedTypes(new[] { "unknown" }));
    }

    [Theory]
    [InlineData("intro", MediaSegmentType.Intro)]
    [InlineData("recap", MediaSegmentType.Recap)]
    [InlineData("credits", MediaSegmentType.Outro)]
    [InlineData("unknown", MediaSegmentType.Unknown)]
    public void ToNativeSegmentType_MapsCustomTypesToJellyfinTypes(string segmentType, MediaSegmentType expected)
    {
        var result = CustomNetflixSegmentTypeMapper.ToNativeSegmentType(segmentType);

        Assert.Equal(expected, result);
    }
}
