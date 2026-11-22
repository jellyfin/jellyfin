using System;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

[Collection("LibraryManagerTests")]
public sealed class ChronologicalFranchiseSortingTests : IDisposable
{
    private readonly ILibraryManager _previousLibraryManager;
    private readonly IServerConfigurationManager _previousConfigurationManager;
    private readonly LibraryOptions _libraryOptions = new();

    public ChronologicalFranchiseSortingTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(_libraryOptions);
        BaseItem.LibraryManager = libraryManager.Object;

        var configManager = new Mock<IServerConfigurationManager>();
        configManager.Setup(m => m.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configManager.Object;
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
        BaseItem.ConfigurationManager = _previousConfigurationManager;
    }

    [Theory]
    [InlineData("Star Trek", "Star Trek")]
    [InlineData("Star Trek: The Next Generation", "Star Trek")]
    [InlineData("  Vikings: Valhalla ", "Vikings")]
    [InlineData("Merlin (2008)", "Merlin")]
    [InlineData("Stargate SG-1", "Stargate SG-1")]
    [InlineData("(2008)", "(2008)")]
    public void Series_GetFranchiseName_UsesNameBeforeColon(string name, string expected)
    {
        Assert.Equal(expected, Series.GetFranchiseName(name));
    }

    [Theory]
    [InlineData("Indiana Jones Collection", "Indiana Jones")]
    [InlineData("The Lord of the Rings Collection", "The Lord of the Rings")]
    [InlineData("Burnt by the Sun Trilogy", "Burnt by the Sun Trilogy")]
    [InlineData("Collection", "Collection")]
    public void Movie_GetFranchiseName_StripsCollectionSuffix(string collectionName, string expected)
    {
        Assert.Equal(expected, Movie.GetFranchiseName(collectionName));
    }

    [Fact]
    public void Series_Enabled_SortsFranchiseTogetherInReleaseOrder()
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = true;
        var series = new[]
        {
            NewSeries("Star Trek: Picard", 2020),
            NewSeries("Stargate SG-1", 1997),
            NewSeries("Star Trek", 1966),
            NewSeries("Star Trek: The Next Generation", 1987),
            NewSeries("Star Wars: Andor", 2022),
            NewSeries("Star Trek: Deep Space Nine", 1993),
        };

        var ordered = series.OrderBy(s => s.SortName, StringComparer.Ordinal).Select(s => s.Name).ToArray();

        Assert.Equal(
            new[] { "Star Trek", "Star Trek: The Next Generation", "Star Trek: Deep Space Nine", "Star Trek: Picard", "Star Wars: Andor", "Stargate SG-1" },
            ordered);
    }

    [Theory]
    [InlineData(false, "Star Trek: Discovery")]
    [InlineData(true, "Star Trek: Voyager")]
    public void Series_Option_ControlsOrder(bool enabled, string expectedFirst)
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = enabled;
        var discovery = NewSeries("Star Trek: Discovery", 2017);
        var voyager = NewSeries("Star Trek: Voyager", 1995);

        var first = string.CompareOrdinal(discovery.SortName, voyager.SortName) < 0 ? discovery : voyager;

        Assert.Equal(expectedFirst, first.Name);
    }

    [Fact]
    public void Movie_Enabled_SortsCollectionTogetherInReleaseOrder()
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = true;
        var movies = new[]
        {
            NewMovie("Indiana Jones and the Temple of Doom", 1984, "Indiana Jones Collection"),
            NewMovie("Raiders of the Lost Ark", 1981, "Indiana Jones Collection"),
            NewMovie("Indiana Jones and the Last Crusade", 1989, "Indiana Jones Collection"),
            NewMovie("Inception", 2010, null),
        };

        var ordered = movies.OrderBy(m => m.SortName, StringComparer.Ordinal).Select(m => m.Name).ToArray();

        Assert.Equal(
            new[] { "Inception", "Raiders of the Lost Ark", "Indiana Jones and the Temple of Doom", "Indiana Jones and the Last Crusade" },
            ordered);
    }

    [Fact]
    public void Movie_ForcedSortName_StillWins()
    {
        _libraryOptions.EnableChronologicalFranchiseSorting = true;
        var movie = NewMovie("Raiders of the Lost Ark", 1981, "Indiana Jones Collection");
        movie.ForcedSortName = "Aaa";

        Assert.Equal("aaa", movie.SortName);
    }

    private static Series NewSeries(string name, int year)
        => new() { Name = name, PremiereDate = new DateTime(year, 9, 1, 0, 0, 0, DateTimeKind.Utc), ProductionYear = year };

    private static Movie NewMovie(string name, int year, string? collection)
        => new() { Name = name, PremiereDate = new DateTime(year, 6, 1, 0, 0, 0, DateTimeKind.Utc), ProductionYear = year, CollectionName = collection! };
}
