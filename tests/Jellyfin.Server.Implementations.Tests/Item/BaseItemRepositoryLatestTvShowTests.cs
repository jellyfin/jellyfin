using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using CollectionType = Jellyfin.Data.Enums.CollectionType;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the latest TV items of a library when episodes carry no creation date.
/// </summary>
public sealed class BaseItemRepositoryLatestTvShowTests : SqliteDbTestFixture
{
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";

    private readonly BaseItemRepository _repository;
    private readonly User _user = new("test", "auth-provider", "reset-provider");

    public BaseItemRepositoryLatestTvShowTests()
    {
        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void GetLatestItemList_EveryEpisodeWithoutCreationDate_ReturnsAnEpisode()
    {
        var undated = new[] { Guid.NewGuid(), Guid.NewGuid() };
        using (var context = CreateDbContext())
        {
            context.Users.Add(_user);
            AddEpisode(context, undated[0], "Undated", null);
            AddEpisode(context, undated[1], "Undated", null);
            context.SaveChanges();
        }

        var items = _repository.GetLatestItemList(Query(), CollectionType.tvshows);

        Assert.Contains(Assert.Single(items).Id, undated);
    }

    [Fact]
    public void GetLatestItemList_UndatedSeriesBesideADatedOne_ReturnsTheDatedEpisode()
    {
        var dated = Guid.NewGuid();
        using (var context = CreateDbContext())
        {
            context.Users.Add(_user);
            AddEpisode(context, dated, "Dated", new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));
            AddEpisode(context, Guid.NewGuid(), "Undated", null);
            context.SaveChanges();
        }

        var items = _repository.GetLatestItemList(Query(), CollectionType.tvshows);

        Assert.Equal(dated, Assert.Single(items).Id);
    }

    private InternalItemsQuery Query()
        => new(_user)
        {
            IncludeItemTypes = [BaseItemKind.Episode],
            Limit = 10
        };

    private static void AddEpisode(JellyfinDbContext context, Guid id, string seriesName, DateTime? dateCreated)
        => context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = EpisodeType,
            Name = $"{seriesName} {id:N}",
            SeriesName = seriesName,
            SeriesId = Guid.NewGuid(),
            PresentationUniqueKey = id.ToString("N"),
            DateCreated = dateCreated
        });
}
