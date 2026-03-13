using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Savers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ScheduledTasks.Tasks;

public class CleanupCollectionAndPlaylistPathsTaskTests
{
    [Fact]
    public async Task ExecuteAsync_RemovesStaleLinkedChild_FromCollectionXml()
    {
        // Asserting Test
        var collectionDir = Directory.CreateTempSubdirectory();
        var mediaDir = Directory.CreateTempSubdirectory();
        var staleMediaPath = Path.Combine(mediaDir.FullName, "movie.mkv");
        await File.WriteAllTextAsync(staleMediaPath, "test").ConfigureAwait(true);

        var collection = new Mock<BoxSet> { CallBase = true };
        collection.Object.Name = "Collection Test";
        collection.Object.Path = collectionDir.FullName;
        collection.Object.LinkedChildren = [
            new LinkedChild
            {
                Path = staleMediaPath,
                Type = LinkedChildType.Manual
            }
        ];

        collection
            .Setup(x => x.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collectionsFolder = new Folder
        {
            Children = [collection.Object]
        };

        var userRootFolder = new Folder
        {
            Children = Array.Empty<BaseItem>()
        };

        var localizationMock = new Mock<ILocalizationManager>();
        var collectionManagerMock = new Mock<ICollectionManager>();
        var playlistManagerMock = new Mock<IPlaylistManager>();
        var loggerMock = new Mock<ILogger<CleanupCollectionAndPlaylistPathsTask>>();
        var providerManagerMock = new Mock<IProviderManager>();
        var libraryManagerMock = new Mock<ILibraryManager>();
        var fileSystemMock = new Mock<IFileSystem>();
        var configManagerMock = new Mock<IServerConfigurationManager>();

        configManagerMock.SetupGet(x => x.Configuration).Returns(new ServerConfiguration());

        playlistManagerMock
            .Setup(x => x.GetPlaylistsFolder())
            .Returns(new Folder { Children = Array.Empty<BaseItem>() });

        libraryManagerMock
            .Setup(x => x.GetUserRootFolder())
            .Returns(userRootFolder);

        libraryManagerMock
            .Setup(x => x.FindByPath(staleMediaPath, false))
            .Returns(new Movie { Path = staleMediaPath });

        libraryManagerMock
            .Setup(x => x.GetCollectionFolders(It.IsAny<BaseItem>(), It.IsAny<IEnumerable<Folder>>()))
            .Returns(new List<Folder>());

        libraryManagerMock
            .Setup(x => x.GetPeople(It.IsAny<BaseItem>()))
            .Returns(Array.Empty<PersonInfo>());

        collectionManagerMock
            .Setup(x => x.GetCollectionsFolder(false))
            .ReturnsAsync(collectionsFolder);

        var xmlSaver = new BoxSetXmlSaver(
            fileSystemMock.Object,
            configManagerMock.Object,
            libraryManagerMock.Object,
            NullLogger<BoxSetXmlSaver>.Instance);

        providerManagerMock
            .Setup(x => x.SaveMetadataAsync(It.IsAny<BaseItem>(), ItemUpdateType.MetadataEdit))
            .Returns<BaseItem, ItemUpdateType>((item, _) => xmlSaver.SaveAsync(item, CancellationToken.None));

        var task = new CleanupCollectionAndPlaylistPathsTask(
            localizationMock.Object,
            collectionManagerMock.Object,
            playlistManagerMock.Object,
            loggerMock.Object,
            providerManagerMock.Object,
            libraryManagerMock.Object,
            fileSystemMock.Object);

        // Execute
        await task.ExecuteAsync(new Progress<double>(), CancellationToken.None).ConfigureAwait(true);

        // Verify Results
        var collectionXmlPath = Path.Combine(collectionDir.FullName, "collection.xml");

        Assert.True(File.Exists(collectionXmlPath));

        var xml = await File.ReadAllTextAsync(collectionXmlPath).ConfigureAwait(true);
        Assert.DoesNotContain(staleMediaPath, xml, StringComparison.Ordinal);
        Assert.DoesNotContain("<CollectionItem>", xml, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(collection.Object.LinkedChildren);

        providerManagerMock.Verify(x => x.SaveMetadataAsync(collection.Object, ItemUpdateType.MetadataEdit), Times.Once);
        collection.Verify(x => x.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, It.IsAny<CancellationToken>()), Times.Once);
    }
}
