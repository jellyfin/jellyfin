using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Verifies that SeriesDatePlayed uses the newest associated playback timestamp, including partial playback.
/// </summary>
public sealed class BaseItemRepositorySeriesDatePlayedTests : SqliteDbTestFixture
{
    private const string CollectionFolderType = "MediaBrowser.Controller.Entities.CollectionFolder";
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";

    private static readonly DateTime CompletedDate = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ControlDate = new(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PartialDate = new(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MovieOwnDate = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MovieAlternateDate = new(2026, 9, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OtherUserMovieDate = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PartialOnlyDate = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OtherUserDate = new(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

    private readonly User _userA = new("user-a", "auth-provider", "reset-provider");
    private readonly User _userB = new("user-b", "auth-provider", "reset-provider");
    private readonly Guid _seriesA = Guid.NewGuid();
    private readonly Guid _seriesB = Guid.NewGuid();
    private readonly Guid _seriesC = Guid.NewGuid();
    private readonly Guid _seriesD = Guid.NewGuid();
    private readonly Guid _library = Guid.NewGuid();
    private readonly Guid _movieOwn = Guid.NewGuid();
    private readonly Guid _movieAlternatePrimary = Guid.NewGuid();
    private readonly Guid _movieAlternate = Guid.NewGuid();
    private readonly BaseItemRepository _repository;

    public BaseItemRepositorySeriesDatePlayedTests()
    {
        using (var context = CreateDbContext())
        {
            Seed(context);
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Theory]
    [InlineData(false, true, SortOrder.Descending)]
    [InlineData(false, true, SortOrder.Ascending)]
    [InlineData(true, true, SortOrder.Descending)]
    [InlineData(true, true, SortOrder.Ascending)]
    [InlineData(false, false, SortOrder.Descending)]
    [InlineData(true, false, SortOrder.Descending)]
    public void SeriesDatePlayed_UsesLatestAssociatedDate(bool search, bool userScoped, SortOrder sortOrder)
    {
        var query = CreateQuery(search, userScoped, sortOrder);
        var ids = _repository.GetItemList(query).Select(item => item.Id).ToList();

        Assert.Equal(4, ids.Count);
        Assert.Contains(_seriesD, ids);

        Guid[] expectedDatedOrder = userScoped
            ? sortOrder == SortOrder.Descending
                ? [_seriesC, _seriesA, _seriesB]
                : [_seriesB, _seriesA, _seriesC]
            : [_seriesB, _seriesC, _seriesA];

        Assert.Equal(expectedDatedOrder, ids.Where(IsDatedSeries).ToArray());

        if (!search && userScoped && sortOrder == SortOrder.Descending)
        {
            var pageQuery = CreateQuery(search, userScoped, sortOrder);
            pageQuery.Limit = 1;

            var page = _repository.GetItems(pageQuery);

            Assert.Equal(4, page.TotalRecordCount);
            Assert.Equal(_seriesC, Assert.Single(page.Items).Id);
        }
    }

    [Theory]
    [InlineData(false, true, SortOrder.Descending)]
    [InlineData(false, true, SortOrder.Ascending)]
    [InlineData(true, true, SortOrder.Descending)]
    [InlineData(true, true, SortOrder.Ascending)]
    [InlineData(false, false, SortOrder.Descending)]
    [InlineData(true, false, SortOrder.Descending)]
    public void DatePlayed_InterleavesSeriesAndMovies(bool search, bool userScoped, SortOrder sortOrder)
    {
        var query = CreateMixedQuery(search, userScoped, sortOrder);
        var ids = _repository.GetItemList(query).Select(item => item.Id).ToList();

        Assert.Equal(6, ids.Count);
        Assert.Contains(_seriesD, ids);
        Assert.Contains(_movieAlternatePrimary, ids);
        Assert.DoesNotContain(_movieAlternate, ids);

        Guid[] expectedDatedOrder = userScoped
            ? sortOrder == SortOrder.Descending
                ? [_seriesC, _movieAlternatePrimary, _movieOwn, _seriesA, _seriesB]
                : [_seriesB, _seriesA, _movieOwn, _movieAlternatePrimary, _seriesC]
            : [_seriesB, _seriesC, _movieOwn, _movieAlternatePrimary, _seriesA];

        Assert.Equal(expectedDatedOrder, ids.Where(id => !id.Equals(_seriesD) && !id.Equals(_movieAlternate)).ToArray());

        if (!search && userScoped && sortOrder == SortOrder.Descending)
        {
            var pageQuery = CreateMixedQuery(search, userScoped, sortOrder);
            pageQuery.Limit = 1;

            var page = _repository.GetItems(pageQuery);

            Assert.Equal(6, page.TotalRecordCount);
            Assert.Equal(_seriesC, Assert.Single(page.Items).Id);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResumableMixedDatePlayed_IncludesPartialSeriesAndMovies(bool search)
    {
        var ids = _repository.GetItemList(CreateResumableMixedQuery(search));

        Assert.Equal([_seriesC, _movieAlternate, _movieOwn, _seriesA], ids.Select(item => item.Id));
    }

    private InternalItemsQuery CreateQuery(bool search, bool userScoped, SortOrder sortOrder)
        => new(userScoped ? _userA : null)
        {
            IncludeItemTypes = [BaseItemKind.Series],
            OrderBy = [(ItemSortBy.SeriesDatePlayed, sortOrder)],
            SearchTerm = search ? "probe" : null
        };

    private InternalItemsQuery CreateMixedQuery(bool search, bool userScoped, SortOrder sortOrder)
        => new(userScoped ? _userA : null)
        {
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            OrderBy = [(ItemSortBy.DatePlayed, sortOrder)],
            SearchTerm = search ? "probe" : null
        };

    private InternalItemsQuery CreateResumableMixedQuery(bool search)
        => new(_userA)
        {
            AncestorIds = [_library],
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            IsResumable = true,
            OrderBy = [(ItemSortBy.DatePlayed, SortOrder.Descending), (ItemSortBy.SortName, SortOrder.Descending)],
            Recursive = true,
            SearchTerm = search ? "probe" : null,
            TopParentIds = [_library]
        };

    private bool IsDatedSeries(Guid id)
        => !id.Equals(_seriesD);

    private void Seed(JellyfinDbContext context)
    {
        context.Users.AddRange(_userA, _userB);
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _library,
            Type = CollectionFolderType,
            Name = "Mixed library",
            CleanName = "mixed library",
            SortName = "mixed library",
            PresentationUniqueKey = _library.ToString("N"),
            IsFolder = true,
            IsVirtualItem = false
        });

        AddSeries(
            context,
            _seriesA,
            "probe alpha",
            (CompletedDate, true, 0L, _userA),
            (PartialDate, false, 900L, _userA));
        var seriesBEpisodes = AddSeries(
            context,
            _seriesB,
            "probe beta",
            (ControlDate, true, 0L, _userA));
        AddUserData(context, seriesBEpisodes[0], OtherUserDate, true, 0L, _userB);
        AddSeries(context, _seriesC, "probe gamma", (PartialOnlyDate, false, 900L, _userA));
        AddSeries(context, _seriesD, "probe delta", (null, false, 0L, _userA));

        AddMovie(
            context,
            _movieOwn,
            "probe movie own",
            null,
            (MovieOwnDate, false, 900L, _userA),
            (OtherUserMovieDate, false, 900L, _userB));
        AddMovie(context, _movieAlternatePrimary, "probe movie alternate", null);
        AddMovie(context, _movieAlternate, "probe movie alternate", _movieAlternatePrimary, (MovieAlternateDate, false, 900L, _userA));

        context.SaveChanges();
    }

    private Guid[] AddSeries(
        JellyfinDbContext context,
        Guid seriesId,
        string name,
        params (DateTime? LastPlayedDate, bool Played, long PlaybackPositionTicks, User User)[] episodes)
    {
        var seriesKey = seriesId.ToString("N");
        var cleanName = name.ToLowerInvariant();

        context.BaseItems.Add(new BaseItemEntity
        {
            Id = seriesId,
            Type = SeriesType,
            Name = name,
            CleanName = cleanName,
            SortName = cleanName,
            PresentationUniqueKey = seriesKey,
            IsFolder = true,
            TopParentId = _library,
            IsVirtualItem = false
        });
        AddAncestor(context, seriesId, _library);

        var episodeIds = new Guid[episodes.Length];
        for (var i = 0; i < episodes.Length; i++)
        {
            var episodeId = Guid.NewGuid();
            episodeIds[i] = episodeId;
            var episodeName = $"{name} episode {i}";

            context.BaseItems.Add(new BaseItemEntity
            {
                Id = episodeId,
                Type = EpisodeType,
                Name = episodeName,
                CleanName = episodeName,
                SortName = episodeName,
                MediaType = "Video",
                SeriesId = seriesId,
                SeriesName = name,
                SeriesPresentationUniqueKey = seriesKey,
                PresentationUniqueKey = episodeId.ToString("N"),
                IsFolder = false,
                TopParentId = _library,
                IsVirtualItem = false
            });
            AddAncestor(context, episodeId, seriesId);
            AddAncestor(context, episodeId, _library);

            var episode = episodes[i];
            AddUserData(context, episodeId, episode.LastPlayedDate, episode.Played, episode.PlaybackPositionTicks, episode.User);
        }

        return episodeIds;
    }

    private void AddMovie(
        JellyfinDbContext context,
        Guid movieId,
        string name,
        Guid? primaryVersionId,
        params (DateTime? LastPlayedDate, bool Played, long PlaybackPositionTicks, User User)[] userData)
    {
        var presentationKey = (primaryVersionId ?? movieId).ToString("N");
        var cleanName = name.ToLowerInvariant();

        context.BaseItems.Add(new BaseItemEntity
        {
            Id = movieId,
            Type = MovieType,
            Name = name,
            CleanName = cleanName,
            SortName = cleanName,
            MediaType = "Video",
            IsFolder = false,
            IsVirtualItem = false,
            PresentationUniqueKey = presentationKey,
            PrimaryVersionId = primaryVersionId,
            TopParentId = _library
        });
        AddAncestor(context, movieId, _library);

        foreach (var data in userData)
        {
            AddUserData(context, movieId, data.LastPlayedDate, data.Played, data.PlaybackPositionTicks, data.User);
        }
    }

    private static void AddUserData(
        JellyfinDbContext context,
        Guid itemId,
        DateTime? lastPlayedDate,
        bool played,
        long playbackPositionTicks,
        User user)
        => context.UserData.Add(new UserData
        {
            ItemId = itemId,
            UserId = user.Id,
            CustomDataKey = $"{itemId:N}-{user.Id:N}",
            LastPlayedDate = lastPlayedDate,
            Played = played,
            PlaybackPositionTicks = playbackPositionTicks,
            Item = null!,
            User = user
        });

    private static void AddAncestor(JellyfinDbContext context, Guid itemId, Guid ancestorId)
        => context.AncestorIds.Add(new AncestorId
        {
            ItemId = itemId,
            ParentItemId = ancestorId,
            Item = null!,
            ParentItem = null!
        });
}
