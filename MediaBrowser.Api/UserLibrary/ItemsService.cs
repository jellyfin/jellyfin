using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetItems
    /// </summary>
    [Route("/Items", "GET", Summary = "Gets items based on a query.")]
    [Route("/Users/{UserId}/Items", "GET", Summary = "Gets items based on a query.")]
    public class GetItems : BaseItemsRequest, IReturn<ItemsResult>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="localization">The localization.</param>
        /// <param name="dtoService">The dto service.</param>
        public ItemsService(IUserManager userManager, ILibraryManager libraryManager, ILocalizationManager localization, IDtoService dtoService)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException("userManager");
            }
            if (libraryManager == null)
            {
                throw new ArgumentNullException("libraryManager");
            }
            if (localization == null)
            {
                throw new ArgumentNullException("localization");
            }
            if (dtoService == null)
            {
                throw new ArgumentNullException("dtoService");
            }

            _userManager = userManager;
            _libraryManager = libraryManager;
            _localization = localization;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetItems request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var result = await GetItems(request).ConfigureAwait(false);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemsResult}.</returns>
        private async Task<ItemsResult> GetItems(GetItems request)
        {
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;
        
            var result = await GetQueryResult(request, user).ConfigureAwait(false);

            if (result == null)
            {
                throw new InvalidOperationException("GetItemsToSerialize returned null");
            }

            if (result.Items == null)
            {
                throw new InvalidOperationException("GetItemsToSerialize result.Items returned null");
            }

            var dtoOptions = GetDtoOptions(request);

            var dtoList = await _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user).ConfigureAwait(false);

            if (dtoList == null)
            {
                throw new InvalidOperationException("GetBaseItemDtos returned null");
            }

            return new ItemsResult
            {
                TotalRecordCount = result.TotalRecordCount,
                Items = dtoList.ToArray()
            };
        }

        /// <summary>
        /// Gets the items to serialize.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private async Task<QueryResult<BaseItem>> GetQueryResult(GetItems request, User user)
        {
            var item = string.IsNullOrEmpty(request.ParentId) ?
                user == null ? _libraryManager.RootFolder : user.RootFolder :
                _libraryManager.GetItemById(request.ParentId);

            if (string.Equals(request.IncludeItemTypes, "Playlist", StringComparison.OrdinalIgnoreCase))
            {
                //item = user == null ? _libraryManager.RootFolder : user.RootFolder;
            }
            else if (string.Equals(request.IncludeItemTypes, "BoxSet", StringComparison.OrdinalIgnoreCase))
            {
                item = user == null ? _libraryManager.RootFolder : user.RootFolder;
            }

            // Default list type = children

            var folder = item as Folder;
            if (folder == null)
            {
                folder = user == null ? _libraryManager.RootFolder : _libraryManager.GetUserRootFolder();
            }

            if (!string.IsNullOrEmpty(request.Ids))
            {
                request.Recursive = true;
                var query = GetItemsQuery(request, user);
                var result = await folder.GetItems(query).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(request.SortBy))
                {
                    var ids = query.ItemIds.ToList();

                    // Try to preserve order
                    result.Items = result.Items.OrderBy(i => ids.IndexOf(i.Id.ToString("N"))).ToArray();
                }

                return result;
            }

            if (request.Recursive)
            {
                return await folder.GetItems(GetItemsQuery(request, user)).ConfigureAwait(false);
            }

            if (user == null)
            {
                return await folder.GetItems(GetItemsQuery(request, null)).ConfigureAwait(false);
            }

            var userRoot = item as UserRootFolder;

            if (userRoot == null)
            {
                return await folder.GetItems(GetItemsQuery(request, user)).ConfigureAwait(false);
            }

            IEnumerable<BaseItem> items = folder.GetChildren(user, true);

            var itemsArray = items.ToArray();

            return new QueryResult<BaseItem>
            {
                Items = itemsArray,
                TotalRecordCount = itemsArray.Length
            };
        }

        private InternalItemsQuery GetItemsQuery(GetItems request, User user)
        {
            var query = new InternalItemsQuery(user)
            {
                IsPlayed = request.IsPlayed,
                MediaTypes = request.GetMediaTypes(),
                IncludeItemTypes = request.GetIncludeItemTypes(),
                ExcludeItemTypes = request.GetExcludeItemTypes(),
                Recursive = request.Recursive,
                SortBy = request.GetOrderBy(),
                SortOrder = request.SortOrder ?? SortOrder.Ascending,

                IsFavorite = request.IsFavorite,
                Limit = request.Limit,
                StartIndex = request.StartIndex,
                IsMissing = request.IsMissing,
                IsVirtualUnaired = request.IsVirtualUnaired,
                IsUnaired = request.IsUnaired,
                CollapseBoxSetItems = request.CollapseBoxSetItems,
                NameLessThan = request.NameLessThan,
                NameStartsWith = request.NameStartsWith,
                NameStartsWithOrGreater = request.NameStartsWithOrGreater,
                HasImdbId = request.HasImdbId,
                IsPlaceHolder = request.IsPlaceHolder,
                IsLocked = request.IsLocked,
                IsInBoxSet = request.IsInBoxSet,
                IsHD = request.IsHD,
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
                Tags = request.GetTags(),
                OfficialRatings = request.GetOfficialRatings(),
                Genres = request.GetGenres(),
                GenreIds = request.GetGenreIds(),
                Studios = request.GetStudios(),
                StudioIds = request.GetStudioIds(),
                Person = request.Person,
                PersonIds = request.GetPersonIds(),
                PersonTypes = request.GetPersonTypes(),
                Years = request.GetYears(),
                ImageTypes = request.GetImageTypes().ToArray(),
                VideoTypes = request.GetVideoTypes().ToArray(),
                AdjacentTo = request.AdjacentTo,
                ItemIds = request.GetItemIds(),
                MinPlayers = request.MinPlayers,
                MaxPlayers = request.MaxPlayers,
                MinCommunityRating = request.MinCommunityRating,
                MinCriticRating = request.MinCriticRating,
                ParentId = string.IsNullOrWhiteSpace(request.ParentId) ? (Guid?)null : new Guid(request.ParentId),
                ParentIndexNumber = request.ParentIndexNumber,
                AiredDuringSeason = request.AiredDuringSeason,
                AlbumArtistStartsWithOrGreater = request.AlbumArtistStartsWithOrGreater,
                EnableTotalRecordCount = request.EnableTotalRecordCount,
                ExcludeItemIds = request.GetExcludeItemIds()
            };

            if (!string.IsNullOrWhiteSpace(request.Ids))
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
                    case ItemFilter.IsRecentlyAdded:
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

            // Filter by Series AirDays
            if (!string.IsNullOrEmpty(request.AirDays))
            {
                query.AirDays = request.AirDays.Split(',').Select(d => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), d, true)).ToArray();
            }

            // ExcludeLocationTypes
            if (!string.IsNullOrEmpty(request.ExcludeLocationTypes))
            {
                query.ExcludeLocationTypes = request.ExcludeLocationTypes.Split(',').Select(d => (LocationType)Enum.Parse(typeof(LocationType), d, true)).ToArray();
            }

            if (!string.IsNullOrEmpty(request.LocationTypes))
            {
                query.LocationTypes = request.LocationTypes.Split(',').Select(d => (LocationType)Enum.Parse(typeof(LocationType), d, true)).ToArray();
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
            if (!string.IsNullOrEmpty(request.ArtistIds))
            {
                var artistIds = request.ArtistIds.Split(new[] { '|', ',' });

                var artistItems = artistIds.Select(_libraryManager.GetItemById).Where(i => i != null).ToList();
                query.ArtistNames = artistItems.Select(i => i.Name).ToArray();
            }

            // Artists
            if (!string.IsNullOrEmpty(request.Artists))
            {
                query.ArtistNames = request.Artists.Split('|');
            }

            // ExcludeArtistIds
            if (!string.IsNullOrEmpty(request.ExcludeArtistIds))
            {
                query.ExcludeArtistIds = request.ExcludeArtistIds.Split('|');
            }

            // Albums
            if (!string.IsNullOrEmpty(request.Albums))
            {
                query.AlbumNames = request.Albums.Split('|');
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
