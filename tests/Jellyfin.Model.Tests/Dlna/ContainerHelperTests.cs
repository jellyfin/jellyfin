using System;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Extensions;
using Xunit;

namespace Jellyfin.Model.Tests.Dlna;

public class ContainerHelperTests
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

    [Theory]
    [InlineData("mp3,mpeg", "mp3")]
    [InlineData("mp3,mpeg,avi", "mp3,avi")]
    [InlineData("-mp3,mpeg", "avi")]
    [InlineData("-mp3,mpeg,avi", "mp4,jpg")]
    public void ContainsContainer_InList_ReturnsTrue(string container, string? extension)
    {
        Assert.True(ContainerHelper.ContainsContainer(container, extension));
    }

    [Theory]
    [InlineData("mp3,mpeg", "avi")]
    [InlineData("mp3,mpeg,avi", "mp4,jpg")]
    [InlineData("mp3,mpeg", null)]
    [InlineData("mp3,mpeg", "")]
    [InlineData("-mp3,mpeg", "mp3")]
    [InlineData("-mp3,mpeg,avi", "mpeg,avi")]
    [InlineData(",mp3,", ",avi,")] // Empty values should be discarded
    [InlineData("-,mp3,", ",mp3,")] // Empty values should be discarded
    public void ContainsContainer_NotInList_ReturnsFalse(string container, string? extension)
    {
        Assert.False(ContainerHelper.ContainsContainer(container, extension));

        if (extension is not null)
        {
            Assert.False(ContainerHelper.ContainsContainer(container, extension.AsSpan()));
        }
    }

    [Theory]
    [InlineData("mp3,mpeg", "mp3")]
    [InlineData("mp3,mpeg,avi", "mp3,avi")]
    [InlineData("-mp3,mpeg", "avi")]
    [InlineData("-mp3,mpeg,avi", "mp4,jpg")]
    public void ContainsContainer_InList_ReturnsTrue_SpanVersion(string container, string? extension)
    {
        Assert.True(ContainerHelper.ContainsContainer(container, extension.AsSpan()));
    }

    [Theory]
    [InlineData(new string[] { "mp3", "mpeg" }, false, "mpeg")]
    [InlineData(new string[] { "mp3", "mpeg", "avi" }, false, "avi")]
    [InlineData(new string[] { "mp3", "", "avi" }, false, "mp3")]
    [InlineData(new string[] { "mp3", "mpeg" }, true, "avi")]
    [InlineData(new string[] { "mp3", "mpeg", "avi" }, true, "mkv")]
    [InlineData(new string[] { "mp3", "", "avi" }, true, "")]
    public void ContainsContainer_ThreeArgs_InList_ReturnsTrue(string[] containers, bool isNegativeList, string inputContainer)
    {
        Assert.True(ContainerHelper.ContainsContainer(containers, isNegativeList, inputContainer));
    }

    [Theory]
    [InlineData(new string[] { "mp3", "mpeg" }, false, "avi")]
    [InlineData(new string[] { "mp3", "mpeg", "avi" }, false, "mkv")]
    [InlineData(new string[] { "mp3", "", "avi" }, false, "")]
    [InlineData(new string[] { "mp3", "mpeg" }, true, "mpeg")]
    [InlineData(new string[] { "mp3", "mpeg", "avi" }, true, "mp3")]
    [InlineData(new string[] { "mp3", "", "avi" }, true, "avi")]
    public void ContainsContainer_ThreeArgs_InList_ReturnsFalse(string[] containers, bool isNegativeList, string inputContainer)
    {
        Assert.False(ContainerHelper.ContainsContainer(containers, isNegativeList, inputContainer));
    }
}
