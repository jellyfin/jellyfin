using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryLatestTests : SqliteDbTestFixture
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(20, 4)]
    public void Latest_SelectsDistinctSeriesByMaximumDate(int limit, int expected)
    {
        using var context = CreateDbContext();
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 30; i++)
        {
            AddEpisode(context, "Newest", date.AddMinutes(-i));
        }

        AddEpisode(context, "Case", date.AddDays(-1));
        AddEpisode(context, "case", date.AddDays(-2));
        AddEpisode(context, "Oldest", date.AddDays(-3));
        AddEpisode(context, null, date.AddDays(1));
        context.SaveChanges();
        var repository = CreateBaseItemRepository(new ItemTypeLookup());
        var result = repository.GetLatestItemList(new InternalItemsQuery { Limit = limit }, CollectionType.tvshows);
        Assert.Equal(expected, result.Count);
        Assert.Equal(new[] { "Newest", "Case", "case", "Oldest" }.Take(expected), result.Select(e => e.Name));
    }

    [Fact]
    public void Latest_EmptyLibraryReturnsEmpty()
    {
        var repository = CreateBaseItemRepository(new ItemTypeLookup());
        Assert.Empty(repository.GetLatestItemList(new InternalItemsQuery { Limit = 16 }, CollectionType.tvshows));
    }

    [Fact]
    public void Latest_DateTiesAndExistingNullDateCutoffArePreserved()
    {
        using var context = CreateDbContext();
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        AddEpisode(context, "A", date);
        AddEpisode(context, "B", date);
        AddEpisode(context, "C", null);
        context.SaveChanges();
        var repository = CreateBaseItemRepository(new ItemTypeLookup());
        var result = repository.GetLatestItemList(new InternalItemsQuery(), CollectionType.tvshows);
        Assert.Equal(new[] { "A", "B" }, result.Select(e => e.Name).OrderBy(e => e, StringComparer.Ordinal));
    }

    private static void AddEpisode(Jellyfin.Database.Implementations.JellyfinDbContext context, string? series, DateTime? date)
    {
        var id = Guid.NewGuid();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = "MediaBrowser.Controller.Entities.TV.Episode",
            Name = series ?? "Episode",
            SortName = series ?? "Episode",
            SeriesName = series,
            DateCreated = date,
            PresentationUniqueKey = id.ToString("N")
        });
    }
}
