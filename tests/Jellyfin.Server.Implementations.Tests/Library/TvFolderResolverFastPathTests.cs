using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.TV;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class TvFolderResolverFastPathTests
{
    private static readonly NamingOptions _namingOptions = new();

    [Fact]
    public void SeriesResolver_TvShowsFolder_ResolvesWithoutEnumeratingChildren()
    {
        var resolver = new SeriesResolver(Mock.Of<ILogger<SeriesResolver>>(), _namingOptions);
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(mgr => mgr.GetConfiguredContentType("/library/Shows/Example Show"))
            .Returns((CollectionType?)null);

        var itemResolveArgs = new ItemResolveArgs(
            Mock.Of<IServerApplicationPaths>(),
            libraryManager.Object)
        {
            Parent = new Folder { Path = "/library/Shows", IsRoot = false },
            CollectionType = CollectionType.tvshows,
            FileInfo = new FileSystemMetadata
            {
                FullName = "/library/Shows/Example Show",
                IsDirectory = true
            },
            FileSystemChildren = []
        };

        var item = resolver.Resolve(itemResolveArgs);

        var series = Assert.IsType<Series>(item);
        Assert.Equal("/library/Shows/Example Show", series.Path);
        Assert.Equal("Example Show", series.Name);
        libraryManager.Verify(mgr => mgr.GetConfiguredContentType("/library/Shows/Example Show"), Times.Once);
    }

    [Fact]
    public void SeasonResolver_SeasonFolder_ResolvesWithoutEnumeratingChildren()
    {
        var localization = new Mock<ILocalizationManager>();
        localization
            .Setup(loc => loc.GetLocalizedString("NameSeasonNumber"))
            .Returns("Season {0}");

        var resolver = new SeasonResolver(_namingOptions, localization.Object, Mock.Of<ILogger<SeasonResolver>>());
        var itemResolveArgs = new ItemResolveArgs(
            Mock.Of<IServerApplicationPaths>(),
            Mock.Of<ILibraryManager>())
        {
            Parent = new Series
            {
                Path = "/library/Shows/Example Show",
                Name = "Example Show"
            },
            FileInfo = new FileSystemMetadata
            {
                FullName = "/library/Shows/Example Show/Season 02",
                IsDirectory = true
            },
            FileSystemChildren = [],
            LibraryOptions = new MediaBrowser.Model.Configuration.LibraryOptions
            {
                PreferredMetadataLanguage = "en",
                SeasonZeroDisplayName = "Specials"
            }
        };

        var item = resolver.Resolve(itemResolveArgs);

        var season = Assert.IsType<Season>(item);
        Assert.Equal(2, season.IndexNumber);
        Assert.Equal("Season 2", season.Name);
        Assert.Equal("/library/Shows/Example Show/Season 02", season.Path);
    }
}
