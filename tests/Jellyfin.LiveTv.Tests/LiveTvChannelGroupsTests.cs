using System;
using Xunit;

namespace Jellyfin.LiveTv.Tests;

public class LiveTvChannelGroupsTests
{
    [Theory]
    [InlineData("Sports", "sports")]
    [InlineData("Sports", " Sports ")]
    [InlineData("Sports", "SPORTS")]
    [InlineData("Séries", "Series")]
    public void GetId_IsTheSameForEverySpellingOfOneGroup(string first, string second)
    {
        Assert.Equal(LiveTvChannelGroups.GetId(first), LiveTvChannelGroups.GetId(second));
    }

    [Fact]
    public void GetId_IsStableAcrossProcessesAndDifferentForDifferentGroups()
    {
        // Pinned so a client may persist the id: changing the derivation is a breaking change.
        Assert.Equal(Guid.Parse("c7e7344a-e172-365d-d97b-2abb2540ce18"), LiveTvChannelGroups.GetId("Sports"));
        Assert.NotEqual(LiveTvChannelGroups.GetId("Sports"), LiveTvChannelGroups.GetId("News"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetId_RejectsBlankNames(string name)
    {
        Assert.Throws<ArgumentException>(() => LiveTvChannelGroups.GetId(name));
    }
}
