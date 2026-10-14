using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;
using SortOrder = Jellyfin.Database.Implementations.Enums.SortOrder;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A missing value is the lowest value of its sort key: it comes first when ascending and last when
/// descending. Each item gets a distinct name so no assertion relies on how equal keys are ordered.
/// </summary>
public sealed class BaseItemRepositoryNullOrderTests : JellyfinDbTestFixture
{
    private const string MissingName = "Missing";
    private const string LowName = "Low";
    private const string HighName = "High";

    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();
    private readonly User _user = new("test", "auth", "reset");

    public BaseItemRepositoryNullOrderTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Theory]
    [InlineData(ItemSortBy.PremiereDate)]
    [InlineData(ItemSortBy.DateCreated)]
    [InlineData(ItemSortBy.CommunityRating)]
    [InlineData(ItemSortBy.CriticRating)]
    [InlineData(ItemSortBy.ProductionYear)]
    [InlineData(ItemSortBy.IndexNumber)]
    [InlineData(ItemSortBy.ParentIndexNumber)]
    [InlineData(ItemSortBy.Album)]
    [InlineData(ItemSortBy.SeriesSortName)]
    public void GetItemList_NullableSortKey_OrdersMissingValueLowest(ItemSortBy sortBy)
    {
        AssertMissingValueIsLowest(sortBy);
    }

    [Fact]
    public void GetItemList_DatePlayed_OrdersMissingValueLowest()
    {
        KnownGap.On(KnownGaps.OuterReferenceInDerivedTable, () => AssertMissingValueIsLowest(ItemSortBy.DatePlayed));
    }

    private void AssertMissingValueIsLowest(ItemSortBy sortBy)
    {
        using (var context = CreateDbContext())
        {
            context.Users.Add(_user);
            AddMovie(context, MissingName, rank: 0);
            AddMovie(context, LowName, rank: 1);
            AddMovie(context, HighName, rank: 2);
            context.SaveChanges();
        }

        Assert.Equal([MissingName, LowName, HighName], Names(sortBy, SortOrder.Ascending));
        Assert.Equal([HighName, LowName, MissingName], Names(sortBy, SortOrder.Descending));
    }

    private string[] Names(ItemSortBy sortBy, SortOrder sortOrder)
    {
        return _repository
            .GetItemList(new InternalItemsQuery(_user)
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                OrderBy = [(sortBy, sortOrder)]
            })
            .Select(i => i.Name)
            .ToArray();
    }

    private void AddMovie(JellyfinDbContext context, string name, int rank)
    {
        var id = Guid.NewGuid();
        var movie = new BaseItemEntity
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
            Name = name,
            SortName = name,
            PresentationUniqueKey = id.ToString("N"),
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };

        if (rank > 0)
        {
            var date = new DateTime(2000 + rank, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            movie.PremiereDate = date;
            movie.DateCreated = date;
            movie.CommunityRating = rank;
            movie.CriticRating = rank;
            movie.ProductionYear = 2000 + rank;
            movie.IndexNumber = rank;
            movie.ParentIndexNumber = rank;
            movie.Album = $"Album {rank}";
            movie.SeriesName = $"Series {rank}";
            context.UserData.Add(new UserData
            {
                ItemId = id,
                UserId = _user.Id,
                CustomDataKey = id.ToString("N"),
                LastPlayedDate = date,
                Item = null!,
                User = null!
            });
        }

        context.BaseItems.Add(movie);
    }
}
