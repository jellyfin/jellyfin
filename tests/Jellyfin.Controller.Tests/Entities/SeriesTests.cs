using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities.TV;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class SeriesTests
{
    [Fact]
    public void OrderBySeasonNumber_OrdersBySeasonNumber()
    {
        var seasons = new[]
        {
            new Season { Name = "Post-Enies Lobby", IndexNumber = 20 },
            new Season { Name = "Orange Town", IndexNumber = 2 },
            new Season { Name = "Syrup Village", IndexNumber = 3 }
        };

        var ordered = Series.OrderBySeasonNumber(seasons).ToList();

        Assert.Equal(new[] { 2, 3, 20 }, ordered.Select(i => i.IndexNumber!.Value));
    }

    [Fact]
    public void OrderBySeasonNumber_SeasonWithoutNumber_First()
    {
        var seasons = new[]
        {
            new Season { Name = "Specials", IndexNumber = 0 },
            new Season { Name = "Unknown" },
            new Season { Name = "Orange Town", IndexNumber = 2 }
        };

        var ordered = Series.OrderBySeasonNumber(seasons).ToList();

        Assert.Null(ordered[0].IndexNumber);
        Assert.Equal(0, ordered[1].IndexNumber);
        Assert.Equal(2, ordered[2].IndexNumber);
    }
}
