using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library.Recommendations;

namespace Emby.Server.Implementations.Library.Recommendations;

/// <summary>
/// Pure function that scores a candidate item against a <see cref="TasteProfile"/>,
/// optionally biased toward a seed item.
/// </summary>
public static class TasteProfileScorer
{
    /// <summary>The additive bonus per shared genre between candidate and seed.</summary>
    internal const float SeedGenreBonus = 5.0f;

    /// <summary>The additive bonus per shared tag between candidate and seed.</summary>
    internal const float SeedTagBonus = 2.0f;

    /// <summary>The additive bonus per shared person between candidate and seed.</summary>
    internal const float SeedPersonBonus = 1.0f;

    /// <summary>The additive bonus per shared studio between candidate and seed.</summary>
    internal const float SeedStudioBonus = 0.5f;

    /// <summary>Maximum number of people considered per candidate (top-N by ascending SortOrder).</summary>
    internal const int MaxPeoplePerCandidate = 5;

    private const float NormalizationEpsilon = 1.0f;

    /// <summary>
    /// Score a candidate item against the user's taste profile.
    /// </summary>
    /// <param name="profile">The user's pre-built taste profile.</param>
    /// <param name="candidate">The candidate item to score.</param>
    /// <param name="seedItem">
    /// An optional seed item used to apply a "Because you watched X" bonus.
    /// Pass <see langword="null"/> when no seed is available.
    /// </param>
    /// <param name="candidatePeople">
    /// Pre-fetched people for the candidate item. The scorer reads people from
    /// this parameter rather than from <paramref name="candidate"/> to avoid
    /// triggering lazy-load calls into the item repository.
    /// </param>
    /// <returns>A non-negative relevance score; higher means more relevant.</returns>
    public static float Score(
        TasteProfile profile,
        BaseItem candidate,
        BaseItem? seedItem,
        IReadOnlyList<PersonInfo> candidatePeople)
    {
        var score = 0f;

        foreach (var g in candidate.Genres ?? Array.Empty<string>())
        {
            if (profile.Genres.TryGetValue(g, out var w))
            {
                score += w;
            }
        }

        foreach (var t in candidate.Tags ?? Array.Empty<string>())
        {
            if (profile.Tags.TryGetValue(t, out var w))
            {
                score += w;
            }
        }

        foreach (var s in candidate.Studios ?? Array.Empty<string>())
        {
            if (profile.Studios.TryGetValue(s, out var w))
            {
                score += w;
            }
        }

        foreach (var p in candidatePeople
            .OrderBy(p => p.SortOrder ?? int.MaxValue)
            .Take(MaxPeoplePerCandidate))
        {
            if (profile.People.TryGetValue(p.Id, out var w))
            {
                score += w;
            }
        }

        if (seedItem is not null)
        {
            score += SeedGenreBonus * CountOverlap(candidate.Genres, seedItem.Genres);
            score += SeedTagBonus * CountOverlap(candidate.Tags, seedItem.Tags);
            score += SeedStudioBonus * CountOverlap(candidate.Studios, seedItem.Studios);

            // People overlap with the seed is not computed here — it would require
            // the seed's people too, doubling the lookup. We let the profile-level
            // people score capture that signal sufficiently.
        }

        return score / (profile.TotalSignalMass + NormalizationEpsilon);
    }

    private static int CountOverlap(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (a is null || b is null || a.Count == 0 || b.Count == 0)
        {
            return 0;
        }

        var set = new HashSet<string>(b, StringComparer.OrdinalIgnoreCase);
        var count = 0;
        foreach (var x in a)
        {
            if (set.Contains(x))
            {
                count++;
            }
        }

        return count;
    }
}
