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
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";

    private static readonly DateTime CompletedDate = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ControlDate = new(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PartialDate = new(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PartialOnlyDate = new(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OtherUserDate = new(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

    private readonly User _userA = new("user-a", "auth-provider", "reset-provider");
    private readonly User _userB = new("user-b", "auth-provider", "reset-provider");
    private readonly Guid _seriesA = Guid.NewGuid();
    private readonly Guid _seriesB = Guid.NewGuid();
    private readonly Guid _seriesC = Guid.NewGuid();
    private readonly Guid _seriesD = Guid.NewGuid();
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

    private InternalItemsQuery CreateQuery(bool search, bool userScoped, SortOrder sortOrder)
        => new(userScoped ? _userA : null)
        {
            IncludeItemTypes = [BaseItemKind.Series],
            OrderBy = [(ItemSortBy.SeriesDatePlayed, sortOrder)],
            SearchTerm = search ? "probe" : null
        };

    private bool IsDatedSeries(Guid id)
        => !id.Equals(_seriesD);

    private void Seed(JellyfinDbContext context)
    {
        context.Users.AddRange(_userA, _userB);

        AddSeries(
            context,
            _seriesA,
            "probe alpha",
            (CompletedDate, true, 0L, _userA),
            (PartialDate, false, 900L, _userA));
        AddSeries(
            context,
            _seriesB,
            "probe beta",
            (ControlDate, true, 0L, _userA),
            (OtherUserDate, true, 0L, _userB));
        AddSeries(context, _seriesC, "probe gamma", (PartialOnlyDate, false, 900L, _userA));
        AddSeries(context, _seriesD, "probe delta", (null, false, 0L, _userA));

        context.SaveChanges();
    }

    private void AddSeries(
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
            IsSeries = true,
            IsVirtualItem = false
        });

        for (var i = 0; i < episodes.Length; i++)
        {
            var episodeId = Guid.NewGuid();
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
                IsVirtualItem = false
            });

            var episode = episodes[i];
            context.UserData.Add(new UserData
            {
                ItemId = episodeId,
                UserId = episode.User.Id,
                CustomDataKey = $"{episodeId:N}-{episode.User.Id:N}",
                LastPlayedDate = episode.LastPlayedDate,
                Played = episode.Played,
                PlaybackPositionTicks = episode.PlaybackPositionTicks,
                Item = null!,
                User = episode.User
            });
        }
    }
}
