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

    private static readonly string[] _artistCreditKinds = [nameof(PersonKind.Artist), nameof(PersonKind.AlbumArtist)];
    private static readonly string[] _albumArtistCreditKinds = [nameof(PersonKind.AlbumArtist)];
    private static readonly string[] _trackArtistCreditKinds = [nameof(PersonKind.Artist)];

    // A fresh expression per access: EF rejects a query tree that reuses one lambda parameter
    // instance across several lambdas, and this filter is combined into a tree more than once.
    private static Expression<Func<BaseItemEntity, bool>> IsFolderFilter => e => e.IsFolder;

    private static IQueryable<Guid> ArtistCreditIds(
        JellyfinDbContext context,
        IReadOnlyList<Guid> artistIds,
        string[] kinds)
        => context.Peoples
            .WhereOneOrMany(artistIds, p => p.ItemId)
            .Where(p => kinds.Contains(p.PersonType))
            .Select(p => p.Id);

    private static IQueryable<BaseItemEntity> WhereCreditedTo(
        IQueryable<BaseItemEntity> baseQuery,
        JellyfinDbContext context,
        IReadOnlyList<Guid> artistIds,
        string[] kinds,
        bool invert = false)
    {
        var creditIds = ArtistCreditIds(context, artistIds, kinds);

        return invert
            ? baseQuery.Where(e => !context.PeopleBaseItemMap.Any(m => m.ItemId == e.Id && creditIds.Contains(m.PeopleId)))
            : baseQuery.Where(e => context.PeopleBaseItemMap.Any(m => m.ItemId == e.Id && creditIds.Contains(m.PeopleId)));
    }

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

            // Non-folders: check own resolution directly (no subquery).
            // Folders (Series, BoxSets): EXISTS check on descendants/linked children.
            // Using navigation properties (a.Item, lc.Child) produces efficient
            // EXISTS + JOIN instead of nested IN (SELECT ...) subqueries.
            baseQuery = baseQuery.Where(e =>
                (!e.IsFolder && e.Width > 0
                    && ((includeSD && e.Width < HDWidth)
                        || (includeHD && e.Width >= HDWidth && !(e.Width >= UHDWidth || e.Height >= UHDHeight))
                        || (include4K && (e.Width >= UHDWidth || e.Height >= UHDHeight))))
                || (e.IsFolder
                    && (e.Children!.Any(a =>
                            a.Item.Width > 0
                            && ((includeSD && a.Item.Width < HDWidth)
                                || (includeHD && a.Item.Width >= HDWidth && !(a.Item.Width >= UHDWidth || a.Item.Height >= UHDHeight))
                                || (include4K && (a.Item.Width >= UHDWidth || a.Item.Height >= UHDHeight))))
                        || context.LinkedChildren.Any(lc =>
                            lc.ParentId == e.Id
                            && lc.Child!.Width > 0
                            && ((includeSD && lc.Child.Width < HDWidth)
                                || (includeHD && lc.Child.Width >= HDWidth && !(lc.Child.Width >= UHDWidth || lc.Child.Height >= UHDHeight))
                                || (include4K && (lc.Child.Width >= UHDWidth || lc.Child.Height >= UHDHeight)))))));
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
                baseQuery = baseQuery.Where(e => e.StartDate > now || e.EndDate < now);
            }
        }

        if (filter.PersonIds.Length > 0)
        {
            var peopleEntityIds = context.Peoples
                .WhereOneOrMany(filter.PersonIds, p => p.ItemId)
                .Select(p => p.Id);

            var personTypes = filter.PersonTypes;
            baseQuery = baseQuery
                .Where(e => context.PeopleBaseItemMap
                    .Any(m => m.ItemId == e.Id && peopleEntityIds.Contains(m.PeopleId) && (personTypes.Length == 0 || personTypes.Contains(m.People.PersonType))));
        }

        if (!string.IsNullOrWhiteSpace(filter.Person))
        {
            var cleanPerson = filter.Person.GetCleanValue();
            var personTypes = filter.PersonTypes;
            baseQuery = baseQuery.Where(e => e.Peoples!.Any(f => f.People.CleanName == cleanPerson && (personTypes.Length == 0 || personTypes.Contains(f.People.PersonType))));
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
                var nameLower = filter.Name.ToLowerInvariant();
                baseQuery = baseQuery.Where(e => e.Name!.ToLower() == nameLower);
            }
            else
            {
                var cleanName = filter.Name.GetCleanValue();
                baseQuery = baseQuery.Where(e => e.CleanName == cleanName);
            }
        }

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
            var likedIds = context.UserData
                .Where(ud => ud.UserId == filter.User!.Id && ud.Rating >= UserItemData.MinLikeValue)
                .Select(ud => ud.ItemId);

            baseQuery = filter.IsLiked.Value
                ? baseQuery.Where(e => likedIds.Contains(e.Id))
                : baseQuery.Where(e => !likedIds.Contains(e.Id));
        }

        if (filter.IsFavoriteOrLiked.HasValue || filter.IsFavorite.HasValue)
        {
            var favoriteIds = context.UserData
                .Where(ud => ud.UserId == filter.User!.Id && ud.IsFavorite)
                .Select(ud => ud.ItemId);

            if (filter.IsFavoriteOrLiked.HasValue)
            {
                baseQuery = filter.IsFavoriteOrLiked.Value
                    ? baseQuery.Where(e => favoriteIds.Contains(e.Id))
                    : baseQuery.Where(e => !favoriteIds.Contains(e.Id));
            }

            if (filter.IsFavorite.HasValue)
            {
                baseQuery = filter.IsFavorite.Value
                    ? baseQuery.Where(e => favoriteIds.Contains(e.Id))
                    : baseQuery.Where(e => !favoriteIds.Contains(e.Id));
            }
        }

        if (filter.IsPlayed.HasValue)
        {
            var userId = filter.User!.Id;

            // Leaf items carry their own played state.
            var playedItemIds = context.UserData
                .Where(ud => ud.UserId == userId && ud.Played)
                .Select(ud => ud.ItemId);

            // Folders (Series, Seasons, BoxSets, albums, ...) have none and count as played once no
            // descendant is left unplayed, matching what the DTO reports for them. This has to key off
            // the item itself rather than off the requested item types: tag and collection listings mix
            // folders and leaf items in a single query.
            var unplayedLeafItems = GetAccessFilteredLeafItemsQuery(context, filter.User!)
                .Where(e => !e.UserData!.Any(ud => ud.UserId == userId && ud.Played));

            var isPlayedFilter = IsFolderFilter.And(BuildHasDescendantFilter(context, unplayedLeafItems).Not())
                .Or(IsFolderFilter.Not().And(e => playedItemIds.Contains(e.Id)));

            baseQuery = baseQuery.Where(filter.IsPlayed.Value ? isPlayedFilter : isPlayedFilter.Not());
        }

        if (filter.IsResumable.HasValue)
        {
            var userId = filter.User!.Id;
            var isResumable = filter.IsResumable.Value;

            // In-progress user data rows; alternate versions track their own progress.
            var inProgress = context.UserData
                .Where(ud => ud.UserId == userId && ud.PlaybackPositionTicks > 0);

            // Series and Seasons are resumable when a descendant is in progress, or when they hold both
            // played and unplayed descendants (partially watched). Alternate versions keep their own
            // progress, so they count towards the in-progress check but not towards the played/unplayed one.
            var leafItems = GetAccessFilteredLeafItemsQuery(context, filter.User!);
            var inProgressLeafItems = GetAccessFilteredLeafItemsQuery(context, filter.User!, includeOwnedItems: true)
                .Where(e => e.UserData!.Any(ud => ud.UserId == userId && ud.PlaybackPositionTicks > 0));

            // Every other folder kind is a container rather than one continuous piece of media
            var resumableFolderTypes = _resumableFolderKinds
                .Select(kind => _itemTypeLookup.BaseItemKindNames.GetValueOrDefault(kind))
                .ToArray();
            var folderIsResumableFilter = IsFolderFilter.And(e => resumableFolderTypes.Contains(e.Type))
                .And(BuildHasDescendantFilter(context, inProgressLeafItems)
                    .Or(BuildHasDescendantFilter(context, leafItems.Where(e => e.UserData!.Any(ud => ud.UserId == userId && ud.Played)))
                        .And(BuildHasDescendantFilter(context, leafItems.Where(e => !e.UserData!.Any(ud => ud.UserId == userId && ud.Played))))));

            if (isResumable)
            {
                // Resume queries surface the version that was actually played, which may be an alternate.
                // Match each version on its own progress rather than coalescing onto the primary.
                var inProgressIds = inProgress.Select(ud => ud.ItemId);

                baseQuery = baseQuery.Where(folderIsResumableFilter
                    .Or(IsFolderFilter.Not().And(e => inProgressIds.Contains(e.Id))));

                // When several versions of the same item are in progress, keep only the most recently played one, use id as tiebreaker.
                // Only in-progress siblings can eliminate a candidate: a version without progress has a NULL max LastPlayedDate,
                // which is never greater and never ties. Restricting the sibling scan to the in-progress set keeps this bounded by
                // the user's Continue Watching count instead of forcing a full BaseItems scan (COALESCE keys are non-indexable) per row.
                // Items in no version group at all have no sibling that could eliminate them, so short-circuit the scan for those.
                baseQuery = baseQuery.Where(e => e.IsFolder
                    || (e.PrimaryVersionId == null && !context.BaseItems.Any(a => a.PrimaryVersionId == e.Id))
                    || !context.BaseItems
                        .Where(s => s.Id != e.Id
                            && inProgressIds.Contains(s.Id)
                            && (s.PrimaryVersionId ?? s.Id) == (e.PrimaryVersionId ?? e.Id))
                        .Any(s =>
                            inProgress.Where(su => su.ItemId == s.Id).Max(su => su.LastPlayedDate)
                                > inProgress.Where(eu => eu.ItemId == e.Id).Max(eu => eu.LastPlayedDate)
                            || (inProgress.Where(su => su.ItemId == s.Id).Max(su => su.LastPlayedDate)
                                    == inProgress.Where(eu => eu.ItemId == e.Id).Max(eu => eu.LastPlayedDate)
                                && s.Id.CompareTo(e.Id) < 0)));
            }
            else
            {
                // Not-resumable queries operate on primaries only.
                var resumableMovieIds = inProgress
                    .Join(context.BaseItems, ud => ud.ItemId, bi => bi.Id, (ud, bi) => bi.PrimaryVersionId ?? bi.Id);

                baseQuery = baseQuery.Where(IsFolderFilter.And(folderIsResumableFilter.Not())
                    .Or(IsFolderFilter.Not().And(e => !resumableMovieIds.Contains(e.Id))));
            }
        }

        if (filter.ArtistIds.Length > 0)
        {
            baseQuery = WhereCreditedTo(baseQuery, context, filter.ArtistIds, _artistCreditKinds);
        }

        if (filter.AlbumArtistIds.Length > 0)
        {
            baseQuery = WhereCreditedTo(baseQuery, context, filter.AlbumArtistIds, _albumArtistCreditKinds);
        }

        if (filter.ContributingArtistIds.Length > 0)
        {
            // Credited on the release but not filed under - the guest on someone else's record.
            var trackCreditIds = ArtistCreditIds(context, filter.ContributingArtistIds, _trackArtistCreditKinds);
            var albumCreditIds = ArtistCreditIds(context, filter.ContributingArtistIds, _albumArtistCreditKinds);

            baseQuery = baseQuery.Where(e =>
                context.PeopleBaseItemMap.Any(m => m.ItemId == e.Id && trackCreditIds.Contains(m.PeopleId))
                && !context.PeopleBaseItemMap.Any(m => m.ItemId == e.Id && albumCreditIds.Contains(m.PeopleId)));
        }

        if (filter.AlbumIds.Length > 0)
        {
            baseQuery = baseQuery.Where(e => e.ParentId.HasValue && filter.AlbumIds.Contains(e.ParentId.Value));
        }

        if (filter.ExcludeArtistIds.Length > 0)
        {
            baseQuery = WhereCreditedTo(baseQuery, context, filter.ExcludeArtistIds, _artistCreditKinds, true);
        }

        if (filter.GenreIds.Count > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.Genre, filter.GenreIds);
        }

        if (filter.Genres.Count > 0)
        {
            var cleanGenres = filter.Genres.Select(e => e.GetCleanValue()).ToArray().OneOrManyExpressionBuilder<ItemValueMap, string>(f => f.ItemValue.CleanValue);
            baseQuery = baseQuery
                    .Where(e => e.ItemValues!.AsQueryable().Where(f => f.ItemValue.Type == ItemValueType.Genre).Any(cleanGenres));
        }

        if (tags.Count > 0)
        {
            var cleanValues = tags.Select(e => e.GetCleanValue()).ToArray().OneOrManyExpressionBuilder<BaseItemTag, string>(f => f.CleanValue);
            baseQuery = baseQuery
                    .Where(e => e.ItemTags!.AsQueryable().Any(cleanValues));
        }

        if (excludeTags.Count > 0)
        {
            var cleanValues = excludeTags.Select(e => e.GetCleanValue()).ToArray().OneOrManyExpressionBuilder<BaseItemTag, string>(f => f.CleanValue);
            baseQuery = baseQuery
                    .Where(e => !e.ItemTags!.AsQueryable().Any(cleanValues));
        }

        if (filter.StudioIds.Length > 0)
        {
            baseQuery = baseQuery.WhereReferencedItem(context, ItemValueType.Studios, filter.StudioIds);
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
            // Exclude owned non-extra items from general queries.
            // Extras (trailers, etc.) have OwnerId set but also have ExtraType set - keep those.
            // Alternate versions (PrimaryVersionId set) are normally excluded too, but resume queries
            // keep them so the actually-played version can surface instead of collapsing onto the primary.
            baseQuery = filter.IsResumable == true
                ? baseQuery.Where(e => e.OwnerId == null || e.ExtraType != null)
                : baseQuery.Where(e => e.PrimaryVersionId == null && (e.OwnerId == null || e.ExtraType != null));
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

        if (filter.SubtitleLanguages.Count > 0)
        {
            var foldersWithSubtitles = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Subtitle, filter.SubtitleLanguages));
            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Subtitle
                     && (filter.SubtitleLanguages.Contains(f.Language) || (filter.SubtitleLanguages.Contains("und") && string.IsNullOrEmpty(f.Language)))))
                    || (e.IsFolder && foldersWithSubtitles.Contains(e.Id)));
        }

        if (filter.AudioLanguages.Count > 0)
        {
            var foldersWithAudio = DescendantQueryHelper.GetFolderIdsMatching(context, new HasMediaStreamType(MediaStreamTypeEntity.Audio, filter.AudioLanguages));
            baseQuery = baseQuery
                .Where(e =>
                    (!e.IsFolder && e.MediaStreams!.Any(f => f.StreamType == MediaStreamTypeEntity.Audio
                     && (filter.AudioLanguages.Contains(f.Language) || (filter.AudioLanguages.Contains("und") && string.IsNullOrEmpty(f.Language)))))
                    || (e.IsFolder && foldersWithAudio.Contains(e.Id)));
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
            // Keyed on the link, so a renamed artist is not deleted as dead by artist validation.
            baseQuery = baseQuery
                    .Where(e => !context.Peoples.Any(p => _artistCreditKinds.Contains(p.PersonType) && p.ItemId == e.Id));
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
                .Where(e => !context.Peoples.Any(f => f.ItemId == e.Id));
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
            baseQuery = baseQuery.WhereExcludeProviderIds(filter.ExcludeProviderIds);
        }

        if (filter.HasAnyProviderId is not null && filter.HasAnyProviderId.Count > 0)
        {
            baseQuery = baseQuery.WhereHasAnyProviderId(filter.HasAnyProviderId);
        }

        if (filter.HasAnyProviderIds is not null && filter.HasAnyProviderIds.Count > 0)
        {
            baseQuery = baseQuery.WhereHasAnyProviderIds(filter.HasAnyProviderIds);
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

        baseQuery = ApplyTopParentFiltering(context, baseQuery, filter);

        if (filter.AncestorIds.Length > 0)
        {
            var ancestorFilter = filter.AncestorIds.OneOrManyExpressionBuilder<AncestorId, Guid>(f => f.ParentItemId);
            baseQuery = baseQuery.Where(e => e.Parents!.AsQueryable().Any(ancestorFilter));
        }

        if (filter.LinkedChildAncestorIds.Length > 0)
        {
            // Keep folder-like items (BoxSets, Playlists) whose linked children descend from any of the requested ancestor ids.
            var linkedChildAncestorIds = filter.LinkedChildAncestorIds;
            baseQuery = baseQuery.Where(e => context.LinkedChildren.Any(lc =>
                lc.ParentId == e.Id
                && lc.Child!.Parents!.Any(a => linkedChildAncestorIds.Contains(a.ParentItemId))));
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

        // Pre-build the blocked-item-id set as a sub-select
        if (filter.ExcludeInheritedTags.Length > 0)
        {
            var excludedTags = filter.ExcludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            var blockedTagItemIds = context.BaseItemTags
                .Where(f => excludedTags.Contains(f.CleanValue))
                .Select(f => f.ItemId);

            baseQuery = baseQuery.Where(e =>
                !blockedTagItemIds.Contains(e.Id)
                && !(e.SeriesId.HasValue && blockedTagItemIds.Contains(e.SeriesId.Value))
                && !e.Parents!.Any(p => blockedTagItemIds.Contains(p.ParentItemId))
                && !(e.TopParentId.HasValue && blockedTagItemIds.Contains(e.TopParentId.Value)));
        }

        if (filter.IncludeInheritedTags.Length > 0)
        {
            var includeTags = filter.IncludeInheritedTags.Select(e => e.GetCleanValue()).ToArray();
            var isPlaylistOnlyQuery = includeTypes.Length == 1 && includeTypes.FirstOrDefault() == BaseItemKind.Playlist;
            var personTypeName = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Person];
            var allowedTagItemIds = context.BaseItemTags
                .Where(f => includeTags.Contains(f.CleanValue))
                .Select(f => f.ItemId);

            baseQuery = baseQuery.Where(e =>
                allowedTagItemIds.Contains(e.Id)
                || (e.SeriesId.HasValue && allowedTagItemIds.Contains(e.SeriesId.Value))
                || e.Parents!.Any(p => allowedTagItemIds.Contains(p.ParentItemId))
                || (e.TopParentId.HasValue && allowedTagItemIds.Contains(e.TopParentId.Value))

                // People don't carry the tags of the media they appear in and would never match
                || e.Type == personTypeName

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
            // Dvds and Blu-rays can either be stored in a folder structure or as an iso file
            // => to find all matches we need to check both: VideoType and IsoType
            // alternatively, we could provide specific IsoType filters
            var videoTypeBs = filter.VideoTypes.Select(vt => $"\"VideoType\":\"{vt}\"").ToArray();
            var isoTypeBs = filter.VideoTypes.Select(vt => $"\"IsoType\":\"{vt}\"").ToArray();
            Expression<Func<BaseItemEntity, bool>> hasVideoType = e => videoTypeBs.Any(f => e.Data!.Contains(f)) || isoTypeBs.Any(f => e.Data!.Contains(f));
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

        // An extra is owned by the single version of an item it is named after, so an extra on any
        // version counts for the item itself
        IQueryable<Guid> WithPrimaryVersions(IQueryable<Guid> ownerIds)
            => ownerIds.Concat(context.BaseItems
                .Where(version => version.PrimaryVersionId != null && ownerIds.Contains(version.Id))
                .Select(version => version.PrimaryVersionId!.Value));

        if (filter.HasSpecialFeature.HasValue)
        {
            var itemsWithExtras = WithPrimaryVersions(context.BaseItems
                .Where(extra => extra.OwnerId != null
                    && extra.ExtraType != null
                    && extra.ExtraType != BaseItemExtraType.Unknown
                    && extra.ExtraType != BaseItemExtraType.Trailer
                    && extra.ExtraType != BaseItemExtraType.ThemeSong
                    && extra.ExtraType != BaseItemExtraType.ThemeVideo)
                .Select(extra => extra.OwnerId!.Value))
                .Distinct();

            Expression<Func<BaseItemEntity, bool>> hasExtras = e => itemsWithExtras.Contains(e.Id);

            baseQuery = filter.HasSpecialFeature.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasExtras)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasExtras);
        }

        if (filter.HasTrailer.HasValue)
        {
            var trailerOwnerIds = WithPrimaryVersions(context.BaseItems
                .Where(extra => extra.ExtraType == BaseItemExtraType.Trailer && extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value));

            Expression<Func<BaseItemEntity, bool>> hasTrailer = e => trailerOwnerIds.Contains(e.Id);

            baseQuery = filter.HasTrailer.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasTrailer)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasTrailer);
        }

        if (filter.HasThemeSong.HasValue)
        {
            var themeSongOwnerIds = WithPrimaryVersions(context.BaseItems
                .Where(extra => extra.ExtraType == BaseItemExtraType.ThemeSong && extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value));

            Expression<Func<BaseItemEntity, bool>> hasThemeSong = e => themeSongOwnerIds.Contains(e.Id);

            baseQuery = filter.HasThemeSong.Value
                ? baseQuery.WhereItemOrDescendantMatches(context, hasThemeSong)
                : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasThemeSong);
        }

        if (filter.HasThemeVideo.HasValue)
        {
            var themeVideoOwnerIds = WithPrimaryVersions(context.BaseItems
                .Where(extra => extra.ExtraType == BaseItemExtraType.ThemeVideo && extra.OwnerId != null)
                .Select(extra => extra.OwnerId!.Value));

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

        return baseQuery;
    }
}
