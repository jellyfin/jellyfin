using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

public class IsPathReferencedByOtherItemsTests
{
    private readonly Emby.Server.Implementations.Library.LibraryManager _libraryManager;
    private readonly Mock<IItemRepository> _itemRepositoryMock;

    public IsPathReferencedByOtherItemsTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configMock = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configMock.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        _itemRepositoryMock = fixture.Freeze<Mock<IItemRepository>>();
        _libraryManager = fixture.Build<Emby.Server.Implementations.Library.LibraryManager>().Do(s => s.AddParts(
                fixture.Create<IEnumerable<IResolverIgnoreRule>>(),
                fixture.Create<IEnumerable<IItemResolver>>(),
                fixture.Create<IEnumerable<IIntroProvider>>(),
                fixture.Create<IEnumerable<IBaseItemComparer>>(),
                fixture.Create<IEnumerable<ILibraryPostScanTask>>()))
            .Create();

        BaseItem.FileSystem ??= fixture.Create<IFileSystem>();
        BaseItem.MediaSourceManager ??= fixture.Create<IMediaSourceManager>();
    }

    [Fact]
    public void IsPathReferencedByOtherItems_OtherItemSharesPath_ReturnsTrue()
    {
        var deletingId = Guid.NewGuid();
        var otherItem = new Movie { Id = Guid.NewGuid(), Path = "/movies/Shared/movie.mkv" };
        _itemRepositoryMock.Setup(i => i.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem> { otherItem });

        var result = _libraryManager.IsPathReferencedByOtherItems("/movies/Shared/movie.mkv", new HashSet<Guid> { deletingId });

        Assert.True(result);
    }

    [Fact]
    public void IsPathReferencedByOtherItems_OnlyDeletingItemsSharePath_ReturnsFalse()
    {
        var deletingId = Guid.NewGuid();
        var deletingItem = new Movie { Id = deletingId, Path = "/movies/Shared/movie.mkv" };
        _itemRepositoryMock.Setup(i => i.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem> { deletingItem });

        var result = _libraryManager.IsPathReferencedByOtherItems("/movies/Shared/movie.mkv", new HashSet<Guid> { deletingId });

        Assert.False(result);
    }

    [Fact]
    public void IsPathReferencedByOtherItems_NoItemReferencesPath_ReturnsFalse()
    {
        _itemRepositoryMock.Setup(i => i.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem>());

        var result = _libraryManager.IsPathReferencedByOtherItems("/movies/Shared/movie.mkv", new HashSet<Guid> { Guid.NewGuid() });

        Assert.False(result);
    }

    [Fact]
    public void IsPathReferencedByOtherItems_EmptyPath_ReturnsFalse()
    {
        var result = _libraryManager.IsPathReferencedByOtherItems(string.Empty, new HashSet<Guid> { Guid.NewGuid() });

        Assert.False(result);
        _itemRepositoryMock.Verify(i => i.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Never);
    }

    [Fact]
    public void IsPathReferencedByOtherItems_QueriesByPathExcludingVirtualItems()
    {
        InternalItemsQuery? captured = null;
        _itemRepositoryMock.Setup(i => i.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(q => captured = q)
            .Returns(new List<BaseItem>());

        _libraryManager.IsPathReferencedByOtherItems("/movies/Shared/movie.mkv", new HashSet<Guid> { Guid.NewGuid() });

        Assert.NotNull(captured);
        Assert.Equal("/movies/Shared/movie.mkv", captured!.Path);
        // Virtual/missing items must never keep a real file alive.
        Assert.False(captured.IsVirtualItem);
    }
}
