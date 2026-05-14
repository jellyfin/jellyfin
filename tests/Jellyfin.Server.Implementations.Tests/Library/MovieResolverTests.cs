using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.Movies;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class MovieResolverTests
{
    private static readonly NamingOptions _namingOptions = new();

    [Fact]
    public void Resolve_GivenLocalAlternateVersion_ResolvesToVideo()
    {
        var movieResolver = new MovieResolver(Mock.Of<IImageProcessor>(), Mock.Of<ILogger<MovieResolver>>(), _namingOptions, Mock.Of<IDirectoryService>());
        var itemResolveArgs = new ItemResolveArgs(
            Mock.Of<IServerApplicationPaths>(),
            null)
        {
            Parent = null,
            FileInfo = new FileSystemMetadata
            {
                FullName = "/movies/Black Panther (2018)/Black Panther (2018) - 1080p 3D.mk3d"
            }
        };

        Assert.NotNull(movieResolver.Resolve(itemResolveArgs));
    }

    [Fact]
    public void Resolve_MetadataOnlyFolder_ResolvesToMovie()
    {
        var movieResolver = new MovieResolver(Mock.Of<IImageProcessor>(), Mock.Of<ILogger<MovieResolver>>(), _namingOptions, Mock.Of<IDirectoryService>());
        var itemResolveArgs = new ItemResolveArgs(
            Mock.Of<IServerApplicationPaths>(),
            null)
        {
            Parent = new MediaBrowser.Controller.Entities.Folder(),
            FileInfo = new FileSystemMetadata
            {
                FullName = "/movies/Upcoming Movie",
                IsDirectory = true
            },
            FileSystemChildren = [
                new FileSystemMetadata { Name = "movie.nfo", FullName = "/movies/Upcoming Movie/movie.nfo" },
                new FileSystemMetadata { Name = "poster.jpg", FullName = "/movies/Upcoming Movie/poster.jpg" }
            ]
        };

        var result = movieResolver.Resolve(itemResolveArgs);
        Assert.NotNull(result);
        Assert.True(result.IsVirtualItem);
        Assert.True(result.IsPlaceHolder);
    }
}
