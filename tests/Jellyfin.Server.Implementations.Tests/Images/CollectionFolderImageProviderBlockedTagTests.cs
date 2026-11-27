using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Images;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Images;

/// <summary>
/// A library collage is one image shown to everyone who can see the library, so it leaves out whatever any of
/// them blocks, and is rebuilt once it pictures something blocked since. Only those users count: anyone else
/// never sees the collage, and what they block must not thin it out. A music library pictures artists, which
/// are blocked either by their own tags or by every item crediting them being blocked.
/// </summary>
[Collection("LibraryManagerTests")]
public sealed class CollectionFolderImageProviderBlockedTagTests : IDisposable
{
    private const string BlockedTag = "Adult";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "jellyfin-tests", Guid.NewGuid().ToString("N"));
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly List<User> _users = [];
    private readonly List<BaseItem> _artists = [];
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IXmlSerializer? _previousXmlSerializer;
    private readonly CollectionFolder _library;
    private readonly CollectionFolderImageProvider _provider;

    private InternalItemsQuery? _artistQuery;
    private InternalItemsQuery? _creditQuery;
    private int _unblockedCredits = 1;

    public CollectionFolderImageProviderBlockedTagTests()
    {
        // A library's visibility, inherited tags and metadata path go through these process-wide statics;
        // restored in Dispose. Without an options file on disk the library loads the default, enabled options.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousXmlSerializer = CollectionFolder.XmlSerializer;
        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.ConfigurationManager = Mock.Of<IServerConfigurationManager>(c => c.ApplicationPaths.InternalMetadataPath == Path.Combine(_root, "metadata"));
        CollectionFolder.XmlSerializer = Mock.Of<IXmlSerializer>();

        _libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        _libraryManager.Setup(l => l.GetItemById(It.IsAny<Guid>())).Returns<Guid>(id => _artists.Find(a => a.Id.Equals(id)));
        _libraryManager
            .Setup(l => l.GetAllArtists(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(q => _artistQuery = q)
            .Returns(() => new QueryResult<(BaseItem Item, ItemCounts ItemCounts)>(_artists.Select(a => (a, new ItemCounts())).ToList()));
        _libraryManager
            .Setup(l => l.GetCount(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(q => _creditQuery = q)
            .Returns(() => _unblockedCredits);

        _fileSystem.Setup(f => f.ContainsSubPath(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        // Saving registers the collage on the library, as the real provider manager does.
        var providerManager = new Mock<IProviderManager>();
        providerManager
            .Setup(p => p.SaveImage(It.IsAny<BaseItem>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ImageType>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .Callback<BaseItem, string, string, ImageType, int?, bool?, CancellationToken>((item, _, _, type, _, _, _) => item.SetImage(
                new ItemImageInfo { Path = Path.Combine(item.GetInternalMetadataPath(), "poster.png"), Type = type, DateModified = DateTime.UtcNow },
                0))
            .Returns(Task.CompletedTask);

        var userManager = new Mock<IUserManager>();
        userManager.Setup(u => u.GetUsers()).Returns(_users);

        _library = new CollectionFolder { Id = Guid.NewGuid(), Name = "Music", Path = Path.Combine(_root, "root", "Music"), CollectionType = CollectionType.music };
        _provider = new CollectionFolderImageProvider(
            _fileSystem.Object,
            providerManager.Object,
            Mock.Of<IApplicationPaths>(p => p.TempDirectory == Path.Combine(_root, "temp")),
            Mock.Of<IImageProcessor>(p => p.SupportsImageCollageCreation),
            _libraryManager.Object,
            userManager.Object);
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        CollectionFolder.XmlSerializer = _previousXmlSerializer!;

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Fact]
    public async Task FetchAsync_TagBlockedByAUserWhoCanSeeTheLibrary_IsLeftOut()
    {
        AddUser(hasLibraryAccess: true, BlockedTag);

        await FetchAsync();

        Assert.NotNull(_artistQuery);
        Assert.Equal([BlockedTag.GetCleanValue()], _artistQuery.ExcludeInheritedTags);
    }

    [Fact]
    public async Task FetchAsync_TagBlockedOnlyByAUserWithoutAccessToTheLibrary_IsKept()
    {
        AddUser(hasLibraryAccess: false, BlockedTag);
        AddUser(hasLibraryAccess: true);

        await FetchAsync();

        Assert.NotNull(_artistQuery);
        Assert.Empty(_artistQuery.ExcludeInheritedTags);
    }

    [Fact]
    public async Task FetchAsync_TagTheLibraryCarriesItself_IsKept()
    {
        // Whoever blocks it can't see the library at all, so leaving the tag out would only empty the collage.
        _library.Tags = [BlockedTag];
        AddUser(hasLibraryAccess: true, BlockedTag);
        AddUser(hasLibraryAccess: true);

        await FetchAsync();

        Assert.NotNull(_artistQuery);
        Assert.Empty(_artistQuery.ExcludeInheritedTags);
    }

    [Fact]
    public async Task HasChanged_CollageOfArtistsNothingBlocks_IsUnchanged()
    {
        AddUser(hasLibraryAccess: true, BlockedTag);
        AddArtist();
        await FetchAsync();

        Assert.False(HasChanged());
    }

    [Fact]
    public async Task HasChanged_PicturedArtistTaggedSinceTheCollageWasBuilt_HasChanged()
    {
        AddUser(hasLibraryAccess: true, BlockedTag);
        var artist = AddArtist();
        await FetchAsync();

        artist.Tags = [BlockedTag];

        Assert.True(HasChanged());
    }

    [Fact]
    public async Task HasChanged_PicturedArtistTaggedWithWhatOnlyAUserWhoCannotSeeTheLibraryBlocks_IsUnchanged()
    {
        AddUser(hasLibraryAccess: false, BlockedTag);
        AddUser(hasLibraryAccess: true, "Unrelated");
        var artist = AddArtist();
        await FetchAsync();

        artist.Tags = [BlockedTag];

        Assert.False(HasChanged());
    }

    [Fact]
    public async Task HasChanged_PicturedArtistNowCreditedOnlyByBlockedItems_HasChanged()
    {
        AddUser(hasLibraryAccess: true, BlockedTag);
        var artist = AddArtist();
        await FetchAsync();

        // The artist carries no tag, but every track crediting it has been tagged since.
        _unblockedCredits = 0;

        Assert.True(HasChanged());
        Assert.NotNull(_creditQuery);
        Assert.Equal([artist.Id], _creditQuery.ArtistIds);
        Assert.Equal(_library.Id, _creditQuery.ParentId);
        Assert.True(_creditQuery.Recursive);
        Assert.Equal([BlockedTag.GetCleanValue()], _creditQuery.ExcludeInheritedTags);
    }

    [Fact]
    public void HasChanged_CollageWithoutARecordWhileATagIsBlocked_HasChanged()
    {
        // A collage built before its pictured items were recorded may picture anything.
        AddUser(hasLibraryAccess: true, BlockedTag);
        _library.SetImage(new ItemImageInfo { Path = Path.Combine(_library.GetInternalMetadataPath(), "poster.png"), Type = ImageType.Primary, DateModified = DateTime.UtcNow }, 0);

        Assert.True(HasChanged());
    }

    [Fact]
    public async Task FetchAsync_NothingLeftThatIsNotBlocked_RemovesTheCollage()
    {
        AddUser(hasLibraryAccess: true, BlockedTag);
        AddArtist();
        await FetchAsync();
        var collage = _library.GetImageInfo(ImageType.Primary, 0);
        Assert.NotNull(collage);

        // Blocked since, so the listing leaves it out.
        _artists.Clear();

        Assert.True((await FetchAsync()).HasFlag(ItemUpdateType.ImageUpdate));
        Assert.Null(_library.GetImageInfo(ImageType.Primary, 0));
        _fileSystem.Verify(f => f.DeleteFile(collage.Path), Times.Once);
    }

    [Fact]
    public async Task FetchAsync_RecordCannotBeWritten_StillReportsTheSavedCollage()
    {
        AddUser(hasLibraryAccess: true, BlockedTag);
        AddArtist();

        // A file where the library's metadata directory belongs makes writing the record fail.
        var metadataPath = _library.GetInternalMetadataPath();
        Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
        await File.WriteAllTextAsync(metadataPath, string.Empty, TestContext.Current.CancellationToken);

        Assert.True((await FetchAsync()).HasFlag(ItemUpdateType.ImageUpdate));

        // With no record to go by, the collage is rebuilt at the next check.
        Assert.True(HasChanged());
    }

    private void AddUser(bool hasLibraryAccess, params string[] blockedTags)
    {
        var user = new User("user" + _users.Count, "auth", "reset");
        user.SetPreference(PreferenceKind.BlockedTags, blockedTags);
        _users.Add(user);

        // Without this, or the library among the user's enabled folders, the library is hidden from them.
        user.SetPermission(PermissionKind.EnableAllFolders, hasLibraryAccess);
    }

    private MusicArtist AddArtist()
    {
        var artist = new MusicArtist { Id = Guid.NewGuid(), Name = "Artist" + _artists.Count };
        artist.SetImage(new ItemImageInfo { Path = Path.Combine(_root, "artists", artist.Name, "folder.jpg"), Type = ImageType.Primary }, 0);
        _artists.Add(artist);
        return artist;
    }

    private Task<ItemUpdateType> FetchAsync()
        => _provider.FetchAsync(_library, new MetadataRefreshOptions(Mock.Of<IDirectoryService>()), TestContext.Current.CancellationToken);

    private bool HasChanged() => _provider.HasChanged(_library, Mock.Of<IDirectoryService>());
}
