using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ItemSortBy = Jellyfin.Data.Enums.ItemSortBy;
using SortOrder = Jellyfin.Database.Implementations.Enums.SortOrder;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// An item with a production year but no premiere date sorts as if it premiered at the start of
/// that year, among the items that have a premiere date. An item with neither is the lowest.
/// Each item gets a distinct name so no assertion relies on how equal keys are ordered.
/// </summary>
public sealed class BaseItemRepositoryPremiereDateOrderTests : JellyfinDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();
    private readonly User _user = new("test", "auth", "reset");

    public BaseItemRepositoryPremiereDateOrderTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Fact]
    public void GetItemList_PremiereDate_SortsYearOnlyItemsAmongDatedItems()
    {
        using (var context = CreateDbContext())
        {
            context.Users.Add(_user);
            AddMovie(context, "Neither");
            AddMovie(context, "Year 2021", productionYear: 2021);
            AddMovie(context, "Dated 15 June 2010", premiereDate: new DateTime(2010, 6, 15, 0, 0, 0, DateTimeKind.Utc));
            AddMovie(context, "Year 9999", productionYear: 9999);
            AddMovie(context, "Year 1999", productionYear: 1999);
            AddMovie(context, "Dated 1 January 2000", premiereDate: new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            AddMovie(context, "Year 1", productionYear: 1);
            AddMovie(context, "Year 1975", productionYear: 1975);
            AddMovie(context, "Year 2010", productionYear: 2010);
            AddMovie(context, "Dated 31 December 2009", premiereDate: new DateTime(2009, 12, 31, 23, 59, 59, DateTimeKind.Utc));
            AddMovie(context, "Dated 1 January 2010, one second in", premiereDate: new DateTime(2010, 1, 1, 0, 0, 1, DateTimeKind.Utc));
            AddMovie(context, "Year 2011", productionYear: 2011);
            context.SaveChanges();
        }

        string[] ascending =
        [
            "Neither",
            "Year 1",
            "Year 1975",
            "Year 1999",
            "Dated 1 January 2000",
            "Dated 31 December 2009",
            "Year 2010",
            "Dated 1 January 2010, one second in",
            "Dated 15 June 2010",
            "Year 2011",
            "Year 2021",
            "Year 9999"
        ];

        Assert.Equal(ascending, Names(SortOrder.Ascending));
        Assert.Equal(ascending.Reverse(), Names(SortOrder.Descending));
    }

    [Fact]
    public void GetItemList_PremiereDate_PrefersPremiereDateToProductionYear()
    {
        using (var context = CreateDbContext())
        {
            context.Users.Add(_user);
            AddMovie(context, "Year 2005", productionYear: 2005);
            AddMovie(
                context,
                "Premiered 2010, year 1980",
                premiereDate: new DateTime(2010, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                productionYear: 1980);
            AddMovie(context, "Year 2015", productionYear: 2015);
            context.SaveChanges();
        }

        Assert.Equal(["Year 2005", "Premiered 2010, year 1980", "Year 2015"], Names(SortOrder.Ascending));
    }

    private string[] Names(SortOrder sortOrder)
    {
        return _repository
            .GetItemList(new InternalItemsQuery(_user)
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                OrderBy = [(ItemSortBy.PremiereDate, sortOrder)]
            })
            .Select(i => i.Name)
            .ToArray();
    }

    private void AddMovie(JellyfinDbContext context, string name, DateTime? premiereDate = null, int? productionYear = null)
    {
        var id = Guid.NewGuid();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
            Name = name,
            SortName = name,
            PresentationUniqueKey = id.ToString("N"),
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false,
            PremiereDate = premiereDate,
            ProductionYear = productionYear
        });
    }
}
