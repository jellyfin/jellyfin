using System;
using MediaBrowser.Controller.CustomNetflix;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixNativePlaystateSyncPolicyTests
{
    [Fact]
    public void ShouldSync_OnlySyncsDefaultProfile()
    {
        Assert.True(CustomNetflixNativePlaystateSyncPolicy.ShouldSync(CreateProfile(isDefault: true)));
        Assert.False(CustomNetflixNativePlaystateSyncPolicy.ShouldSync(CreateProfile(isDefault: false)));
        Assert.False(CustomNetflixNativePlaystateSyncPolicy.ShouldSync(null));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    [InlineData(1.5, 15000000)]
    public void SecondsToTicks_ClampsNegativePositions(double seconds, long expectedTicks)
    {
        var ticks = CustomNetflixNativePlaystateSyncPolicy.SecondsToTicks(seconds);

        Assert.Equal(expectedTicks, ticks);
    }

    [Fact]
    public void HashToken_IsStableWithoutRetainingTheToken()
    {
        var hash = CustomNetflixNativePlaystateSyncPolicy.HashToken("secret-token");

        Assert.Equal(hash, CustomNetflixNativePlaystateSyncPolicy.HashToken("secret-token"));
        Assert.NotEqual("secret-token", hash);
        Assert.Equal("no-token", CustomNetflixNativePlaystateSyncPolicy.HashToken(null));
    }

    private static CustomNetflixProfileDto CreateProfile(bool isDefault)
        => new()
        {
            Id = Guid.NewGuid(),
            JellyfinUserId = Guid.NewGuid(),
            Name = "Profile",
            IsDefault = isDefault
        };
}
