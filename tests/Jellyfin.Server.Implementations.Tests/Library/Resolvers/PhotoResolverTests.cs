using Emby.Server.Implementations.Library.Resolvers;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.Resolvers;

public class PhotoResolverTests
{
    [Theory]
    [InlineData("name.mp4", "name", true)]
    [InlineData("name.mp4", "NAME", true)]
    [InlineData("name.mkv", "name-poster", true)]
    [InlineData("name.mkv", "name-thumb", true)]
    [InlineData("name.mkv", "name-fanart", true)]
    [InlineData("name.mkv", "name-fanart1", true)]
    [InlineData("name.mkv", "name-fanart-1", true)]
    [InlineData("name.mkv", "name-fanart20", true)]
    [InlineData("name.mkv", "name-backdrop", true)]
    [InlineData("name.mkv", "name-backdrop-2", true)]
    [InlineData("name.mkv", "name-POSTER", true)]
    [InlineData("name.mkv", "name-landscape", true)]
    [InlineData("name.mkv", "name-cover", true)]
    [InlineData("name.mkv", "name-disc", true)]
    [InlineData("name.mkv", "name-logo", true)]
    [InlineData("name.mp4", "name001", false)]
    [InlineData("name.mp4", "name002", false)]
    [InlineData("name.mp4", "name_01", false)]
    [InlineData("name.mp4", "name-vacation", false)]
    [InlineData("name.mp4", "name-trip", false)]
    [InlineData("name.mp4", "other", false)]
    [InlineData("2024.mp4", "20240101_120000", false)]
    public void IsOwnedByResolvedMedia_Validations(string videoFile, string imageFilename, bool expected)
    {
        var result = PhotoResolver.IsOwnedByResolvedMedia(videoFile, imageFilename);
        Assert.Equal(expected, result);
    }
}
