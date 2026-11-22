using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

[Collection("LibraryManagerTests")]
public sealed class ChronologicalFranchiseSortingTests : IDisposable
{
    private readonly ILibraryManager _previousLibraryManager;
    private readonly IServerConfigurationManager _previousConfigurationManager;
    private readonly ILinkedChildrenService _previousLinkedChildrenService;
    private readonly LibraryOptions _libraryOptions = new();
    private readonly Dictionary<Guid, BaseItem> _items = new();
    private readonly List<BoxSet> _collections = new();

    public ChronologicalFranchiseSortingTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousLinkedChildrenService = BaseItem.LinkedChildrenService;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(_libraryOptions);
        libraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns((Guid id) => _items.GetValueOrDefault(id));
        libraryManager.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns((InternalItemsQuery q) => q.ItemIds.Select(id => _items[id]).ToList());
        BaseItem.LibraryManager = libraryManager.Object;

        var linkedChildren = new Mock<ILinkedChildrenService>();
        linkedChildren.Setup(m => m.GetManualLinkedParentIds(It.IsAny<Guid>(), BaseItemKind.BoxSet))
            .Returns((Guid childId, BaseItemKind? _) => _collections
                .Where(c => c.LinkedChildren.Any(l => l.ItemId.HasValue && l.ItemId.Value.Equals(childId)))
                .Select(c => c.Id)
                .ToList());
        BaseItem.LinkedChildrenService = linkedChildren.Object;

        var configManager = new Mock<IServerConfigurationManager>();
        configManager.Setup(m => m.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configManager.Object;
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
        BaseItem.ConfigurationManager = _previousConfigurationManager;
        BaseItem.LinkedChildrenService = _previousLinkedChildrenService;
    }

    [Theory]
    [InlineData("Northwind", "Northwind")]
    [InlineData("Northwind: The Next Voyage", "Northwind")]
    [InlineData("  Ironvale: Rising ", "Ironvale")]
    [InlineData("Ironvale (2008)", "Ironvale")]
    [InlineData("Ironvale Origins", "Ironvale Origins")]
    [InlineData("Sky-Runner: The Series", "Sky-Runner")]
    [InlineData("Northwind - The Specials", "Northwind")]
    [InlineData("Northwind \u2013 The Specials", "Northwind")]
    [InlineData("Sky-Runner '97", "Sky-Runner '97")]
    [InlineData("(2008)", "(2008)")]
    public void Series_GetFranchiseName_UsesNameBeforeColonOrDash(string name, string expected)
    {
        Assert.Equal(expected, Series.GetFranchiseName(name));
    }

    [Theory]
    [InlineData("Northwind Collection", "Northwind")]
    [InlineData("The Northwind Saga Collection", "The Northwind Saga")]
    [InlineData("Northwind Trilogy", "Northwind Trilogy")]
    [InlineData("Collection", "Collection")]
    public void GetCollectionFranchiseName_StripsCollectionSuffix(string collectionName, string expected)
    {
        Assert.Equal(expected, BaseItem.GetCollectionFranchiseName(collectionName));
    }

    [Fact]
    public void Series_Enabled_SortsFranchiseTogetherInReleaseOrder()
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = true;
        var series = new[]
        {
            NewSeries("Northwind: Horizons", 2020),
            NewSeries("Northwinds Bay", 1997),
            NewSeries("Northwind", 1966),
            NewSeries("Northwind: The Next Voyage", 1987),
            NewSeries("Northern Lights: Aurora", 2022),
            NewSeries("Northwind: Deep Harbor", 1993),
        };

        var ordered = series.OrderBy(s => s.SortName, StringComparer.Ordinal).Select(s => s.Name).ToArray();

        Assert.Equal(
            new[] { "Northern Lights: Aurora", "Northwind", "Northwind: The Next Voyage", "Northwind: Deep Harbor", "Northwind: Horizons", "Northwinds Bay" },
            ordered);
    }

    [Theory]
    [InlineData(false, "Northwind: Again")]
    [InlineData(true, "Northwind: Beyond")]
    public void Series_Option_ControlsOrder(bool enabled, string expectedFirst)
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = enabled;
        var later = NewSeries("Northwind: Again", 2017);
        var earlier = NewSeries("Northwind: Beyond", 1995);

        var first = string.CompareOrdinal(later.SortName, earlier.SortName) < 0 ? later : earlier;

        Assert.Equal(expectedFirst, first.Name);
    }

    [Fact]
    public void Movie_Enabled_SortsCollectionTogetherInReleaseOrder()
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = true;
        var movies = new[]
        {
            NewMovie("Ironvale: The Second Gate", 1984, "Ironvale Collection"),
            NewMovie("Raid on the Silent Tower", 1981, "Ironvale Collection"),
            NewMovie("Ironvale: The Last Gate", 1989, "Ironvale Collection"),
            NewMovie("Glass Orchard", 2010, null),
        };

        var ordered = movies.OrderBy(m => m.SortName, StringComparer.Ordinal).Select(m => m.Name).ToArray();

        Assert.Equal(
            new[] { "Glass Orchard", "Raid on the Silent Tower", "Ironvale: The Second Gate", "Ironvale: The Last Gate" },
            ordered);
    }

    [Fact]
    public void Movie_ForcedSortName_StillWins()
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = true;
        var movie = NewMovie("Raid on the Silent Tower", 1981, "Ironvale Collection");
        movie.ForcedSortName = "Aaa";

        Assert.Equal("aaa", movie.SortName);
    }

    [Theory]
    [InlineData("Northwind: Part Two", "Northwind", true)]
    [InlineData("Northwind Origins", "Northwind", true)]
    [InlineData("The Glass Orchard", "Glass Orchard", true)]
    [InlineData("Glass Orchard", "glass orchard", true)]
    [InlineData("Captain's Log", "Captains", true)]
    [InlineData("Log", "Captain", false)]
    [InlineData("Northwinds", "Northwind", false)]
    [InlineData("Harbor Lights", "Northwind", false)]
    [InlineData("Snow Day", "Holidays", false)]
    public void IsNamedAfterFranchise_MatchesWholeLeadingWords(string title, string franchise, bool expected)
    {
        Assert.Equal(expected, BaseItem.IsNamedAfterFranchise(title, franchise));
    }

    [Fact]
    public void Series_InFranchiseCollection_SortTogetherInReleaseOrder()
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = true;
        var original = NewSeries("Northwind: The Early Years", 2008);
        var spinOffA = NewSeries("Harbor Lights", 2019);
        var spinOffB = NewSeries("Anchor Point", 2022);
        var drama = NewSeries("Glass Orchard", 2008);
        var prequel = NewSeries("Before the Orchard", 2015);
        var unrelated = NewSeries("Blue Meadow", 2003);
        AddCollection("Northwind", original, spinOffA, spinOffB);
        AddCollection("Glass Orchard", drama, prequel);

        var ordered = new[] { spinOffB, prequel, spinOffA, unrelated, original, drama }
            .OrderBy(s => s.SortName, StringComparer.Ordinal).Select(s => s.Name).ToArray();

        Assert.Equal(
            new[] { "Blue Meadow", "Glass Orchard", "Before the Orchard", "Northwind: The Early Years", "Harbor Lights", "Anchor Point" },
            ordered);
    }

    [Fact]
    public void ThemedCollection_DoesNotChangeOrder()
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = true;
        var first = NewMovie("Snow Day", 1990, null);
        var second = NewMovie("Winter Lights", 2003, null);
        AddCollection("Holidays", first, second);

        Assert.Equal("snow day", first.SortName);
        Assert.Equal("winter lights", second.SortName);
    }

    [Fact]
    public void Movie_InFranchiseCollection_WithoutTmdbCollection_SortsWithFranchise()
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = true;
        var original = NewMovie("Ironvale", 1978, null);
        var sequel = NewMovie("Ironvale II", 1981, null);
        var third = NewMovie("Season of Ash", 1982, null);
        AddCollection("Ironvale Collection", original, sequel, third);

        var ordered = new[] { third, sequel, original }
            .OrderBy(m => m.SortName, StringComparer.Ordinal).Select(m => m.Name).ToArray();

        Assert.Equal(new[] { "Ironvale", "Ironvale II", "Season of Ash" }, ordered);
    }

    [Fact]
    public void Collections_Ignored_WhenOptionDisabled()
    {
        var spinOff = NewSeries("Anchor Point", 2022);
        AddCollection("Northwind", NewSeries("Northwind: The Early Years", 2008), spinOff);

        Assert.Equal("anchor point", spinOff.SortName);
    }

    private void AddCollection(string name, params BaseItem[] members)
    {
        var collection = new BoxSet { Id = Guid.NewGuid(), Name = name };
        foreach (var member in members)
        {
            _items[member.Id] = member;
        }

        collection.LinkedChildren = members.Select(LinkedChild.Create).ToArray();
        _items[collection.Id] = collection;
        _collections.Add(collection);
    }

    private static Series NewSeries(string name, int year)
        => new() { Id = Guid.NewGuid(), Name = name, PremiereDate = new DateTime(year, 9, 1, 0, 0, 0, DateTimeKind.Utc), ProductionYear = year };

    private static Movie NewMovie(string name, int year, string? collection)
        => new() { Id = Guid.NewGuid(), Name = name, PremiereDate = new DateTime(year, 6, 1, 0, 0, 0, DateTimeKind.Utc), ProductionYear = year, CollectionName = collection! };
}
