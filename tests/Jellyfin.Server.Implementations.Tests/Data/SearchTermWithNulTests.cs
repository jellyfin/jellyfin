using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Library.Search;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Activity;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// A NUL character in the text a caller searches for is ignored, so the search finds what it finds without
/// the NUL. Some engines reject a NUL in a query parameter. SQLite stops reading a LIKE pattern at a NUL,
/// so "%Quas\0ar%" behaved as "%Quas", which only finds values that end in "quas", "%Quasar\0%" behaved as
/// "%Quasar", which only finds values that end in "quasar", and "%\0Quasar%" behaved as "%", which finds
/// every row. The second item of each pair is a decoy that only the last of these would also find.
/// </summary>
public sealed class SearchTermWithNulTests : SqliteDbTestFixture
{
    private readonly ItemTypeLookup _itemTypeLookup = new();
    private readonly BaseItemRepository _repository;
    private readonly SqlSearchProvider _searchProvider;
    private readonly ActivityManager _activityManager;

    public SearchTermWithNulTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);

        var quasar = new User("Quasar", "auth", "reset");
        var quasi = new User("Quasi", "auth", "reset");

        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(CreateMovie("Item One", "Quasar Wave"));
            context.BaseItems.Add(CreateMovie("Item Two", "Quasi Modo"));
            context.Users.Add(quasar);
            context.Users.Add(quasi);
            context.ActivityLogs.Add(new ActivityLog("Quasar Login", "QuasarType", quasar.Id)
            {
                Overview = "Quasar overview",
                ShortOverview = "Quasar short"
            });
            context.ActivityLogs.Add(new ActivityLog("Quasi Logout", "QuasiType", quasi.Id)
            {
                Overview = "Quasi overview",
                ShortOverview = "Quasi short"
            });
            context.SaveChanges();
        }

        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(scm => scm.Configuration).Returns(new ServerConfiguration());

        _searchProvider = new SqlSearchProvider(
            CreateDbContextFactory(),
            _itemTypeLookup,
            new Mock<ILibraryManager>().Object,
            new Mock<IUserManager>().Object,
            _repository,
            serverConfigurationManager.Object);
        _activityManager = new ActivityManager(CreateDbContextFactory());
    }

    [Theory]
    [InlineData("Quas\0ar", "Quasar")]
    [InlineData("Quasar\0", "Quasar")]
    [InlineData("\0Quasar", "Quasar")]
    public async Task Search_TermWithNul_MatchesAsWithoutNul(string term, string termWithoutNul)
    {
        Assert.Equal(["Item One"], SearchByTerm(termWithoutNul));
        Assert.Equal(["Item One"], SearchByTerm(term));

        Assert.Equal(["Item One"], SearchByNameContains(termWithoutNul));
        Assert.Equal(["Item One"], SearchByNameContains(term));

        Assert.Equal(["Item One"], await SearchWithProviderAsync(termWithoutNul).ConfigureAwait(true));
        Assert.Equal(["Item One"], await SearchWithProviderAsync(term).ConfigureAwait(true));

        foreach (var filter in new[]
        {
            nameof(ActivityLogQuery.Name),
            nameof(ActivityLogQuery.Overview),
            nameof(ActivityLogQuery.ShortOverview),
            nameof(ActivityLogQuery.Type),
            nameof(ActivityLogQuery.Username)
        })
        {
            Assert.Equal(["Quasar Login"], await GetActivityNamesAsync(filter, termWithoutNul).ConfigureAwait(true));
            Assert.Equal(["Quasar Login"], await GetActivityNamesAsync(filter, term).ConfigureAwait(true));
        }
    }

    private string[] SearchByTerm(string term)
        => GetNames(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            SearchTerm = term
        });

    private string[] SearchByNameContains(string term)
        => GetNames(new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            NameContains = term
        });

    private string[] GetNames(InternalItemsQuery query)
        => _repository.GetItemList(query).Select(i => i.Name).Order(StringComparer.Ordinal).ToArray();

    private async Task<List<string>> SearchWithProviderAsync(string term)
    {
        var results = await _searchProvider.SearchAsync(
            new SearchProviderQuery { SearchTerm = term, Limit = 10 },
            CancellationToken.None).ConfigureAwait(false);

        var ids = results.Select(r => r.ItemId).ToList();
        using var context = CreateDbContext();
        return context.BaseItems
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Name ?? string.Empty)
            .AsEnumerable()
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<string[]> GetActivityNamesAsync(string filter, string term)
    {
        var query = new ActivityLogQuery();
        switch (filter)
        {
            case nameof(ActivityLogQuery.Name):
                query.Name = term;
                break;
            case nameof(ActivityLogQuery.Overview):
                query.Overview = term;
                break;
            case nameof(ActivityLogQuery.ShortOverview):
                query.ShortOverview = term;
                break;
            case nameof(ActivityLogQuery.Type):
                query.Type = term;
                break;
            case nameof(ActivityLogQuery.Username):
                query.Username = term;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(filter), filter, null);
        }

        var result = await _activityManager.GetPagedResultAsync(query).ConfigureAwait(false);

        return result.Items.Select(e => e.Name).Order(StringComparer.Ordinal).ToArray();
    }

    private BaseItemEntity CreateMovie(string name, string originalTitle)
    {
        var id = Guid.NewGuid();
        return new BaseItemEntity
        {
            Id = id,
            PresentationUniqueKey = id.ToString("N"),
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
            Name = name,
            CleanName = name.GetCleanValue(),
            SortName = name.ToLowerInvariant(),
            OriginalTitle = originalTitle,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
