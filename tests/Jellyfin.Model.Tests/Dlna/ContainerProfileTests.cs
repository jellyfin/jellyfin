using System;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Extensions;
using Xunit;

namespace Jellyfin.Model.Tests.Dlna;

public class ContainerProfileTests
{
    private readonly ContainerProfile _emptyContainerProfile = new ContainerProfile();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("mp4")]
    public void ContainsContainer_EmptyContainerProfile_ReturnsTrue(string? containers)
    {
        Assert.True(_emptyContainerProfile.ContainsContainer(containers));
    }

    [InlineData("mp3,mpeg", "mp3")]
    [InlineData("mp3,mpeg", "")]
    [InlineData("mp3,mpeg,avi", "mp3,avi")]
    [InlineData("-mp3,mpeg", "avi")]
    [InlineData("-mp3,mpeg,avi", "mp4,jpg")]
    [Theory]
    public void ContainsContainer_InList_ReturnsTrue(string container, string extension)
    {
        Assert.True(container.ContainsContainer(extension));
    }

    [InlineData("mp3,mpeg", "avi")]
    [InlineData("mp3,mpeg", null)]
    [InlineData("mp3,mpeg,avi", "mp4,jpg")]
    [InlineData("-mp3,mpeg", "mp3")]
    [InlineData("-mp3,mpeg,avi", "mpeg,avi")]
    [Theory]
    public void ContainsContainer_NotInList_ReturnsFalse(string container, string? extension)
    {
        Assert.False(container.ContainsContainer(extension));
    }

    [InlineData("mp3,mpeg", "mp3")]
    [InlineData("mp3,mpeg", "")]
    [InlineData("mp3,mpeg,avi", "mp3,avi")]
    [InlineData("-mp3,mpeg", "avi")]
    [InlineData("-mp3,mpeg,avi", "mp4,jpg")]
    [Theory]
    public void ContainsContainer_InList_ReturnsTrue_SpanVersion(string container, string extension)
    {
        Assert.True(container.ContainsContainer(extension.AsSpan()));
    }
}
