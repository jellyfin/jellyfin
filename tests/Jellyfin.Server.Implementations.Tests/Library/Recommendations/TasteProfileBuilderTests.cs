using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Library.Recommendations;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library.Recommendations;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.Recommendations;

/// <summary>
/// Unit tests for <see cref="TasteProfileBuilder"/>.
/// </summary>
public sealed class TasteProfileBuilderTests
{
    /// <summary>
    /// An empty history should produce a zero-mass profile with no entries in any collection.
    /// </summary>
    [Fact]
    public void Build_EmptyHistory_ReturnsEmptyProfile()
    {
        var profile = TasteProfileBuilder.Build(
            BaseItemKind.Movie,
            historyItems: Array.Empty<BaseItem>(),
            isPlayed: _ => false,
            isFavorite: _ => false,
            isLiked: _ => false,
            peopleByItem: new Dictionary<Guid, IReadOnlyList<PersonInfo>>());

        Assert.Equal(0f, profile.TotalSignalMass);
        Assert.Empty(profile.Genres);
        Assert.Empty(profile.Tags);
        Assert.Empty(profile.People);
        Assert.Empty(profile.Studios);
    }

    /// <summary>
    /// A single watched item should accumulate the expected per-field weighted scores.
    /// </summary>
    [Fact]
    public void Build_SingleWatchedItem_AccumulatesExpectedFieldWeights()
    {
        var item = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Inception",
            Genres = new[] { "Sci-Fi", "Thriller" },
            Tags = new[] { "dreams" },
            Studios = new[] { "Warner Bros" }
        };
        var personId = Guid.NewGuid();
        var people = new Dictionary<Guid, IReadOnlyList<PersonInfo>>
        {
            [item.Id] = new[]
            {
                new PersonInfo { Id = personId, Name = "Nolan", Type = PersonKind.Director, SortOrder = 0, ItemId = item.Id }
            }
        };

        var profile = TasteProfileBuilder.Build(
            BaseItemKind.Movie,
            historyItems: new BaseItem[] { item },
            isPlayed: i => i.Id.Equals(item.Id),
            isFavorite: _ => false,
            isLiked: _ => false,
            peopleByItem: people);

        // signal = 1.0 (played) × field weights:
        //   genre 3.0 each, tag 1.5 each, studio 0.5 each, person 1.0 each
        Assert.Equal(3.0f, profile.Genres["Sci-Fi"]);
        Assert.Equal(3.0f, profile.Genres["Thriller"]);
        Assert.Equal(1.5f, profile.Tags["dreams"]);
        Assert.Equal(0.5f, profile.Studios["Warner Bros"]);
        Assert.Equal(1.0f, profile.People[personId]);
        // total = 3+3+1.5+0.5+1 = 9
        Assert.Equal(9.0f, profile.TotalSignalMass);
    }

    /// <summary>
    /// Watched and favorited signals should stack so the total genre weight is 9.0.
    /// </summary>
    [Fact]
    public void Build_WatchedAndFavorited_StacksSignals()
    {
        var item = new Movie
        {
            Id = Guid.NewGuid(),
            Genres = new[] { "Drama" }
        };

        var profile = TasteProfileBuilder.Build(
            BaseItemKind.Movie,
            historyItems: new BaseItem[] { item },
            isPlayed: _ => true,
            isFavorite: _ => true,
            isLiked: _ => false,
            peopleByItem: new Dictionary<Guid, IReadOnlyList<PersonInfo>>());

        // signal = 1.0 + 2.0 = 3.0; genre weight 3.0 → 3 × 3 = 9.0
        Assert.Equal(9.0f, profile.Genres["Drama"]);
    }

    /// <summary>
    /// Items of a different kind than the requested kind must not contribute to the profile.
    /// </summary>
    [Fact]
    public void Build_PerKindIsolation_DoesNotMixSeriesIntoMovieProfile()
    {
        var movie = new Movie { Id = Guid.NewGuid(), Genres = new[] { "Drama" } };
        var series = new MediaBrowser.Controller.Entities.TV.Series { Id = Guid.NewGuid(), Genres = new[] { "Comedy" } };

        var profile = TasteProfileBuilder.Build(
            BaseItemKind.Movie,
            historyItems: new BaseItem[] { movie, series },
            isPlayed: _ => true,
            isFavorite: _ => false,
            isLiked: _ => false,
            peopleByItem: new Dictionary<Guid, IReadOnlyList<PersonInfo>>());

        Assert.True(profile.Genres.ContainsKey("Drama"));
        Assert.False(profile.Genres.ContainsKey("Comedy"));
    }

    /// <summary>
    /// Only the top 5 people (by ascending SortOrder) should be included per item.
    /// </summary>
    [Fact]
    public void Build_CapsPeopleAtTop5BySortOrder()
    {
        var item = new Movie { Id = Guid.NewGuid() };
        var people = Enumerable.Range(0, 10)
            .Select(i => new PersonInfo { Id = Guid.NewGuid(), Name = $"P{i}", SortOrder = i, ItemId = item.Id })
            .ToArray();
        var lookup = new Dictionary<Guid, IReadOnlyList<PersonInfo>> { [item.Id] = people };

        var profile = TasteProfileBuilder.Build(
            BaseItemKind.Movie,
            historyItems: new BaseItem[] { item },
            isPlayed: _ => true,
            isFavorite: _ => false,
            isLiked: _ => false,
            peopleByItem: lookup);

        Assert.Equal(5, profile.People.Count);
        // The 5 with lowest SortOrder are kept
        for (var i = 0; i < 5; i++)
        {
            Assert.True(profile.People.ContainsKey(people[i].Id));
        }
    }
}
