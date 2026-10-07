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
    private const string FolderName = "www.UIndex.org    -    Misery 1990 1080p BluRay x264-OFT";
    private const string FilePath = "/movies/www.UIndex.org    -    Misery 1990 1080p BluRay x264-OFT/Misery 1990 1080p BluRay x264-OFT.mkv";

    private static readonly NamingOptions _namingOptions = new();

    private readonly List<(string Name, int Year)> _searches = [];

    [Fact]
    public async Task FindMatchAsync_FolderNameFindsNothing_SearchesByFileName()
    {
        var match = await TmdbMovieProvider.FindMatchAsync(FolderName, FilePath, null, ParseName, SearchOnlyKnownTitle);

        Assert.NotNull(match);
        Assert.Equal(1234, match.Id);
        Assert.Equal([("www UIndex org Misery", 1990), ("Misery", 1990)], _searches);
    }

    [Fact]
    public async Task FindMatchAsync_FolderNameFindsResults_DoesNotSearchByFileName()
    {
        var match = await TmdbMovieProvider.FindMatchAsync("Misery 1990", FilePath, null, ParseName, SearchOnlyKnownTitle);

        Assert.NotNull(match);
        Assert.Equal(1234, match.Id);
        Assert.Equal([("Misery", 1990)], _searches);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/movies/www.UIndex.org    -    Misery 1990 1080p BluRay x264-OFT.mkv")]
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

        IReadOnlyList<SearchMovie>? results = name == "Misery"
            ? [new SearchMovie { Id = 1234, Title = "Misery", ReleaseDate = new DateTime(1990, 11, 30, 0, 0, 0, DateTimeKind.Utc) }]
            : [];
        return Task.FromResult(results);
    }
}
