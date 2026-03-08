#pragma warning disable RS0030 // Do not use banned APIs
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1309 // Use ordinal string comparison
#pragma warning disable CA1311 // Specify a culture or use an invariant version
#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.MatchCriteria;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using BaseItemEntity = Jellyfin.Database.Implementations.Entities.BaseItemEntity;

namespace Jellyfin.Server.Implementations.Item;

public sealed partial class BaseItemRepository
{
    private static readonly IReadOnlyList<char> SearchWildcardTerms = ['%', '_', '[', ']', '^'];

    private static readonly string ImdbProviderName = MetadataProvider.Imdb.ToString().ToLowerInvariant();
    private static readonly string TmdbProviderName = MetadataProvider.Tmdb.ToString().ToLowerInvariant();
    private static readonly string TvdbProviderName = MetadataProvider.Tvdb.ToString().ToLowerInvariant();

    /// <inheritdoc />
    public IQueryable<BaseItemEntity> TranslateQuery(
        IQueryable<BaseItemEntity> baseQuery,
        JellyfinDbContext context,
        InternalItemsQuery filter)
    {
        const int HDWidth = 1200;
        const int UHDWidth = 3800;
        const int UHDHeight = 2100;

        var minWidth = filter.MinWidth;
        var maxWidth = filter.MaxWidth;
        var now = DateTime.UtcNow;

        if (filter.IsHD.HasValue || filter.Is4K.HasValue)
        {
            bool includeSD = false;
            bool includeHD = false;
            bool include4K = false;

            if (filter.IsHD.HasValue && !filter.IsHD.Value)
            {
                includeSD = true;
            }

            if (filter.IsHD.HasValue && filter.IsHD.Value)
            {
                includeHD = true;
            }

            if (filter.Is4K.HasValue && filter.Is4K.Value)
            {
                include4K = true;
            }

            baseQuery = baseQuery.Where(e =>
                (includeSD && e.Width < HDWidth) ||
                (includeHD && e.Width >= HDWidth && !(e.Width >= UHDWidth || e.Height >= UHDHeight)) ||
                (include4K && (e.Width >= UHDWidth || e.Height >= UHDHeight)));
        }

        if (minWidth.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Width >= minWidth);
        }

        if (filter.MinHeight.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Height >= filter.MinHeight);
        }

        if (maxWidth.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Width <= maxWidth);
        }

        if (filter.MaxHeight.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.Height <= filter.MaxHeight);
        }

        if (filter.IsLocked.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsLocked == filter.IsLocked);
        }

        var tags = filter.Tags.ToList();
        var excludeTags = filter.ExcludeTags.ToList();

        if (filter.IsMovie.HasValue)
        {
            var shouldIncludeAllMovieTypes = filter.IsMovie.Value
                && (filter.IncludeItemTypes.Length == 0
                    || filter.IncludeItemTypes.Contains(BaseItemKind.Movie)
                    || filter.IncludeItemTypes.Contains(BaseItemKind.Trailer));

            if (!shouldIncludeAllMovieTypes)
            {
                baseQuery = baseQuery.Where(e => e.IsMovie == filter.IsMovie.Value);
            }
        }

        if (filter.IsSeries.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsSeries == filter.IsSeries);
        }

        if (filter.IsSports.HasValue)
        {
            if (filter.IsSports.Value)
            {
                tags.Add("Sports");
            }
            else
            {
                excludeTags.Add("Sports");
            }
        }

        if (filter.IsNews.HasValue)
        {
            if (filter.IsNews.Value)
            {
                tags.Add("News");
            }
            else
            {
                excludeTags.Add("News");
            }
        }

        if (filter.IsKids.HasValue)
        {
            if (filter.IsKids.Value)
            {
                tags.Add("Kids");
            }
            else
            {
                excludeTags.Add("Kids");
            }
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var cleanedSearchTerm = filter.SearchTerm.GetCleanValue();
            var originalSearchTerm = filter.SearchTerm;
            if (SearchWildcardTerms.Any(f => cleanedSearchTerm.Contains(f)))
            {
                cleanedSearchTerm = $"%{cleanedSearchTerm.Trim('%')}%";
                var likeSearchTerm = $"%{originalSearchTerm.Trim('%')}%";
                baseQuery = baseQuery.Where(e => EF.Functions.Like(e.CleanName!, cleanedSearchTerm) || (e.OriginalTitle != null && EF.Functions.Like(e.OriginalTitle, likeSearchTerm)));
            }
            else
            {
                var likeSearchTerm = $"%{originalSearchTerm}%";
                baseQuery = baseQuery.Where(e => e.CleanName!.Contains(cleanedSearchTerm) || (e.OriginalTitle != null && EF.Functions.Like(e.OriginalTitle, likeSearchTerm)));
            }
        }

        if (filter.IsFolder.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IsFolder == filter.IsFolder);
        }

        var includeTypes = filter.IncludeItemTypes;

        // Only specify excluded types if no included types are specified
        if (filter.IncludeItemTypes.Length == 0)
        {
            var excludeTypes = filter.ExcludeItemTypes;
            if (excludeTypes.Length == 1)
            {
                if (_itemTypeLookup.BaseItemKindNames.TryGetValue(excludeTypes[0], out var excludeTypeName))
                {
                    baseQuery = baseQuery.Where(e => e.Type != excludeTypeName);
                }
            }
            else if (excludeTypes.Length > 1)
            {
                var excludeTypeName = new List<string>();
                foreach (var excludeType in excludeTypes)
                {
                    if (_itemTypeLookup.BaseItemKindNames.TryGetValue(excludeType, out var baseItemKindName))
                    {
                        excludeTypeName.Add(baseItemKindName!);
                    }
                }

                baseQuery = baseQuery.Where(e => !excludeTypeName.Contains(e.Type));
            }
        }
        else
        {
            string[] types = includeTypes.Select(f => _itemTypeLookup.BaseItemKindNames.GetValueOrDefault(f)).Where(e => e != null).ToArray()!;
            baseQuery = baseQuery.WhereOneOrMany(types, f => f.Type);
        }

        if (filter.ChannelIds.Count > 0)
        {
            baseQuery = baseQuery.Where(e => e.ChannelId != null && filter.ChannelIds.Contains(e.ChannelId.Value));
        }

        if (!filter.ParentId.IsEmpty())
        {
            baseQuery = baseQuery.Where(e => e.ParentId!.Value == filter.ParentId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Path))
        {
            var pathToQuery = GetPathToSave(filter.Path);
            baseQuery = baseQuery.Where(e => e.Path == pathToQuery);
        }

        if (!string.IsNullOrWhiteSpace(filter.PresentationUniqueKey))
        {
            baseQuery = baseQuery.Where(e => e.PresentationUniqueKey == filter.PresentationUniqueKey);
        }

        if (filter.MinCommunityRating.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.CommunityRating >= filter.MinCommunityRating);
        }

        if (filter.MinIndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IndexNumber >= filter.MinIndexNumber);
        }

        if (filter.MinParentAndIndexNumber.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => (e.ParentIndexNumber == filter.MinParentAndIndexNumber.Value.ParentIndexNumber && e.IndexNumber >= filter.MinParentAndIndexNumber.Value.IndexNumber) || e.ParentIndexNumber > filter.MinParentAndIndexNumber.Value.ParentIndexNumber);
        }

        if (filter.MinDateCreated.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateCreated >= filter.MinDateCreated);
        }

        if (filter.MinDateLastSaved.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateLastSaved != null && e.DateLastSaved >= filter.MinDateLastSaved.Value);
        }

        if (filter.MinDateLastSavedForUser.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.DateLastSaved != null && e.DateLastSaved >= filter.MinDateLastSavedForUser.Value);
        }

        if (filter.IndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.IndexNumber == filter.IndexNumber.Value);
        }

        if (filter.ParentIndexNumber.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.ParentIndexNumber == filter.ParentIndexNumber.Value);
        }

        if (filter.ParentIndexNumberNotEquals.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.ParentIndexNumber != filter.ParentIndexNumberNotEquals.Value || e.ParentIndexNumber == null);
        }

        var minEndDate = filter.MinEndDate;
        var maxEndDate = filter.MaxEndDate;

        if (filter.HasAired.HasValue)
        {
            if (filter.HasAired.Value)
            {
                maxEndDate = DateTime.UtcNow;
            }
            else
            {
                minEndDate = DateTime.UtcNow;
            }
        }

        if (minEndDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.EndDate >= minEndDate);
        }

        if (maxEndDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.EndDate <= maxEndDate);
        }

        if (filter.MinStartDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.StartDate >= filter.MinStartDate.Value);
        }

        if (filter.MaxStartDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.StartDate <= filter.MaxStartDate.Value);
        }

        if (filter.MinPremiereDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.PremiereDate >= filter.MinPremiereDate.Value);
        }

        if (filter.MaxPremiereDate.HasValue)
        {
            baseQuery = baseQuery.Where(e => e.PremiereDate <= filter.MaxPremiereDate.Value);
        }

        if (filter.TrailerTypes.Length > 0)
        {
            var trailerTypes = filter.TrailerTypes.Select(e => (int)e).ToArray();
            baseQuery = baseQuery.Where(e => e.TrailerTypes!.Any(w => trailerTypes.Contains(w.Id)));
        }

        if (filter.IsAiring.HasValue)
        {
            if (filter.IsAiring.Value)
            {
                baseQuery = baseQuery.Where(e => e.StartDate <= now && e.EndDate >= now);
            }
            else
            {
                baseQuery = baseQuery.Where(e => e.StartDate > now && e.EndDate < now);
            }
        }

        if (filter.PersonIds.Length > 0)
        {
            var peopleEntityIds = context.BaseItems
                .WhereOneOrMany(filter.PersonIds, b => b.Id)
                .Join(
                    context.Peoples,
                    b => b.Name,
                    p => p.Name,
                    (b, p) => p.Id);

            baseQuery = baseQuery
                .Where(e => context.PeopleBaseItemMap
                    .Any(m => m.ItemId == e.Id && peopleEntityIds.Contains(m.PeopleId)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Person))
        {
            baseQuery = baseQuery.Where(e => e.Peoples!.Any(f => f.People.Name == filter.Person));
        }

        if (!string.IsNullOrWhiteSpace(filter.MinSortName))
        {
            // this does not makes sense.
            // baseQuery = baseQuery.Where(e => e.SortName >= query.MinSortName);
            // whereClauses.Add("SortName>=@MinSortName");
            // statement?.TryBind("@MinSortName", query.MinSortName);
        }

        if (!string.IsNullOrWhiteSpace(filter.ExternalSeriesId))
        {
            baseQuery = baseQuery.Where(e => e.ExternalSeriesId == filter.ExternalSeriesId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ExternalId))
        {
            baseQuery = baseQuery.Where(e => e.ExternalId == filter.ExternalId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            if (filter.UseRawName == true)
            {
                baseQuery = baseQuery.Where(e => e.Name == filter.Name);
            }
            else
            {
                var cleanName = filter.Name.GetCleanValue();
                baseQuery = baseQuery.Where(e => e.CleanName == cleanName);
            }
        }

        // These are the same, for now
        var nameContains = filter.NameContains;
        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            if (SearchWildcardTerms.Any(f => nameContains.Contains(f)))
            {
                nameContains = $"%{nameContains.Trim('%')}%";
                baseQuery = baseQuery.Where(e => EF.Functions.Like(e.CleanName, nameContains) || EF.Functions.Like(e.OriginalTitle, nameContains));
            }
            else
            {
                var likeNameContains = $"%{nameContains}%";
                baseQuery = baseQuery.Where(e =>
                                    e.CleanName!.Contains(nameContains)
                                    || EF.Functions.Like(e.OriginalTitle, likeNameContains));
            }
        }

        // When box set collapsing is active, defer name-range filters to after the collapse.
        // Otherwise, items are filtered by their own name but then collapsed into a BoxSet
        // whose name may fall in a different range (e.g. "21 Jump Street" is under "#"
        // but its BoxSet "Jump Street Collection" should appear under "J").
        if (filter.CollapseBoxSetItems != true)
        {
            baseQuery = ApplyNameFilters(baseQuery, filter);
        }

        if (filter.ImageTypes.Length > 0)
        {
            var imgTypes = filter.ImageTypes.Select(e => (ImageInfoImageType)e).ToArray();
            baseQuery = baseQuery.Where(e => e.Images!.Any(w => imgTypes.Contains(w.ImageType)));
        }

        if (filter.IsLiked.HasValue)
        {
            var isLiked = filter.IsLiked.Value;
            baseQuery = baseQuery.Where(e => e.UserData!.Any(ud => ud.UserId == filter.User!.Id && ud.Rating >= UserItemData.MinLikeValue) == isLiked);
        }

        if (filter.IsFavoriteOrLiked.HasValue)
        {
            var isFavoriteOrLiked = filter.IsFavoriteOrLiked.Value;
            baseQuery = baseQuery.Where(e => e.UserData!.Any(ud => ud.UserId == filter.User!.Id && ud.IsFavorite) == isFavoriteOrLiked);
        }

        if (filter.IsFavorite.HasValue)
        {
            var isFavorite = filter.IsFavorite.Value;
            baseQuery = baseQuery.Where(e => e.UserData!.Any(ud => ud.UserId == filter.User!.Id && ud.IsFavorite) == isFavorite);
        }

        if (filter.IsPlayed.HasValue)
        {
            // We should probably figure this out for all folders, but for right now, this is the only place where we need it
            if (filter.IncludeItemTypes.Length == 1 && filter.IncludeItemTypes[0] == BaseItemKind.Series)
            {
                // Get played series IDs by joining episodes to UserData via SeriesId (Guid foreign key).
                // Don't filter episodes by TopParentIds here - the series will be filtered by baseQuery anyway.
                // This allows the materialized list to be reused across library-scoped queries.
                var playedSeriesIdList = context.BaseItems
                    .Where(e => !e.IsFolder && !e.IsVirtualItem && e.SeriesId.HasValue)
                    .Join(
                        context.UserData.Where(ud => ud.UserId == filter.User!.Id && ud.Played),
                        episode => episode.Id,
                        ud => ud.ItemId,
                        (episode, ud) => episode.SeriesId!.Value)
                    .Distinct();

                var isPlayed = filter.IsPlayed.Value;
                baseQuery = baseQuery.Where(s => playedSeriesIdList.Contains(s.Id) == isPlayed);
            }
            else if (filter.IncludeItemTypes.Length == 1 && filter.IncludeItemTypes[0] == BaseItemKind.BoxSet)
            {
                var boxSetIds = baseQuery.Select(e => e.Id).ToList();
                var playedCounts = GetPlayedAndTotalCountBatch(boxSetIds, filter.User!);
                var playedBoxSetIds = playedCounts
                    .Where(kvp => kvp.Value.Total > 0 && kvp.Value.Played == kvp.Value.Total)
                    .Select(kvp => kvp.Key);

                var isPlayedBoxSet = filter.IsPlayed.Value;
                baseQuery = baseQuery.Where(s => playedBoxSetIds.Contains(s.Id) == isPlayedBoxSet);
            }
            else
            {
                var playedItemIds = context.UserData
                    .Where(ud => ud.UserId == filter.User!.Id && ud.Played)
                    .Select(ud => ud.ItemId);
                var isPlayedItem = filter.IsPlayed.Value;
                baseQuery = baseQuery.Where(e => playedItemIds.Contains(e.Id) == isPlayedItem);
            }
        }

        if (filter.IsResumable.HasValue)
        {
            var resumableItemIds = context.UserData
                .Where(ud => ud.UserId == filter.User!.Id && ud.PlaybackPositionTicks > 0)
                .Select(ud => ud.ItemId);
            var isResumable = filter.IsResumable.Value;
            baseQuery = baseQuery.Where(e => resumableItemIds.Contains(e.Id) == isResumable);
        }

        if (filter.ArtistIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItemMultipleTypes(context, [ItemValueType.Artist, ItemValueType.AlbumArtist], filter.ArtistIds);
        }

        if (filter.AlbumArtistIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.AlbumArtist, filter.AlbumArtistIds);
        }

        if (filter.ContributingArtistIds.Length > 0)
        {
            var contributingNames = context.BaseItems
                .Where(b => filter.ContributingArtistIds.Contains(b.Id))
                .Select(b => b.CleanName);

            baseQuery = baseQuery.Where(e =>
                e.ItemValues!.Any(ivm =>
                    ivm.ItemValue.Type == ItemValueType.Artist &&
                    contributingNames.Contains(ivm.ItemValue.CleanValue))
                &&
                !e.ItemValues!.Any(ivm =>
                    ivm.ItemValue.Type == ItemValueType.AlbumArtist &&
                    contributingNames.Contains(ivm.ItemValue.CleanValue)));
        }

        if (filter.AlbumIds.Length > 0)
        {
            var subQuery = context.BaseItems.WhereOneOrMany(filter.AlbumIds, f => f.Id);
            baseQuery = baseQuery.Where(e => subQuery.Any(f => f.Name == e.Album));
        }

        if (filter.ExcludeArtistIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItemMultipleTypes(context, [ItemValueType.Artist, ItemValueType.AlbumArtist], filter.ExcludeArtistIds, true);
        }

        if (filter.GenreIds.Count > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.Genre, filter.GenreIds.ToArray());
        }

        if (filter.Genres.Count > 0)
        {
            var cleanGenres = filter.Genres.Select(e => e.GetCleanValue()).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Genre).Any(cleanGenres));
        }

        if (tags.Count > 0)
        {
            var cleanValues = tags.Select(e => e.GetCleanValue()).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Tags).Any(cleanValues));
        }

        if (excludeTags.Count > 0)
        {
            var cleanValues = excludeTags.Select(e => e.GetCleanValue()).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => !e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Tags).Any(cleanValues));
        }

        if (filter.StudioIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.Studios, filter.StudioIds.ToArray());
        }

        if (filter.OfficialRatings.Length > 0)
        {
            var ratings = filter.OfficialRatings;
            baseQuery = baseQuery.WhereItemOrDescendantMatches(context, e => ratings.Contains(e.OfficialRating));
        }

        Expression<Func<BaseItemEntity, bool>>? minParentalRatingFilter = null;
        if (filter.MinParentalRating != null)
        {
            var min = filter.MinParentalRating;
            var minScore = min.Score;
            var minSubScore = min.SubScore ?? 0;

            minParentalRatingFilter = e =>
                e.InheritedParentalRatingValue == null ||
                e.InheritedParentalRatingValue > minScore ||
                (e.InheritedParentalRatingValue == minScore && (e.InheritedParentalRatingSubValue ?? 0) >= minSubScore);
        }

        Expression<Func<BaseItemEntity, bool>>? maxParentalRatingFilter = null;
        if (filter.MaxParentalRating != null)
        {
            maxParentalRatingFilter = BuildMaxParentalRatingFilter(context, filter.MaxParentalRating);
        }

        if (filter.HasParentalRating ?? false)
        {
            if (minParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(minParentalRatingFilter);
            }

            if (maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(maxParentalRatingFilter);
            }
        }
        else if (filter.BlockUnratedItems.Length > 0)
        {
            var unratedItemTypes = filter.BlockUnratedItems.Select(f => f.ToString()).ToArray();
            Expression<Func<BaseItemEntity, bool>> unratedItemFilter = e => e.InheritedParentalRatingValue != null || !unratedItemTypes.Contains(e.UnratedType);

            if (minParentalRatingFilter != null && maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(unratedItemFilter.And(minParentalRatingFilter.And(maxParentalRatingFilter)));
            }
            else if (minParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(unratedItemFilter.And(minParentalRatingFilter));
            }
            else if (maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(unratedItemFilter.And(maxParentalRatingFilter));
            }
            else
            {
                baseQuery = baseQuery.Where(unratedItemFilter);
            }
        }
        else if (minParentalRatingFilter != null || maxParentalRatingFilter != null)
        {
            if (minParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(minParentalRatingFilter);
            }

            if (maxParentalRatingFilter != null)
            {
                baseQuery = baseQuery.Where(maxParentalRatingFilter);
            }
        }
        else if (!filter.HasParentalRating ?? false)
        {
            baseQuery = baseQuery
                .Where(e => e.InheritedParentalRatingValue == null);
        }

        if (filter.HasOfficialRating.HasValue)
        {
            Expression<Func<BaseItemEntity, bool>> hasRating =
                e => e.OfficialRating != null && e.OfficialRating != string.Empty;

            baseQuery = filter.HasOfficialRating.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasRating)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasRating);
        }

        if (filter.HasOverview.HasValue)
        {
            if (filter.HasOverview.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.Overview != null && e.Overview != string.Empty);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.Overview == null || e.Overview == string.Empty);
            }
        }

        if (filter.HasOwnerId.HasValue)
        {
            if (filter.HasOwnerId.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.OwnerId != null);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.OwnerId == null);
            }
        }
        else if (filter.OwnerIds.Length == 0 && filter.ExtraTypes.Length == 0 && !filter.IncludeOwnedItems)
        {
            // Exclude alternate versions from general queries. Alternate versions have
            // OwnerId set (pointing to their primary) but no ExtraType.
            // Extras (trailers, etc.) also have OwnerId but DO have ExtraType set - keep those.
            baseQuery = baseQuery.Where(e => e.OwnerId == null || e.ExtraType != null);
        }

        if (filter.OwnerIds.Length > 0)
        {
            baseQuery = baseQuery.Where(e => e.OwnerId != null && filter.OwnerIds.Contains(e.OwnerId.Value));
        }

        if (filter.ExtraTypes.Length > 0)
        {
            // Convert ExtraType enum to BaseItemExtraType enum via int cast (same underlying values)
            var extraTypeValues = filter.ExtraTypes.Select(e => (BaseItemExtraType?)(int)e).ToArray();
            baseQuery = baseQuery.Where(e => e.ExtraType != null && extraTypeValues.Contains(e.ExtraType));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoAudioTrackWithLanguage))
        {
            var lang = filter.HasNoAudioTrackWithLanguage;
            var foldersWithAudio = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Audio, lang));

            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && !e.MediaStreams!.Any(ms => ms.StreamType == MediaStreamTypeEntity.Audio && ms.Language == lang))
                    || (e.IsFolder && !foldersWithAudio.Contains(e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoInternalSubtitleTrackWithLanguage))
        {
            var lang = filter.HasNoInternalSubtitleTrackWithLanguage;
            var foldersWithSubtitles = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Subtitle, lang, IsExternal: false));

            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && !e.MediaStreams!.Any(ms => ms.StreamType == MediaStreamTypeEntity.Subtitle && !ms.IsExternal && ms.Language == lang))
                    || (e.IsFolder && !foldersWithSubtitles.Contains(e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoExternalSubtitleTrackWithLanguage))
        {
            var lang = filter.HasNoExternalSubtitleTrackWithLanguage;
            var foldersWithSubtitles = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Subtitle, lang, IsExternal: true));

            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && !e.MediaStreams!.Any(ms => ms.StreamType == MediaStreamTypeEntity.Subtitle && ms.IsExternal && ms.Language == lang))
                    || (e.IsFolder && !foldersWithSubtitles.Contains(e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.HasNoSubtitleTrackWithLanguage))
        {
            var lang = filter.HasNoSubtitleTrackWithLanguage;
            var foldersWithSubtitles = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Subtitle, lang));

            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && !e.MediaStreams!.Any(ms => ms.StreamType == MediaStreamTypeEntity.Subtitle && ms.Language == lang))
                    || (e.IsFolder && !foldersWithSubtitles.Contains(e.Id)));
        }

        if (filter.HasSubtitles.HasValue)
        {
            var hasSubtitles = filter.HasSubtitles.Value;
            var foldersWithSubtitles = DescendantQueryHelper.GetFolderIdsMatching(context, new HasSubtitles());
            if (hasSubtitles)
            {
                baseQuery = baseQuery
                    .Where(e =>
                        (!e.IsFolder && e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Subtitle))
                        || (e.IsFolder && foldersWithSubtitles.Contains(e.Id)));
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e =>
                        (!e.IsFolder && !e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Subtitle))
                        || (e.IsFolder && !foldersWithSubtitles.Contains(e.Id)));
            }
        }

        if (filter.HasChapterImages.HasValue)
        {
            var hasChapterImages = filter.HasChapterImages.Value;
            var foldersWithChapterImages = DescendantQueryHelper.GetFolderIdsMatching(context, new HasChapterImages());
            if (hasChapterImages)
            {
                baseQuery = baseQuery
                    .Where(e =>
                        (!e.IsFolder && e.Chapters!.Any(f => f.ImagePath != null))
                        || (e.IsFolder && foldersWithChapterImages.Contains(e.Id)));
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e =>
                        (!e.IsFolder && !e.Chapters!.Any(f => f.ImagePath != null))
                        || (e.IsFolder && !foldersWithChapterImages.Contains(e.Id)));
            }
        }

        if (filter.HasDeadParentId.HasValue && filter.HasDeadParentId.Value)
        {
            baseQuery = baseQuery
                .Where(e => e.ParentId.HasValue && !context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId)).Any(f => f.Id == e.ParentId.Value));
        }

        if (filter.IsDeadArtist.HasValue && filter.IsDeadArtist.Value)
        {
            baseQuery = baseQuery
                    .Where(e => !context.ItemValues.Where(f => _getAllArtistsValueTypes.Contains(f.Type)).Any(f => f.Value == e.Name));
        }

        if (filter.IsDeadStudio.HasValue && filter.IsDeadStudio.Value)
        {
            baseQuery = baseQuery
                    .Where(e => !context.ItemValues.Where(f => _getStudiosValueTypes.Contains(f.Type)).Any(f => f.Value == e.Name));
        }

        if (filter.IsDeadGenre.HasValue && filter.IsDeadGenre.Value)
        {
            baseQuery = baseQuery
                    .Where(e => !context.ItemValues.Where(f => _getGenreValueTypes.Contains(f.Type)).Any(f => f.Value == e.Name));
        }

        if (filter.IsDeadPerson.HasValue && filter.IsDeadPerson.Value)
        {
            baseQuery = baseQuery
                .Where(e => !context.Peoples.Any(f => f.Name == e.Name));
        }

        if (filter.Years.Length > 0)
        {
            baseQuery = baseQuery.WhereOneOrMany(filter.Years, e => e.ProductionYear!.Value);
        }

        var isVirtualItem = filter.IsVirtualItem ?? filter.IsMissing;
        if (isVirtualItem.HasValue)
        {
            baseQuery = baseQuery
                .Where(e => e.IsVirtualItem == isVirtualItem.Value);
        }

        if (filter.IsSpecialSeason.HasValue)
        {
            if (filter.IsSpecialSeason.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.IndexNumber == 0);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.IndexNumber != 0);
            }
        }

        if (filter.IsUnaired.HasValue)
        {
            if (filter.IsUnaired.Value)
            {
                baseQuery = baseQuery
                    .Where(e => e.PremiereDate >= now);
            }
            else
            {
                baseQuery = baseQuery
                    .Where(e => e.PremiereDate < now);
            }
        }

        if (filter.MediaTypes.Length > 0)
        {
            var mediaTypes = filter.MediaTypes.Select(f => f.ToString()).ToArray();
            baseQuery = baseQuery.WhereOneOrMany(mediaTypes, e => e.MediaType);
        }

        if (filter.ItemIds.Length > 0)
        {
            baseQuery = baseQuery.WhereOneOrMany(filter.ItemIds, e => e.Id);
        }

        if (filter.ExcludeItemIds.Length > 0)
        {
            baseQuery = baseQuery
                .Where(e => !filter.ExcludeItemIds.Contains(e.Id));
        }

        if (filter.ExcludeProviderIds is not null && filter.ExcludeProviderIds.Count > 0)
        {
            var exclude = filter.ExcludeProviderIds.Select(e => $"{e.Key}:{e.Value}").ToArray();
            baseQuery = baseQuery.Where(e => e.Provider!.Select(f => f.ProviderId + ":" + f.ProviderValue)!.All(f => !exclude.Contains(f)));
        }

        if (filter.HasAnyProviderId is not null && filter.HasAnyProviderId.Count > 0)
        {
            // Allow setting a null or empty value to get all items that have the specified provider set.
            var includeAny = filter.HasAnyProviderId.Where(e => string.IsNullOrEmpty(e.Value)).Select(e => e.Key).ToArray();
            if (includeAny.Length > 0)
            {
                baseQuery = baseQuery.Where(e => e.Provider!.Any(f => includeAny.Contains(f.ProviderId)));
            }

            var includeSelected = filter.HasAnyProviderId.Where(e => !string.IsNullOrEmpty(e.Value)).Select(e => $"{e.Key}:{e.Value}").ToArray();
            if (includeSelected.Length > 0)
            {
                baseQuery = baseQuery.Where(e => e.Provider!.Select(f => f.ProviderId + ":" + f.ProviderValue)!.Any(f => includeSelected.Contains(f)));
            }
        }

        if (filter.HasImdbId.HasValue)
        {
            baseQuery = filter.HasImdbId.Value
                ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == ImdbProviderName))
                : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != ImdbProviderName));
        }

        if (filter.HasTmdbId.HasValue)
        {
            baseQuery = filter.HasTmdbId.Value
                ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == TmdbProviderName))
                : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != TmdbProviderName));
        }

        if (filter.HasTvdbId.HasValue)
        {
            baseQuery = filter.HasTvdbId.Value
                ? baseQuery.Where(e => e.Provider!.Any(f => f.ProviderId.ToLower() == TvdbProviderName))
                : baseQuery.Where(e => e.Provider!.All(f => f.ProviderId.ToLower() != TvdbProviderName));
        }

        var queryTopParentIds = filter.TopParentIds;

        if (queryTopParentIds.Length > 0)
        {
            var includedItemByNameTypes = GetItemByNameTypesInQuery(filter);
            var enableItemsByName = (filter.IncludeItemsByName ?? false) && includedItemByNameTypes.Count > 0;
            if (enableItemsByName && includedItemByNameTypes.Count > 0)
            {
                baseQuery = baseQuery.Where(e => includedItemByNameTypes.Contains(e.Type) || queryTopParentIds.Any(w => w == e.TopParentId!.Value));
            }
            else
            {
                baseQuery = baseQuery.WhereOneOrMany(queryTopParentIds, e => e.TopParentId!.Value);
            }
        }

        if (filter.AncestorIds.Length > 0)
        {
            var ancestorFilter = filter.AncestorIds.OneOrManyExpressionBuilder<AncestorId, Guid>(f => f.ParentItemId);
            baseQuery = baseQuery.Where(e => e.Parents!.AsQueryable().Any(ancestorFilter));
        }

        if (!string.IsNullOrWhiteSpace(filter.AncestorWithPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => context.BaseItems.Where(e => e.Id != EF.Constant(PlaceholderId)).Where(f => f.PresentationUniqueKey == filter.AncestorWithPresentationUniqueKey).Any(f => f.Children!.Any(w => w.ItemId == e.Id)));
        }

        if (!string.IsNullOrWhiteSpace(filter.SeriesPresentationUniqueKey))
        {
            baseQuery = baseQuery
                .Where(e => e.SeriesPresentationUniqueKey == filter.SeriesPresentationUniqueKey);
        }

        if (filter.ExcludeInheritedTags.Length > 0)
        {
            var excludedTags = filter.ExcludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            baseQuery = baseQuery.Where(e =>
                !context.ItemValuesMap.Any(f =>
                    f.ItemValue.Type == ItemValueType.Tags
                    && excludedTags.Contains(f.ItemValue.CleanValue)
                    && (f.ItemId == e.Id
                        || (e.SeriesId.HasValue && f.ItemId == e.SeriesId.Value)
                        || e.Parents!.Any(p => f.ItemId == p.ParentItemId)
                        || (e.TopParentId.HasValue && f.ItemId == e.TopParentId.Value))));
        }

        if (filter.IncludeInheritedTags.Length > 0)
        {
            var includeTags = filter.IncludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            var isPlaylistOnlyQuery = includeTypes.Length == 1 && includeTypes.FirstOrDefault() == BaseItemKind.Playlist;
            baseQuery = baseQuery.Where(e =>
                context.ItemValuesMap.Any(f =>
                    f.ItemValue.Type == ItemValueType.Tags
                    && includeTags.Contains(f.ItemValue.CleanValue)
                    && (f.ItemId == e.Id
                        || (e.SeriesId.HasValue && f.ItemId == e.SeriesId.Value)
                        || e.Parents!.Any(p => f.ItemId == p.ParentItemId)
                        || (e.TopParentId.HasValue && f.ItemId == e.TopParentId.Value)))

                // A playlist should be accessible to its owner regardless of allowed tags
                || (isPlaylistOnlyQuery && e.Data!.Contains($"OwnerUserId\":\"{filter.User!.Id:N}\"")));
        }

        if (filter.SeriesStatuses.Length > 0)
        {
            var seriesStatus = filter.SeriesStatuses.Select(e => e.ToString()).ToArray();
            baseQuery = baseQuery
                .Where(e => seriesStatus.Any(f => e.Data!.Contains(f)));
        }

        if (filter.BoxSetLibraryFolders.Length > 0)
        {
            var boxsetFolders = filter.BoxSetLibraryFolders.Select(e => e.ToString("N", CultureInfo.InvariantCulture)).ToArray();
            baseQuery = baseQuery
                .Where(e => boxsetFolders.Any(f => e.Data!.Contains(f)));
        }

        if (filter.VideoTypes.Length > 0)
        {
            var videoTypeBs = filter.VideoTypes.Select(vt => $"\"VideoType\":\"{vt}\"").ToArray();
            Expression<Func<BaseItemEntity, bool>> hasVideoType = e => videoTypeBs.Any(f => e.Data!.Contains(f));
            baseQuery = baseQuery.WhereItemOrDescendantMatches(context, hasVideoType);
        }

        if (filter.Is3D.HasValue)
        {
            Expression<Func<BaseItemEntity, bool>> is3D = e => e.Data!.Contains("Video3DFormat");

            baseQuery = filter.Is3D.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, is3D)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, is3D);
        }

        if (filter.IsPlaceHolder.HasValue)
        {
            Expression<Func<BaseItemEntity, bool>> isPlaceHolder = e => e.Data!.Contains("IsPlaceHolder\":true");

            baseQuery = filter.IsPlaceHolder.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, isPlaceHolder)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, isPlaceHolder);
        }

        if (filter.HasSpecialFeature.HasValue)
        {
            var itemsWithExtras = context.BaseItems
                .Where(extra => extra.OwnerId != null
                    && extra.ExtraType != null
                    && extra.ExtraType != BaseItemExtraType.Unknown
                    && extra.ExtraType != BaseItemExtraType.Trailer
                    && extra.ExtraType != BaseItemExtraType.ThemeSong
                    && extra.ExtraType != BaseItemExtraType.ThemeVideo)
                .Select(extra => extra.OwnerId!.Value)
                .Distinct();

            Expression<Func<BaseItemEntity, bool>> hasExtras = e => itemsWithExtras.Contains(e.Id);

            baseQuery = filter.HasSpecialFeature.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasExtras)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasExtras);
        }

        if (filter.HasTrailer.HasValue)
        {
            var trailerOwnerIds = context.BaseItems
                .Where(extra => extra.ExtraType == BaseItemExtraType.Trailer && extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value);

            Expression<Func<BaseItemEntity, bool>> hasTrailer = e => trailerOwnerIds.Contains(e.Id);

            baseQuery = filter.HasTrailer.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasTrailer)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasTrailer);
        }

        if (filter.HasThemeSong.HasValue)
        {
            var themeSongOwnerIds = context.BaseItems
                .Where(extra => extra.ExtraType == BaseItemExtraType.ThemeSong && extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value);

            Expression<Func<BaseItemEntity, bool>> hasThemeSong = e => themeSongOwnerIds.Contains(e.Id);

            baseQuery = filter.HasThemeSong.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasThemeSong)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasThemeSong);
        }

        if (filter.HasThemeVideo.HasValue)
        {
            var themeVideoOwnerIds = context.BaseItems
                .Where(extra => extra.ExtraType == BaseItemExtraType.ThemeVideo && extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value);

            Expression<Func<BaseItemEntity, bool>> hasThemeVideo = e => themeVideoOwnerIds.Contains(e.Id);

            baseQuery = filter.HasThemeVideo.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasThemeVideo)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasThemeVideo);
        }

        if (filter.AiredDuringSeason.HasValue)
        {
            var seasonNumber = filter.AiredDuringSeason.Value;
            if (seasonNumber < 1)
            {
                baseQuery = baseQuery.Where(e => e.ParentIndexNumber == seasonNumber);
            }
            else
            {
                var seasonStr = seasonNumber.ToString(CultureInfo.InvariantCulture);
                baseQuery = baseQuery.Where(e =>
                    e.ParentIndexNumber == seasonNumber
                    || (e.Data != null && (
                        e.Data.Contains("\"AirsAfterSeasonNumber\":" + seasonStr)
                        || e.Data.Contains("\"AirsBeforeSeasonNumber\":" + seasonStr))));
            }
        }

        if (filter.AdjacentTo.HasValue && !filter.AdjacentTo.Value.IsEmpty())
        {
            var adjacentToId = filter.AdjacentTo.Value;
            var targetItem = context.BaseItems.Where(e => e.Id == adjacentToId).Select(e => new { e.SortName, e.Id }).FirstOrDefault();
            if (targetItem is not null)
            {
                var targetSortName = targetItem.SortName ?? string.Empty;

                // Fetch both prev and next adjacent items in a single query using Concat (UNION ALL).
                var adjacentIds = context.BaseItems
                    .Where(e => string.Compare(e.SortName, targetSortName) < 0)
                    .OrderByDescending(e => e.SortName)
                    .Select(e => e.Id)
                    .Take(1)
                    .Concat(
                        context.BaseItems
                            .Where(e => string.Compare(e.SortName, targetSortName) > 0)
                            .OrderBy(e => e.SortName)
                            .Select(e => e.Id)
                            .Take(1))
                    .ToList();

                adjacentIds.Add(adjacentToId);
                baseQuery = baseQuery.Where(e => adjacentIds.Contains(e.Id));
            }
        }

        return baseQuery;
    }
}
