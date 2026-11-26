using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using Xunit;

namespace Jellyfin.Controller.Tests.LiveTv;

public class LiveTvChannelTests
{
    [Fact]
    public void IsSports_Set_StoresTheSportsTag()
    {
        // Channel queries filter IsSports on the "Sports" tag, so the flag has to live there.
        var channel = new LiveTvChannel { IsSports = true };

        Assert.True(channel.IsSports);
        Assert.Equal(["Sports"], channel.Tags);
    }

    [Fact]
    public void IsNews_Set_StoresTheNewsTag()
    {
        var channel = new LiveTvChannel { IsNews = true };

        Assert.True(channel.IsNews);
        Assert.Equal(["News"], channel.Tags);
    }

    [Fact]
    public void IsSportsAndIsNews_Cleared_RemoveOnlyTheirTags()
    {
        var channel = new LiveTvChannel { Tags = ["Kids", "Sports", "Custom", "News"] };

        channel.IsSports = false;
        channel.IsNews = false;

        Assert.False(channel.IsSports);
        Assert.False(channel.IsNews);
        Assert.Equal(["Kids", "Custom"], channel.Tags);
    }

    [Fact]
    public void IsSports_SetTwice_DoesNotDuplicateTheTag()
    {
        var channel = new LiveTvChannel();

        channel.IsSports = true;
        channel.IsSports = true;

        Assert.Equal(["Sports"], channel.Tags);
    }

    [Fact]
    public void IsSportsAndIsNews_ReadExistingTags_IgnoringCase()
    {
        // Channels refreshed before this change only had the tags a user or tuner gave them.
        var channel = new LiveTvChannel { Tags = ["sports", "NEWS"] };

        Assert.True(channel.IsSports);
        Assert.True(channel.IsNews);
    }

    [Fact]
    public void RemoveTag_MatchesIgnoringCaseAndKeepsOtherTags()
    {
        var item = new LiveTvChannel { Tags = ["Kids", "Custom"] };

        item.RemoveTag("kids");

        Assert.Equal(["Custom"], item.Tags);
    }

    [Fact]
    public void RemoveTag_MissingTag_LeavesTagsUnchanged()
    {
        string[] tags = ["Custom"];
        var item = new LiveTvChannel { Tags = tags };

        item.RemoveTag("Kids");

        Assert.Same(tags, item.Tags);
    }
}
