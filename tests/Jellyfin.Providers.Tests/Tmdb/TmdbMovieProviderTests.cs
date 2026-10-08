using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Providers.Plugins.Tmdb.Movies;
using TMDbLib.Objects.Search;
using Xunit;

namespace Jellyfin.Providers.Tests.Tmdb;

public class TmdbMovieProviderTests
{
    private const string FolderName = "www.example.org    -    Hoop Dreams 1994 1080p";
    private const string FilePath = "/movies/www.example.org    -    Hoop Dreams 1994 1080p/Hoop Dreams 1994 1080p.mkv";

    private static readonly NamingOptions _namingOptions = new();

    private readonly List<(string Name, int Year)> _searches = [];

    [Fact]
    public async Task FindMatchAsync_FolderNameFindsNothing_SearchesByFileName()
    {
        var match = await TmdbMovieProvider.FindMatchAsync(FolderName, FilePath, null, ParseName, SearchOnlyKnownTitle);

        Assert.NotNull(match);
        Assert.Equal(1234, match.Id);
        Assert.Equal([("www example org Hoop Dreams", 1994), ("Hoop Dreams", 1994)], _searches);
    }

    [Fact]
    public async Task FindMatchAsync_FolderNameFindsResults_DoesNotSearchByFileName()
    {
        var match = await TmdbMovieProvider.FindMatchAsync("Hoop Dreams 1994", FilePath, null, ParseName, SearchOnlyKnownTitle);

        Assert.NotNull(match);
        Assert.Equal(1234, match.Id);
        Assert.Equal([("Hoop Dreams", 1994)], _searches);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/movies/www.example.org    -    Hoop Dreams 1994 1080p.mkv")]
    public async Task FindMatchAsync_NoDifferentFileName_SearchesOnce(string? path)
    {
        var match = await TmdbMovieProvider.FindMatchAsync(FolderName, path, null, ParseName, SearchOnlyKnownTitle);

        Assert.Null(match);
        Assert.Single(_searches);
    }

    [Fact]
    public async Task FindMatchAsync_NeitherNameFindsAnything_ReturnsNull()
    {
        var match = await TmdbMovieProvider.FindMatchAsync(FolderName, FilePath, null, ParseName, (_, _) => Task.FromResult<IReadOnlyList<SearchMovie>?>([]));

        Assert.Null(match);
    }

    private static ItemLookupInfo ParseName(string name)
    {
        var result = VideoResolver.CleanDateTime(name, _namingOptions);
        return new ItemLookupInfo
        {
            Name = VideoResolver.TryCleanString(result.Name, _namingOptions, out var newName) ? newName : result.Name,
            Year = result.Year
        };
    }

    private Task<IReadOnlyList<SearchMovie>?> SearchOnlyKnownTitle(string name, int year)
    {
        _searches.Add((name, year));

        IReadOnlyList<SearchMovie> results = name == "Hoop Dreams"
            ? [new SearchMovie { Id = 1234, Title = "Hoop Dreams", ReleaseDate = new DateTime(1994, 10, 14, 0, 0, 0, DateTimeKind.Utc) }]
            : [];
        return Task.FromResult<IReadOnlyList<SearchMovie>?>(results);
    }
}
