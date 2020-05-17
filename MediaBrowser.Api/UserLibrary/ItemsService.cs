using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetItems
    /// </summary>
    [Route("/Items", "GET", Summary = "Gets items based on a query.")]
    [Route("/Users/{UserId}/Items", "GET", Summary = "Gets items based on a query.")]
    public class GetItems : BaseItemsRequest, IReturn<QueryResult<BaseItemDto>>
    {
    }

    [Route("/Users/{UserId}/Items/Resume", "GET", Summary = "Gets items based on a query.")]
    public class GetResumeItems : BaseItemsRequest, IReturn<QueryResult<BaseItemDto>>
    {
    }

    /// <summary>
    /// Class ItemsService
    /// </summary>
    [Authenticated]
    public class ItemsService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;

        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="localization">The localization.</param>
        /// <param name="dtoService">The dto service.</param>
        public ItemsService(
            ILogger<ItemsService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            ILibraryManager libraryManager,
            ILocalizationManager localization,
            IDtoService dtoService,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _localization = localization;
            _dtoService = dtoService;
            _authContext = authContext;
        }

        public object Get(GetResumeItems request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var parentIdGuid = string.IsNullOrWhiteSpace(request.ParentId) ? Guid.Empty : new Guid(request.ParentId);

            var options = GetDtoOptions(_authContext, request);

            var ancestorIds = Array.Empty<Guid>();

            var excludeFolderIds = user.Configuration.LatestItemsExcludes;
            if (parentIdGuid.Equals(Guid.Empty) && excludeFolderIds.Length > 0)
            {
                ancestorIds = _libraryManager.GetUserRootFolder().GetChildren(user, true)
                    .Where(i => i is Folder)
                    .Where(i => !excludeFolderIds.Contains(i.Id.ToString("N", CultureInfo.InvariantCulture)))
                    .Select(i => i.Id)
                    .ToArray();
            }

            var itemsResult = _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                IsResumable = true,
                StartIndex = request.StartIndex,
                Limit = request.Limit,
                ParentId = parentIdGuid,
                Recursive = true,
                DtoOptions = options,
                MediaTypes = request.GetMediaTypes(),
                IsVirtualItem = false,
                CollapseBoxSetItems = false,
                EnableTotalRecordCount = request.EnableTotalRecordCount,
                AncestorIds = ancestorIds,
                IncludeItemTypes = request.GetIncludeItemTypes(),
                ExcludeItemTypes = request.GetExcludeItemTypes(),
                SearchTerm = request.SearchTerm
            });

            var returnItems = _dtoService.GetBaseItemDtos(itemsResult.Items, options, user);

            var result = new QueryResult<BaseItemDto>
            {
                StartIndex = request.StartIndex.GetValueOrDefault(),
                TotalRecordCount = itemsResult.TotalRecordCount,
                Items = returnItems
            };

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItems request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var result = GetItems(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="request">The request.</param>
        private QueryResult<BaseItemDto> GetItems(GetItems request)
        {
            var user = request.UserId == Guid.Empty ? null : _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var result = GetQueryResult(request, dtoOptions, user);

            if (result == null)
            {
                throw new InvalidOperationException("GetItemsToSerialize returned null");
            }

            if (result.Items == null)
            {
                throw new InvalidOperationException("GetItemsToSerialize result.Items returned null");
            }

            var dtoList = _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user);

            return new QueryResult<BaseItemDto>
            {
                StartIndex = request.StartIndex.GetValueOrDefault(),
                TotalRecordCount = result.TotalRecordCount,
                Items = dtoList
            };
        }

        /// <summary>
        /// Gets the items to serialize.
        /// </summary>
        private QueryResult<BaseItem> GetQueryResult(GetItems request, DtoOptions dtoOptions, User user)
        {
            if (string.Equals(request.IncludeItemTypes, "Playlist", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.IncludeItemTypes, "BoxSet", StringComparison.OrdinalIgnoreCase))
            {
                request.ParentId = null;
            }

            BaseItem item = null;

            if (!string.IsNullOrEmpty(request.ParentId))
            {
                item = _libraryManager.GetItemById(request.ParentId);
            }

            if (item == null)
            {
                item = _libraryManager.GetUserRootFolder();
            }

            if (!(item is Folder folder))
            {
                folder = _libraryManager.GetUserRootFolder();
            }

            if (folder is IHasCollectionType hasCollectionType
                && string.Equals(hasCollectionType.CollectionType, CollectionType.Playlists, StringComparison.OrdinalIgnoreCase))
            {
                request.Recursive = true;
                request.IncludeItemTypes = "Playlist";
            }

            bool isInEnabledFolder = user.Policy.EnabledFolders.Any(i => new Guid(i) == item.Id)
                    // Assume all folders inside an EnabledChannel are enabled
                    || user.Policy.EnabledChannels.Any(i => new Guid(i) == item.Id);

            var collectionFolders = _libraryManager.GetCollectionFolders(item);
            foreach (var collectionFolder in collectionFolders)
            {
                if (user.Policy.EnabledFolders.Contains(
                    collectionFolder.Id.ToString("N", CultureInfo.InvariantCulture),
                    StringComparer.OrdinalIgnoreCase))
                {
                    isInEnabledFolder = true;
                }
            }

            if (!(item is UserRootFolder) && !user.Policy.EnableAllFolders && !isInEnabledFolder && !user.Policy.EnableAllChannels)
            {
                Logger.LogWarning("{UserName} is not permitted to access Library {ItemName}.", user.Name, item.Name);
                return new QueryResult<BaseItem>
                {
                    Items = Array.Empty<BaseItem>(),
                    TotalRecordCount = 0,
                    StartIndex = 0
                };
            }

            if (request.Recursive || !string.IsNullOrEmpty(request.Ids) || !(item is UserRootFolder))
            {
                return folder.GetItems(GetItemsQuery(request, dtoOptions, user));
            }

            var itemsArray = folder.GetChildren(user, true);
            return new QueryResult<BaseItem>
            {
                Items = itemsArray,
                TotalRecordCount = itemsArray.Count,
                StartIndex = 0
            };
        }

        private InternalItemsQuery GetItemsQuery(GetItems request, DtoOptions dtoOptions, User user)
        {
            var query = new InternalItemsQuery(user)
            {
                IsPlayed = request.IsPlayed,
                MediaTypes = request.GetMediaTypes(),
                IncludeItemTypes = request.GetIncludeItemTypes(),
                ExcludeItemTypes = request.GetExcludeItemTypes(),
                Recursive = request.Recursive,
                OrderBy = request.GetOrderBy(),

                IsFavorite = request.IsFavorite,
                Limit = request.Limit,
                StartIndex = request.StartIndex,
                IsMissing = request.IsMissing,
                IsUnaired = request.IsUnaired,
                CollapseBoxSetItems = request.CollapseBoxSetItems,
                NameLessThan = request.NameLessThan,
                NameStartsWith = request.NameStartsWith,
                NameStartsWithOrGreater = request.NameStartsWithOrGreater,
                HasImdbId = request.HasImdbId,
                IsPlaceHolder = request.IsPlaceHolder,
                IsLocked = request.IsLocked,
                MinWidth = request.MinWidth,
                MinHeight = request.MinHeight,
                MaxWidth = request.MaxWidth,
                MaxHeight = request.MaxHeight,
                Is3D = request.Is3D,
                HasTvdbId = request.HasTvdbId,
                HasTmdbId = request.HasTmdbId,
                HasOverview = request.HasOverview,
                HasOfficialRating = request.HasOfficialRating,
                HasParentalRating = request.HasParentalRating,
                HasSpecialFeature = request.HasSpecialFeature,
                HasSubtitles = request.HasSubtitles,
                HasThemeSong = request.HasThemeSong,
                HasThemeVideo = request.HasThemeVideo,
                HasTrailer = request.HasTrailer,
                IsHD = request.IsHD,
                Is4K = request.Is4K,
                Tags = request.GetTags(),
                OfficialRatings = request.GetOfficialRatings(),
                Genres = request.GetGenres(),
                ArtistIds = GetGuids(request.ArtistIds),
                AlbumArtistIds = GetGuids(request.AlbumArtistIds),
                ContributingArtistIds = GetGuids(request.ContributingArtistIds),
                GenreIds = GetGuids(request.GenreIds),
                StudioIds = GetGuids(request.StudioIds),
                Person = request.Person,
                PersonIds = GetGuids(request.PersonIds),
                PersonTypes = request.GetPersonTypes(),
                Years = request.GetYears(),
                ImageTypes = request.GetImageTypes(),
                VideoTypes = request.GetVideoTypes(),
                AdjacentTo = request.AdjacentTo,
                ItemIds = GetGuids(request.Ids),
                MinCommunityRating = request.MinCommunityRating,
                MinCriticRating = request.MinCriticRating,
                ParentId = string.IsNullOrWhiteSpace(request.ParentId) ? Guid.Empty : new Guid(request.ParentId),
                ParentIndexNumber = request.ParentIndexNumber,
                EnableTotalRecordCount = request.EnableTotalRecordCount,
                ExcludeItemIds = GetGuids(request.ExcludeItemIds),
                DtoOptions = dtoOptions,
                SearchTerm = request.SearchTerm
            };

            if (!string.IsNullOrWhiteSpace(request.Ids) || !string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query.CollapseBoxSetItems = false;
            }

            foreach (var filter in request.GetFilters())
            {
                switch (filter)
                {
                    case ItemFilter.Dislikes:
                        query.IsLiked = false;
                        break;
                    case ItemFilter.IsFavorite:
                        query.IsFavorite = true;
                        break;
                    case ItemFilter.IsFavoriteOrLikes:
                        query.IsFavoriteOrLiked = true;
                        break;
                    case ItemFilter.IsFolder:
                        query.IsFolder = true;
                        break;
                    case ItemFilter.IsNotFolder:
                        query.IsFolder = false;
                        break;
                    case ItemFilter.IsPlayed:
                        query.IsPlayed = true;
                        break;
                    case ItemFilter.IsResumable:
                        query.IsResumable = true;
                        break;
                    case ItemFilter.IsUnplayed:
                        query.IsPlayed = false;
                        break;
                    case ItemFilter.Likes:
                        query.IsLiked = true;
                        break;
                }
            }

            if (!string.IsNullOrEmpty(request.MinDateLastSaved))
            {
                query.MinDateLastSaved = DateTime.Parse(request.MinDateLastSaved, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MinDateLastSavedForUser))
            {
                query.MinDateLastSavedForUser = DateTime.Parse(request.MinDateLastSavedForUser, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MinPremiereDate))
            {
                query.MinPremiereDate = DateTime.Parse(request.MinPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MaxPremiereDate))
            {
                query.MaxPremiereDate = DateTime.Parse(request.MaxPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            // Filter by Series Status
            if (!string.IsNullOrEmpty(request.SeriesStatus))
            {
                query.SeriesStatuses = request.SeriesStatus.Split(',').Select(d => (SeriesStatus)Enum.Parse(typeof(SeriesStatus), d, true)).ToArray();
            }

            // ExcludeLocationTypes
            if (!string.IsNullOrEmpty(request.ExcludeLocationTypes))
            {
                var excludeLocationTypes = request.ExcludeLocationTypes.Split(',').Select(d => (LocationType)Enum.Parse(typeof(LocationType), d, true)).ToArray();
                if (excludeLocationTypes.Contains(LocationType.Virtual))
                {
                    query.IsVirtualItem = false;
                }
            }

            if (!string.IsNullOrEmpty(request.LocationTypes))
            {
                var requestedLocationTypes =
                    request.LocationTypes.Split(',');

                if (requestedLocationTypes.Length > 0 && requestedLocationTypes.Length < 4)
                {
                    query.IsVirtualItem = requestedLocationTypes.Contains(LocationType.Virtual.ToString());
                }
            }

            // Min official rating
            if (!string.IsNullOrWhiteSpace(request.MinOfficialRating))
            {
                query.MinParentalRating = _localization.GetRatingLevel(request.MinOfficialRating);
            }

            // Max official rating
            if (!string.IsNullOrWhiteSpace(request.MaxOfficialRating))
            {
                query.MaxParentalRating = _localization.GetRatingLevel(request.MaxOfficialRating);
            }

            // Artists
            if (!string.IsNullOrEmpty(request.Artists))
            {
                query.ArtistIds = request.Artists.Split('|').Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetArtist(i, new DtoOptions(false));
                    }
                    catch
                    {
                        return null;
                    }
                }).Where(i => i != null).Select(i => i.Id).ToArray();
            }

            // ExcludeArtistIds
            if (!string.IsNullOrWhiteSpace(request.ExcludeArtistIds))
            {
                query.ExcludeArtistIds = GetGuids(request.ExcludeArtistIds);
            }

            if (!string.IsNullOrWhiteSpace(request.AlbumIds))
            {
                query.AlbumIds = GetGuids(request.AlbumIds);
            }

            // Albums
            if (!string.IsNullOrEmpty(request.Albums))
            {
                query.AlbumIds = request.Albums.Split('|').SelectMany(i =>
                {
                    return _libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(MusicAlbum).Name },
                        Name = i,
                        Limit = 1
                    });
                }).ToArray();
            }

            // Studios
            if (!string.IsNullOrEmpty(request.Studios))
            {
                query.StudioIds = request.Studios.Split('|').Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetStudio(i);
                    }
                    catch
                    {
                        return null;
                    }
                }).Where(i => i != null).Select(i => i.Id).ToArray();
            }

            // Apply default sorting if none requested
            if (query.OrderBy.Count == 0)
            {
                // Albums by artist
                if (query.ArtistIds.Length > 0 && query.IncludeItemTypes.Length == 1 && string.Equals(query.IncludeItemTypes[0], "MusicAlbum", StringComparison.OrdinalIgnoreCase))
                {
                    query.OrderBy = new[]
                    {
                        new ValueTuple<string, SortOrder>(ItemSortBy.ProductionYear, SortOrder.Descending),
                        new ValueTuple<string, SortOrder>(ItemSortBy.SortName, SortOrder.Ascending)
                    };
                }
            }

            return query;
        }
    }

    /// <summary>
    /// Class DateCreatedComparer
    /// </summary>
    public class DateCreatedComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return x.DateCreated.CompareTo(y.DateCreated);
        }
    }
}
