using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Library.Search;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Library;

/// <summary>
/// Covers what <see cref="SqlSearchProvider"/> returns for a version group merged across two
/// libraries: the primary represents the group wherever it is visible, and the version stands in
/// for it for a user who cannot open the library the primary lives in.
/// </summary>
public sealed class SqlSearchProviderTests : SqliteDbTestFixture
{
    private static readonly Guid _movieLibraryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _movie4KLibraryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _primaryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid _versionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly SqlSearchProvider _provider;
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly User _user = new("test", "auth-provider", "reset-provider");

    public SqlSearchProviderTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        var movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie]!;
        var folderTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Folder]!;

        using (var context = CreateDbContext())
        {
            context.Users.Add(_user);
            context.BaseItems.Add(CreateLibrary(_movieLibraryId, folderTypeName, "Movies", "/movies"));
            context.BaseItems.Add(CreateLibrary(_movie4KLibraryId, folderTypeName, "Movies-4K", "/movies-4k"));
            context.BaseItems.Add(CreateMovie(_primaryId, movieTypeName, _movie4KLibraryId, null));
            context.BaseItems.Add(CreateMovie(_versionId, movieTypeName, _movieLibraryId, _primaryId));
            context.SaveChanges();
        }

        var userManager = new Mock<IUserManager>();
        userManager.Setup(u => u.GetUserById(_user.Id)).Returns(_user);

        _provider = new SqlSearchProvider(
            CreateDbContextFactory(),
            itemTypeLookup,
            _libraryManager.Object,
            userManager.Object,
            CreateBaseItemRepository(itemTypeLookup));
    }

    [Fact]
    public async Task SearchAsync_UserWithoutThePrimarysLibrary_FindsTheVersion()
    {
        RestrictUserTo(_movieLibraryId);

        var hits = await SearchAsync().ConfigureAwait(true);

        Assert.Equal([_versionId], hits);
    }

    [Fact]
    public async Task SearchAsync_UserWithBothLibraries_FindsThePrimaryOnce()
    {
        RestrictUserTo(_movieLibraryId, _movie4KLibraryId);

        var hits = await SearchAsync().ConfigureAwait(true);

        Assert.Equal([_primaryId], hits);
    }

    private void RestrictUserTo(params Guid[] libraryIds)
    {
        _libraryManager
            .Setup(l => l.ConfigureUserAccess(It.IsAny<InternalItemsQuery>(), It.IsAny<User>()))
            .Callback<InternalItemsQuery, User>((query, _) => query.TopParentIds = libraryIds);
    }

    private async Task<List<Guid>> SearchAsync()
    {
        var results = await _provider.SearchAsync(
            new SearchProviderQuery { SearchTerm = "coco", UserId = _user.Id, Limit = 10 },
            CancellationToken.None).ConfigureAwait(false);

        return results.Select(r => r.ItemId).ToList();
    }

    private static BaseItemEntity CreateLibrary(Guid id, string typeName, string name, string path)
        => new()
        {
            Id = id,
            Type = typeName,
            Name = name,
            Path = path,
            IsFolder = true
        };

    private static BaseItemEntity CreateMovie(Guid id, string typeName, Guid libraryId, Guid? primaryVersionId)
        => new()
        {
            Id = id,
            Type = typeName,
            Name = "Coco",
            CleanName = "coco",
            SortName = "Coco",
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false,
            ParentId = libraryId,
            TopParentId = libraryId,
            PresentationUniqueKey = (primaryVersionId ?? id).ToString("N"),
            PrimaryVersionId = primaryVersionId
        };
}
