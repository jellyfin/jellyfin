using System;
using System.Linq;
using System.Linq.Expressions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
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
    /// <returns>Func to be executed later for sorting query.</returns>
    public static Expression<Func<BaseItemEntity, object?>> MapOrderByField(ItemSortBy sortBy, InternalItemsQuery query)
    {
        return sortBy switch
        {
            ItemSortBy.AirTime => e => e.SortName, // TODO
            ItemSortBy.Runtime => e => e.RunTimeTicks,
            ItemSortBy.Random => e => EF.Functions.Random(),
            ItemSortBy.DatePlayed => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.LastPlayedDate,
            ItemSortBy.PlayCount => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.PlayCount,
            ItemSortBy.IsFavoriteOrLiked => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.IsFavorite,
            ItemSortBy.IsFolder => e => e.IsFolder,
            ItemSortBy.IsPlayed => e => e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.Played,
            ItemSortBy.IsUnplayed => e => !e.UserData!.FirstOrDefault(f => f.UserId.Equals(query.User!.Id))!.Played,
            ItemSortBy.DateLastContentAdded => e => e.DateLastMediaAdded,
            ItemSortBy.Artist => e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.Artist).Select(f => f.ItemValue.CleanValue).FirstOrDefault(),
            ItemSortBy.AlbumArtist => e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.AlbumArtist).Select(f => f.ItemValue.CleanValue).FirstOrDefault(),
            ItemSortBy.Studio => e => e.ItemValues!.Where(f => f.ItemValue.Type == ItemValueType.Studios).Select(f => f.ItemValue.CleanValue).FirstOrDefault(),
            ItemSortBy.OfficialRating => e => e.InheritedParentalRatingValue,
            // ItemSortBy.SeriesDatePlayed => "(Select MAX(LastPlayedDate) from TypedBaseItems B" + GetJoinUserDataText(query) + " where Played=1 and B.SeriesPresentationUniqueKey=A.PresentationUniqueKey)",
            ItemSortBy.SeriesSortName => e => e.SeriesName,
            // ItemSortBy.AiredEpisodeOrder => "AiredEpisodeOrder",
            ItemSortBy.Album => e => e.Album,
            ItemSortBy.DateCreated => e => e.DateCreated,
            ItemSortBy.PremiereDate => e => (e.PremiereDate ?? (e.ProductionYear.HasValue ? DateTime.MinValue.AddYears(e.ProductionYear.Value - 1) : null)),
            ItemSortBy.StartDate => e => e.StartDate,
            ItemSortBy.Name => e => e.CleanName,
            ItemSortBy.CommunityRating => e => e.CommunityRating,
            ItemSortBy.ProductionYear => e => e.ProductionYear,
            ItemSortBy.CriticRating => e => e.CriticRating,
            ItemSortBy.VideoBitRate => e => e.TotalBitrate,
            ItemSortBy.ParentIndexNumber => e => e.ParentIndexNumber,
            ItemSortBy.IndexNumber => e => e.IndexNumber,
            _ => e => e.SortName
        };
    }
}
