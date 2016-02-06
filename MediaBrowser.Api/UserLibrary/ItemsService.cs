using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
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
        private readonly IUserDataManager _userDataRepository;

        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;

        private readonly IDtoService _dtoService;
        private readonly ICollectionManager _collectionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="localization">The localization.</param>
        /// <param name="dtoService">The dto service.</param>
        /// <param name="collectionManager">The collection manager.</param>
        public ItemsService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, ILocalizationManager localization, IDtoService dtoService, ICollectionManager collectionManager)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _localization = localization;
            _dtoService = dtoService;
            _collectionManager = collectionManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetItems request)
        {
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
            var parentItem = string.IsNullOrEmpty(request.ParentId) ? null : _libraryManager.GetItemById(request.ParentId);
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;

            var result = await GetItemsToSerialize(request, user, parentItem).ConfigureAwait(false);

            var dtoOptions = GetDtoOptions(request);

            return new ItemsResult
            {
                TotalRecordCount = result.Item1.TotalRecordCount,
                Items = _dtoService.GetBaseItemDtos(result.Item1.Items, dtoOptions, user).ToArray()
            };
        }

        /// <summary>
        /// Gets the items to serialize.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="user">The user.</param>
        /// <param name="parentItem">The parent item.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private async Task<Tuple<QueryResult<BaseItem>, bool>> GetItemsToSerialize(GetItems request, User user, BaseItem parentItem)
        {
            var item = string.IsNullOrEmpty(request.ParentId) ?
                user == null ? _libraryManager.RootFolder : user.RootFolder :
                parentItem;

            if (string.Equals(request.IncludeItemTypes, "Playlist", StringComparison.OrdinalIgnoreCase))
            {
                item = user == null ? _libraryManager.RootFolder : user.RootFolder;
            }
            else if (string.Equals(request.IncludeItemTypes, "BoxSet", StringComparison.OrdinalIgnoreCase))
            {
                item = user == null ? _libraryManager.RootFolder : user.RootFolder;
            }

            // Default list type = children

            if (!string.IsNullOrEmpty(request.Ids))
            {
                request.Recursive = true;
                var query = GetItemsQuery(request, user);
                var result = await ((Folder)item).GetItems(query).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(request.SortBy))
                {
                    var ids = query.ItemIds.ToList();

                    // Try to preserve order
                    result.Items = result.Items.OrderBy(i => ids.IndexOf(i.Id.ToString("N"))).ToArray();
                }

                return new Tuple<QueryResult<BaseItem>, bool>(result, true);
            }

            if (request.Recursive)
            {
                var result = await ((Folder)item).GetItems(GetItemsQuery(request, user)).ConfigureAwait(false);

                return new Tuple<QueryResult<BaseItem>, bool>(result, true);
            }

            if (user == null)
            {
                var result = await ((Folder)item).GetItems(GetItemsQuery(request, null)).ConfigureAwait(false);

                return new Tuple<QueryResult<BaseItem>, bool>(result, true);
            }

            var userRoot = item as UserRootFolder;

            if (userRoot == null)
            {
                var result = await ((Folder)item).GetItems(GetItemsQuery(request, user)).ConfigureAwait(false);

                return new Tuple<QueryResult<BaseItem>, bool>(result, true);
            }

            IEnumerable<BaseItem> items = ((Folder)item).GetChildren(user, true);

            var itemsArray = items.ToArray();

            return new Tuple<QueryResult<BaseItem>, bool>(new QueryResult<BaseItem>
            {
                Items = itemsArray,
                TotalRecordCount = itemsArray.Length

            }, false);
        }

        private InternalItemsQuery GetItemsQuery(GetItems request, User user)
        {
            var query = new InternalItemsQuery
            {
                User = user,
                IsPlayed = request.IsPlayed,
                MediaTypes = request.GetMediaTypes(),
                IncludeItemTypes = request.GetIncludeItemTypes(),
                ExcludeItemTypes = request.GetExcludeItemTypes(),
                Recursive = request.Recursive,
                SortBy = request.GetOrderBy(),
                SortOrder = request.SortOrder ?? SortOrder.Ascending,

                Filter = i => ApplyAdditionalFilters(request, i, user, _libraryManager),

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
                IsYearMismatched = request.IsYearMismatched,
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
                MinCriticRating = request.MinCriticRating
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

            return query;
        }

        /// <summary>
        /// Applies filtering
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="user">The user.</param>
        /// <param name="repository">The repository.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        internal static IEnumerable<BaseItem> ApplyFilter(IEnumerable<BaseItem> items, ItemFilter filter, User user, IUserDataManager repository)
        {
            // Avoid implicitly captured closure
            var currentUser = user;

            switch (filter)
            {
                case ItemFilter.IsFavoriteOrLikes:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        if (userdata == null)
                        {
                            return false;
                        }

                        var likes = userdata.Likes ?? false;
                        var favorite = userdata.IsFavorite;

                        return likes || favorite;
                    });

                case ItemFilter.Likes:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.Likes.HasValue && userdata.Likes.Value;
                    });

                case ItemFilter.Dislikes:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.Likes.HasValue && !userdata.Likes.Value;
                    });

                case ItemFilter.IsFavorite:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.IsFavorite;
                    });

                case ItemFilter.IsResumable:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.PlaybackPositionTicks > 0;
                    });

                case ItemFilter.IsPlayed:
                    return items.Where(item => item.IsPlayed(currentUser));

                case ItemFilter.IsUnplayed:
                    return items.Where(item => item.IsUnplayed(currentUser));

                case ItemFilter.IsFolder:
                    return items.Where(item => item.IsFolder);

                case ItemFilter.IsNotFolder:
                    return items.Where(item => !item.IsFolder);

                case ItemFilter.IsRecentlyAdded:
                    return items.Where(item => (DateTime.UtcNow - item.DateCreated).TotalDays <= 10);
            }

            return items;
        }

        private bool ApplyAdditionalFilters(GetItems request, BaseItem i, User user, ILibraryManager libraryManager)
        {
            // Artists
            if (!string.IsNullOrEmpty(request.ArtistIds))
            {
                var artistIds = request.ArtistIds.Split(new[] { '|', ',' });

                var audio = i as IHasArtist;

                if (!(audio != null && artistIds.Any(id =>
                {
                    var artistItem = libraryManager.GetItemById(id);
                    return artistItem != null && audio.HasAnyArtist(artistItem.Name);
                })))
                {
                    return false;
                }
            }

            // Artists
            if (!string.IsNullOrEmpty(request.Artists))
            {
                var artists = request.Artists.Split('|');

                var audio = i as IHasArtist;

                if (!(audio != null && artists.Any(audio.HasAnyArtist)))
                {
                    return false;
                }
            }

            // Albums
            if (!string.IsNullOrEmpty(request.Albums))
            {
                var albums = request.Albums.Split('|');

                var audio = i as Audio;

                if (audio != null)
                {
                    if (!albums.Any(a => string.Equals(a, audio.Album, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                var album = i as MusicAlbum;

                if (album != null)
                {
                    if (!albums.Any(a => string.Equals(a, album.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                var musicVideo = i as MusicVideo;

                if (musicVideo != null)
                {
                    if (!albums.Any(a => string.Equals(a, musicVideo.Album, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                return false;
            }

            // Min official rating
            if (!string.IsNullOrEmpty(request.MinOfficialRating))
            {
                var level = _localization.GetRatingLevel(request.MinOfficialRating);

                if (level.HasValue)
                {
                    var rating = i.CustomRating;

                    if (string.IsNullOrEmpty(rating))
                    {
                        rating = i.OfficialRating;
                    }

                    if (!string.IsNullOrEmpty(rating))
                    {
                        var itemLevel = _localization.GetRatingLevel(rating);

                        if (!(!itemLevel.HasValue || itemLevel.Value >= level.Value))
                        {
                            return false;
                        }
                    }
                }
            }

            // Max official rating
            if (!string.IsNullOrEmpty(request.MaxOfficialRating))
            {
                var level = _localization.GetRatingLevel(request.MaxOfficialRating);

                if (level.HasValue)
                {
                    var rating = i.CustomRating;

                    if (string.IsNullOrEmpty(rating))
                    {
                        rating = i.OfficialRating;
                    }

                    if (!string.IsNullOrEmpty(rating))
                    {
                        var itemLevel = _localization.GetRatingLevel(rating);

                        if (!(!itemLevel.HasValue || itemLevel.Value <= level.Value))
                        {
                            return false;
                        }
                    }
                }
            }

            // LocationTypes
            if (!string.IsNullOrEmpty(request.LocationTypes))
            {
                var vals = request.LocationTypes.Split(',');
                if (!vals.Contains(i.LocationType.ToString(), StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // ExcludeLocationTypes
            if (!string.IsNullOrEmpty(request.ExcludeLocationTypes))
            {
                var vals = request.ExcludeLocationTypes.Split(',');
                if (vals.Contains(i.LocationType.ToString(), StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(request.AlbumArtistStartsWithOrGreater))
            {
                var ok = new[] { i }.OfType<IHasAlbumArtist>()
                    .Any(p => string.Compare(request.AlbumArtistStartsWithOrGreater, p.AlbumArtists.FirstOrDefault(), StringComparison.CurrentCultureIgnoreCase) < 1);

                if (!ok)
                {
                    return false;
                }
            }

            // Filter by Series Status
            if (!string.IsNullOrEmpty(request.SeriesStatus))
            {
                var vals = request.SeriesStatus.Split(',');

                var ok = new[] { i }.OfType<Series>().Any(p => p.Status.HasValue && vals.Contains(p.Status.Value.ToString(), StringComparer.OrdinalIgnoreCase));

                if (!ok)
                {
                    return false;
                }
            }

            // Filter by Series AirDays
            if (!string.IsNullOrEmpty(request.AirDays))
            {
                var days = request.AirDays.Split(',').Select(d => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), d, true));

                var ok = new[] { i }.OfType<Series>().Any(p => p.AirDays != null && days.Any(d => p.AirDays.Contains(d)));

                if (!ok)
                {
                    return false;
                }
            }

            if (request.ParentIndexNumber.HasValue)
            {
                var filterValue = request.ParentIndexNumber.Value;

                var episode = i as Episode;

                if (episode != null)
                {
                    if (episode.ParentIndexNumber.HasValue && episode.ParentIndexNumber.Value != filterValue)
                    {
                        return false;
                    }
                }

                var song = i as Audio;

                if (song != null)
                {
                    if (song.ParentIndexNumber.HasValue && song.ParentIndexNumber.Value != filterValue)
                    {
                        return false;
                    }
                }
            }

            if (request.AiredDuringSeason.HasValue)
            {
                var episode = i as Episode;

                if (episode == null)
                {
                    return false;
                }

                if (!Series.FilterEpisodesBySeason(new[] { episode }, request.AiredDuringSeason.Value, true).Any())
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(request.MinPremiereDate))
            {
                var date = DateTime.Parse(request.MinPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();

                if (!(i.PremiereDate.HasValue && i.PremiereDate.Value >= date))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(request.MaxPremiereDate))
            {
                var date = DateTime.Parse(request.MaxPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();

                if (!(i.PremiereDate.HasValue && i.PremiereDate.Value <= date))
                {
                    return false;
                }
            }

            return true;
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
