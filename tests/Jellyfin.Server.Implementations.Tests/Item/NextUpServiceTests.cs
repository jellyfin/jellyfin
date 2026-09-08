using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class NextUpServiceTests : SqliteDbTestFixture
{
    private readonly User _user = new("user", "auth", "reset");
    private readonly User _otherUser = new("other", "auth", "reset");
    private readonly Guid _inaccessibleId = Guid.NewGuid();
    private readonly Guid _seriesALastWatchedId = Guid.NewGuid();
    private readonly Guid _seriesANextUpId = Guid.NewGuid();
    private readonly Guid _seriesBLastWatchedId = Guid.NewGuid();
    private readonly Guid _seriesBNextUpId = Guid.NewGuid();
    private readonly NextUpService _service;

    public NextUpServiceTests()
    {
        using (var context = CreateDbContext())
        {
            Seed(context);
        }

        var helpers = new Mock<IItemQueryHelpers>();
        helpers.Setup(h => h.ApplyAccessFiltering(
                It.IsAny<JellyfinDbContext>(),
                It.IsAny<IQueryable<BaseItemEntity>>(),
                It.IsAny<InternalItemsQuery>()))
            .Returns((JellyfinDbContext _, IQueryable<BaseItemEntity> query, InternalItemsQuery _) =>
                query.Where(entity => !entity.Id.Equals(_inaccessibleId)));
        helpers.Setup(h => h.ApplyNavigations(It.IsAny<IQueryable<BaseItemEntity>>(), It.IsAny<InternalItemsQuery>()))
            .Returns((IQueryable<BaseItemEntity> query, InternalItemsQuery _) => query);
        helpers.Setup(h => h.DeserializeBaseItem(It.IsAny<BaseItemEntity>(), It.IsAny<bool>()))
            .Returns((BaseItemEntity entity, bool _) => new Episode
            {
                Id = entity.Id,
                ParentIndexNumber = entity.ParentIndexNumber,
                IndexNumber = entity.IndexNumber
            });

        _service = new NextUpService(CreateDbContextFactory(), new ItemTypeLookup(), helpers.Object);
    }

    [Fact]
    public void Batch_UsesLatestAccessibleCurrentUserPlaybackAcrossAlternateAndSpecials()
    {
        var results = _service.GetNextUpEpisodesBatch(
            new InternalItemsQuery(_user),
            ["series-a", "series-b"],
            includeSpecials: false,
            includeWatchedForRewatching: false);

        Assert.Equal(new DateTime(2025, 1, 10), results["series-a"].LastPlayedDate);
        Assert.Equal(new DateTime(2025, 1, 12), results["series-b"].LastPlayedDate);
        Assert.Equal(_seriesALastWatchedId, results["series-a"].LastWatched!.Id);
        Assert.Equal(_seriesANextUpId, results["series-a"].NextUp!.Id);
        Assert.Equal(_seriesBLastWatchedId, results["series-b"].LastWatched!.Id);
        Assert.Equal(_seriesBNextUpId, results["series-b"].NextUp!.Id);
    }

    private void Seed(JellyfinDbContext context)
    {
        context.Users.AddRange(_user, _otherUser);

        AddEpisode(context, _seriesALastWatchedId, "series-a", 1, 1);
        AddEpisode(context, _seriesANextUpId, "series-a", 1, 2);
        var alternateId = Guid.NewGuid();
        AddEpisode(context, alternateId, "series-a", 1, 1, _seriesALastWatchedId);
        AddEpisode(context, Guid.NewGuid(), "series-a", 0, 1);
        AddEpisode(context, _inaccessibleId, "series-a", 1, 3);

        AddEpisode(context, _seriesBLastWatchedId, "series-b", 1, 1);
        AddEpisode(context, _seriesBNextUpId, "series-b", 1, 2);
        AddEpisode(context, Guid.NewGuid(), "series-b", 0, 1);

        AddUserData(context, _seriesALastWatchedId, _user, new DateTime(2025, 1, 1), played: true);
        AddUserData(context, _seriesANextUpId, _otherUser, new DateTime(2025, 2, 1), played: false);
        AddUserData(context, alternateId, _user, new DateTime(2025, 1, 10), played: false);
        AddUserData(context, context.BaseItems.Local.First(e => e.SeriesPresentationUniqueKey == "series-a" && e.ParentIndexNumber == 0 && !e.Id.Equals(BaseItemRepository.PlaceholderId)).Id, _user, new DateTime(2025, 1, 5), played: false);
        AddUserData(context, _inaccessibleId, _user, new DateTime(2025, 3, 1), played: false);
        AddUserData(context, BaseItemRepository.PlaceholderId, _user, new DateTime(2025, 4, 1), played: false);

        AddUserData(context, _seriesBLastWatchedId, _user, new DateTime(2025, 1, 2), played: true);
        AddUserData(context, context.BaseItems.Local.First(e => e.SeriesPresentationUniqueKey == "series-b" && e.ParentIndexNumber == 0).Id, _user, new DateTime(2025, 1, 12), played: false);

        context.SaveChanges();
    }

    private static void AddEpisode(JellyfinDbContext context, Guid id, string seriesKey, int season, int episode, Guid? primaryVersionId = null)
    {
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = new ItemTypeLookup().BaseItemKindNames[BaseItemKind.Episode],
            Name = id.ToString(),
            SortName = id.ToString(),
            SeriesPresentationUniqueKey = seriesKey,
            ParentIndexNumber = season,
            IndexNumber = episode,
            PrimaryVersionId = primaryVersionId
        });
    }

    private static void AddUserData(JellyfinDbContext context, Guid itemId, User user, DateTime lastPlayedDate, bool played)
    {
        context.UserData.Add(new UserData
        {
            ItemId = itemId,
            UserId = user.Id,
            CustomDataKey = itemId.ToString("N"),
            LastPlayedDate = lastPlayedDate,
            Played = played,
            Item = null!,
            User = null!
        });
    }
}
