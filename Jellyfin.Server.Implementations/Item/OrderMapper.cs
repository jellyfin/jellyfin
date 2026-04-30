#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Linq;
using System.Linq.Expressions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Static class for methods which maps types of ordering to their respecting ordering functions.
/// </summary>
public static class OrderMapper
{
    /// <summary>
    /// Creates Func to be executed later with a given BaseItemEntity input for sorting items on query.
    /// </summary>
    /// <param name="sortBy">Item property to sort by.</param>
    /// <param name="query">Context Query.</param>
    /// <param name="jellyfinDbContext">Context.</param>
    /// <returns>Func to be executed later for sorting query.</returns>
    public static Expression<Func<BaseItemEntity, object?>> MapOrderByField(ItemSortBy sortBy, InternalItemsQuery query, JellyfinDbContext jellyfinDbContext)
    {
        return (sortBy, query.User) switch
        {
            (ItemSortBy.AirTime, _) => e => e.SortName, // TODO
            (ItemSortBy.Runtime, _) => e => e.RunTimeTicks,
            (ItemSortBy.Random, _) => e => EF.Functions.Random(),
            (ItemSortBy.DatePlayed, _) => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.LastPlayedDate,
            (ItemSortBy.PlayCount, _) => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.PlayCount,
            (ItemSortBy.IsFavoriteOrLiked, _) => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.IsFavorite,
            (ItemSortBy.IsFolder, _) => e => e.IsFolder,
            (ItemSortBy.IsPlayed, _) => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.Played,
            (ItemSortBy.IsUnplayed, _) => e => !e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.Played,
            (ItemSortBy.DateLastContentAdded, _) => e => e.DateLastMediaAdded,
            (ItemSortBy.Artist, _) => e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.Artist).Select(f => f.ItemValue.CleanValue).FirstOrDefault(),
            (ItemSortBy.AlbumArtist, _) => e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.AlbumArtist).Select(f => f.ItemValue.CleanValue).FirstOrDefault(),
            (ItemSortBy.Studio, _) => e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.Studios).Select(f => f.ItemValue.CleanValue).FirstOrDefault(),
            (ItemSortBy.OfficialRating, _) => e => e.InheritedParentalRatingValue,
            (ItemSortBy.SeriesSortName, _) => e => e.SeriesName,
            (ItemSortBy.Album, _) => e => e.Album,
            (ItemSortBy.DateCreated, _) => e => e.DateCreated,
            (ItemSortBy.PremiereDate, _) => e => (e.PremiereDate ?? (e.ProductionYear.HasValue ? DateTime.MinValue.AddYears(e.ProductionYear.Value - 1) : null)),
            (ItemSortBy.StartDate, _) => e => e.StartDate,
            (ItemSortBy.Name, _) => e => e.CleanName,
            (ItemSortBy.CommunityRating, _) => e => e.CommunityRating,
            (ItemSortBy.ProductionYear, _) => e => e.ProductionYear,
            (ItemSortBy.CriticRating, _) => e => e.CriticRating,
            (ItemSortBy.VideoBitRate, _) => e => e.TotalBitrate,
            (ItemSortBy.ParentIndexNumber, _) => e => e.ParentIndexNumber,
            (ItemSortBy.IndexNumber, _) => e => e.IndexNumber,
            (ItemSortBy.SeriesDatePlayed, not null) => e =>
                            jellyfinDbContext.BaseItems
                                .Where(w => w.SeriesPresentationUniqueKey == e.PresentationUniqueKey)
                                .Join(jellyfinDbContext.UserData.Where(w => w.UserId == query.User.Id && w.Played), f => f.Id, f => f.ItemId, (item, userData) => userData.LastPlayedDate)
                                .Max(f => f),
            (ItemSortBy.SeriesDatePlayed, null) => e => jellyfinDbContext.BaseItems.Where(w => w.SeriesPresentationUniqueKey == e.PresentationUniqueKey)
                                .Join(jellyfinDbContext.UserData.Where(w => w.Played), f => f.Id, f => f.ItemId, (item, userData) => userData.LastPlayedDate)
                                .Max(f => f),
            // ItemSortBy.SeriesDatePlayed => e => jellyfinDbContext.UserData
            //     .Where(u => u.Item!.SeriesPresentationUniqueKey == e.PresentationUniqueKey && u.Played)
            //     .Max(f => f.LastPlayedDate),
            // ItemSortBy.AiredEpisodeOrder => "AiredEpisodeOrder",
            _ => e => e.SortName
        };
    }

    /// <summary>
    /// Creates an expression to order search results by match quality.
    /// Prioritizes: exact match (0) > prefix match with word boundary (1) > prefix match (2) > contains (3).
    /// </summary>
    /// <param name="searchTerm">The search term to match against.</param>
    /// <returns>An expression that returns an integer representing match quality (lower is better).</returns>
    public static Expression<Func<BaseItemEntity, int>> MapSearchRelevanceOrder(string searchTerm)
    {
        var cleanSearchTerm = GetCleanValue(searchTerm);
        var searchPrefix = cleanSearchTerm + " ";
        return e =>
            e.CleanName == cleanSearchTerm ? 0 :
            e.CleanName!.StartsWith(searchPrefix) ? 1 :
            e.CleanName!.StartsWith(cleanSearchTerm) ? 2 : 3;
    }

    private static string GetCleanValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return value.RemoveDiacritics().ToLowerInvariant();
    }
}
