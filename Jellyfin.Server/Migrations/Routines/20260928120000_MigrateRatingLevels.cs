using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migrate rating levels.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
[JellyfinMigration("2026-09-28T12:00:00", nameof(MigrateRatingLevels))]
[JellyfinMigrationBackup(JellyfinDb = true)]
#pragma warning restore CS0618 // Type or member is obsolete
internal class MigrateRatingLevels : IDatabaseMigrationRoutine
{
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";
    private const string SeasonType = "MediaBrowser.Controller.Entities.TV.Season";
    private const string AggregateFolderType = "MediaBrowser.Controller.Entities.AggregateFolder";
    private const int UpdateBatchSize = 1000;

    private readonly IStartupLogger _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _provider;
    private readonly ILocalizationManager _localizationManager;
    private readonly ILibraryManager _libraryManager;

    public MigrateRatingLevels(
        IDbContextFactory<JellyfinDbContext> provider,
        IStartupLogger<MigrateRatingLevels> logger,
        ILocalizationManager localizationManager,
        ILibraryManager libraryManager)
    {
        _provider = provider;
        _localizationManager = localizationManager;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        _logger.LogInformation("Recalculating parental rating levels based on rating string.");
        using var context = _provider.CreateDbContext();
        using var transaction = context.Database.BeginTransaction();

        var items = context.BaseItems
            .AsNoTracking()
            .Select(e => new RatingNode(
                e.Id,
                e.Type,
                e.ParentId,
                e.OwnerId,
                e.SeasonId,
                e.SeriesId,
                e.OfficialRating,
                e.CustomRating,
                e.Path,
                e.PreferredMetadataCountryCode,
                e.InheritedParentalRatingValue,
                e.InheritedParentalRatingSubValue))
            .ToDictionary(e => e.Id);

        var libraries = GetLibraries(items);
        var scores = new Dictionary<(string Rating, string? CountryCode), ParentalRatingScore?>();
        var changes = new Dictionary<(int? Score, int? SubScore), List<Guid>>();
        foreach (var item in items.Values)
        {
            // Mirrors BaseItem.OnMetadataChanged: a custom rating anywhere up the display parent chain
            // wins over the official one, and items without a rating of their own inherit their parent's.
            var rating = ResolveRating(items, item, static e => e.CustomRating)
                ?? ResolveRating(items, item, static e => e.OfficialRating);

            ParentalRatingScore? ratingScore = null;
            if (rating is not null)
            {
                var scoreKey = (Rating: rating, CountryCode: ResolveCountryCode(items, libraries, item));
                if (!scores.TryGetValue(scoreKey, out ratingScore))
                {
                    ratingScore = _localizationManager.GetRatingScore(scoreKey.Rating, scoreKey.CountryCode);
                    scores[scoreKey] = ratingScore;
                }
            }

            var key = (ratingScore?.Score, ratingScore?.SubScore);
            if (key.Score == item.InheritedParentalRatingValue && key.SubScore == item.InheritedParentalRatingSubValue)
            {
                continue;
            }

            if (!changes.TryGetValue(key, out var ids))
            {
                ids = [];
                changes[key] = ids;
            }

            ids.Add(item.Id);
        }

        foreach (var ((score, subScore), ids) in changes)
        {
            foreach (var batch in ids.Chunk(UpdateBatchSize))
            {
                context.BaseItems
                    .Where(e => batch.Contains(e.Id))
                    .ExecuteUpdate(f => f
                        .SetProperty(e => e.InheritedParentalRatingValue, score)
                        .SetProperty(e => e.InheritedParentalRatingSubValue, subScore));
            }
        }

        transaction.Commit();
        _logger.LogInformation("Updated the parental rating level of {Count} items.", changes.Values.Sum(e => e.Count));
    }

    private List<Library> GetLibraries(Dictionary<Guid, RatingNode> items)
    {
        var libraries = new List<Library>();
        foreach (var virtualFolder in _libraryManager.GetVirtualFolders(false))
        {
            RatingNode? folder = null;
            if (Guid.TryParse(virtualFolder.ItemId, out var folderId))
            {
                items.TryGetValue(folderId, out folder);
            }

            libraries.Add(new Library(
                folder?.Path,
                virtualFolder.Locations ?? [],
                folder?.PreferredMetadataCountryCode,
                virtualFolder.LibraryOptions?.MetadataCountryCode));
        }

        return libraries;
    }

    // Mirrors BaseItem.GetPreferredMetadataCountryCode; a null result falls back to the server's country.
    private static string? ResolveCountryCode(Dictionary<Guid, RatingNode> items, List<Library> libraries, RatingNode item)
    {
        var visited = new HashSet<Guid>();
        for (RatingNode? current = item; current is not null && visited.Add(current.Id); current = GetItem(items, current.ParentId))
        {
            if (!string.IsNullOrEmpty(current.PreferredMetadataCountryCode))
            {
                return current.PreferredMetadataCountryCode;
            }
        }

        // Like LibraryManager.GetCollectionFolders: the item below the root decides which libraries it is in.
        visited.Clear();
        var top = item;
        while (visited.Add(top.Id))
        {
            var parent = GetItem(items, top.ParentId);
            if (parent?.Type == AggregateFolderType)
            {
                break;
            }

            var next = parent ?? GetItem(items, top.OwnerId);
            if (next is null)
            {
                break;
            }

            top = next;
        }

        var matches = top.Path is null
            ? []
            : libraries.Where(l => string.Equals(l.Path, top.Path, StringComparison.OrdinalIgnoreCase)
                || l.Locations.Contains(top.Path, StringComparer.OrdinalIgnoreCase)).ToList();

        var countryCode = matches.Select(l => l.PreferredMetadataCountryCode).FirstOrDefault(c => !string.IsNullOrEmpty(c))
            ?? matches.FirstOrDefault()?.MetadataCountryCode;

        return string.IsNullOrEmpty(countryCode) ? null : countryCode;
    }

    private static RatingNode? GetItem(Dictionary<Guid, RatingNode> items, Guid? id)
        => id is { } value && items.TryGetValue(value, out var item) ? item : null;

    private static string? ResolveRating(Dictionary<Guid, RatingNode> items, RatingNode item, Func<RatingNode, string?> selector)
    {
        var visited = new HashSet<Guid>();
        RatingNode? current = item;
        while (current is not null && visited.Add(current.Id))
        {
            var rating = selector(current);
            if (!string.IsNullOrEmpty(rating))
            {
                return rating;
            }

            var parentId = current.Type switch
            {
                EpisodeType => current.SeasonId,
                SeasonType => current.SeriesId,
                _ => current.ParentId
            };

            current = GetItem(items, parentId);
        }

        return null;
    }

    private sealed record RatingNode(
        Guid Id,
        string Type,
        Guid? ParentId,
        Guid? OwnerId,
        Guid? SeasonId,
        Guid? SeriesId,
        string? OfficialRating,
        string? CustomRating,
        string? Path,
        string? PreferredMetadataCountryCode,
        int? InheritedParentalRatingValue,
        int? InheritedParentalRatingSubValue);

    private sealed record Library(string? Path, string[] Locations, string? PreferredMetadataCountryCode, string? MetadataCountryCode);
}
