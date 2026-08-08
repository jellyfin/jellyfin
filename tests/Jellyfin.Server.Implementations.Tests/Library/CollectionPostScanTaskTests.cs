using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library.Validators;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class CollectionPostScanTaskTests
{
    private readonly Mock<ILibraryManager> _libraryManager = new(MockBehavior.Loose);
    private readonly Mock<ICollectionManager> _collectionManager = new(MockBehavior.Loose);
    private readonly CollectionPostScanTask _task;

    public CollectionPostScanTaskTests()
    {
        _task = new CollectionPostScanTask(
            _libraryManager.Object,
            _collectionManager.Object,
            NullLogger<CollectionPostScanTask>.Instance);
    }

    [Fact]
    public async Task Run_RenamedLockedCollection_DoesNotRecreateUnderDefaultName()
    {
        const string DefaultName = "The Original Collection";
        const string RenamedName = "My Renamed Collection";
        const string CollectionId = "10";

        var movie1 = CreateMovie("Film A", DefaultName, CollectionId);
        var movie2 = CreateMovie("Film B", DefaultName, CollectionId);

        var existingBoxSet = new BoxSet { Name = RenamedName, IsLocked = true };
        existingBoxSet.SetProviderId(MetadataProvider.Tmdb, CollectionId);

        var library = new Folder { Name = "Movies" };
        var rootFolder = new AggregateFolder { Children = new List<BaseItem> { library } };

        _libraryManager.Setup(l => l.RootFolder).Returns(rootFolder);
        _libraryManager.Setup(l => l.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(new LibraryOptions { AutomaticallyAddToCollection = true });

        _libraryManager
            .Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.Movie))))
            .Returns(new List<BaseItem> { movie1, movie2 });
        _libraryManager
            .Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.BoxSet))))
            .Returns(new List<BaseItem> { existingBoxSet });

        _collectionManager
            .Setup(c => c.CreateCollectionAsync(It.IsAny<CollectionCreationOptions>()))
            .ReturnsAsync(new BoxSet { Name = DefaultName });
        _collectionManager
            .Setup(c => c.AddToCollectionAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>()))
            .Returns(Task.CompletedTask);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _collectionManager.Verify(
            c => c.CreateCollectionAsync(It.IsAny<CollectionCreationOptions>()),
            Times.Never,
            "renamed+locked collection must be matched by stable id, not recreated under its old name");

        // Members are re-added to the existing box set.
        _collectionManager.Verify(
            c => c.AddToCollectionAsync(existingBoxSet.Id, It.IsAny<IEnumerable<Guid>>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_NewCollection_StampsTmdbCollectionIdOnCreate()
    {
        const string CollectionName = "Brand New Collection";
        const string CollectionId = "42";

        var movie1 = CreateMovie("Film A", CollectionName, CollectionId);
        var movie2 = CreateMovie("Film B", CollectionName, CollectionId);

        var library = new Folder { Name = "Movies" };
        var rootFolder = new AggregateFolder { Children = new List<BaseItem> { library } };

        _libraryManager.Setup(l => l.RootFolder).Returns(rootFolder);
        _libraryManager.Setup(l => l.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(new LibraryOptions { AutomaticallyAddToCollection = true });

        _libraryManager
            .Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.Movie))))
            .Returns(new List<BaseItem> { movie1, movie2 });

        // No existing box sets, so the create path runs.
        _libraryManager
            .Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Contains(BaseItemKind.BoxSet))))
            .Returns(new List<BaseItem>());

        // Capture the options the task hands to CreateCollectionAsync so we can inspect them.
        CollectionCreationOptions? captured = null;
        _collectionManager
            .Setup(c => c.CreateCollectionAsync(It.IsAny<CollectionCreationOptions>()))
            .Callback<CollectionCreationOptions>(o => captured = o)
            .ReturnsAsync(new BoxSet { Name = CollectionName });
        _collectionManager
            .Setup(c => c.AddToCollectionAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Guid>>()))
            .Returns(Task.CompletedTask);

        await _task.Run(new Progress<double>(), CancellationToken.None);

        _collectionManager.Verify(
            c => c.CreateCollectionAsync(It.IsAny<CollectionCreationOptions>()),
            Times.Once);

        Assert.NotNull(captured);
        Assert.Equal(CollectionName, captured!.Name);
        Assert.Equal(CollectionId, captured.GetProviderId(MetadataProvider.Tmdb));
    }

    private static Movie CreateMovie(string name, string collectionName, string tmdbCollectionId)
    {
        var movie = new Movie { Name = name, Id = Guid.NewGuid(), CollectionName = collectionName };
        movie.SetProviderId(MetadataProvider.TmdbCollection, tmdbCollectionId);
        return movie;
    }
}
