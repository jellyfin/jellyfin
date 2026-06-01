using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Library.Recommendations;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library.Recommendations;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.Recommendations;

/// <summary>
/// Unit tests for <see cref="TasteProfileScorer"/>.
/// </summary>
public class TasteProfileScorerTests
{
    private static TasteProfile MakeProfile(params (string Genre, float Weight)[] genres)
    {
        var g = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, w) in genres)
        {
            g[name] = w;
        }

        var total = 0f;
        foreach (var w in g.Values)
        {
            total += w;
        }

        return new TasteProfile(
            BaseItemKind.Movie,
            DateTime.UtcNow,
            g,
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<Guid, float>(),
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
            total);
    }

    /// <summary>
    /// A candidate whose genres overlap the profile more should score higher than one with no overlap.
    /// </summary>
    [Fact]
    public void Score_HigherOverlap_ScoresHigher()
    {
        var profile = MakeProfile(("Sci-Fi", 10f), ("Drama", 5f));
        var sciFi = new Movie { Id = Guid.NewGuid(), Genres = new[] { "Sci-Fi" } };
        var unrelated = new Movie { Id = Guid.NewGuid(), Genres = new[] { "Western" } };

        var sciFiScore = TasteProfileScorer.Score(profile, sciFi, seedItem: null, candidatePeople: Array.Empty<PersonInfo>());
        var unrelatedScore = TasteProfileScorer.Score(profile, unrelated, seedItem: null, candidatePeople: Array.Empty<PersonInfo>());

        Assert.True(sciFiScore > unrelatedScore);
    }

    /// <summary>
    /// Scoring against a zero-mass profile should return 0 without throwing.
    /// </summary>
    [Fact]
    public void Score_EmptyProfile_DoesNotThrowOrDivideByZero()
    {
        var empty = TasteProfile.Empty(BaseItemKind.Movie);
        var candidate = new Movie { Id = Guid.NewGuid(), Genres = new[] { "Sci-Fi" } };

        var score = TasteProfileScorer.Score(empty, candidate, seedItem: null, candidatePeople: Array.Empty<PersonInfo>());
        Assert.Equal(0f, score);
    }

    /// <summary>
    /// Providing a seed item that shares genres with the candidate should yield a higher score
    /// than scoring the same candidate without a seed.
    /// </summary>
    [Fact]
    public void Score_WithSeed_AppliesOverlapBonus()
    {
        var profile = MakeProfile(("Sci-Fi", 1f));
        var candidate = new Movie { Id = Guid.NewGuid(), Genres = new[] { "Sci-Fi", "Thriller" } };
        var seed = new Movie { Id = Guid.NewGuid(), Genres = new[] { "Sci-Fi" } };

        var withSeed = TasteProfileScorer.Score(profile, candidate, seedItem: seed, candidatePeople: Array.Empty<PersonInfo>());
        var withoutSeed = TasteProfileScorer.Score(profile, candidate, seedItem: null, candidatePeople: Array.Empty<PersonInfo>());

        Assert.True(withSeed > withoutSeed);
    }

    /// <summary>
    /// The scorer must use <paramref name="candidatePeople"/> for person signals rather than
    /// loading people from the BaseItem itself.
    /// </summary>
    [Fact]
    public void Score_UsesPeopleFromCandidateLookup_NotFromBaseItem()
    {
        var personId = Guid.NewGuid();
        var profile = new TasteProfile(
            BaseItemKind.Movie,
            DateTime.UtcNow,
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<Guid, float> { [personId] = 5f },
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
            TotalSignalMass: 5f);
        var candidate = new Movie { Id = Guid.NewGuid() };
        var people = new[] { new PersonInfo { Id = personId, Name = "Match", SortOrder = 0, ItemId = candidate.Id } };

        var score = TasteProfileScorer.Score(profile, candidate, seedItem: null, candidatePeople: people);

        Assert.True(score > 0);
    }
}
