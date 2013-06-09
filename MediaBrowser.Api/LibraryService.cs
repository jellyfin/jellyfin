using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetCriticReviews
    /// </summary>
    [Route("/Items/{Id}/CriticReviews", "GET")]
    [Api(Description = "Gets critic reviews for an item")]
    public class GetCriticReviews : IReturn<ItemReviewsResult>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }
    }

    /// <summary>
    /// Class GetThemeSongs
    /// </summary>
    [Route("/Items/{Id}/ThemeSongs", "GET")]
    [Api(Description = "Gets theme songs for an item")]
    public class GetThemeSongs : IReturn<ThemeSongsResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class GetThemeVideos
    /// </summary>
    [Route("/Items/{Id}/ThemeVideos", "GET")]
    [Api(Description = "Gets video backdrops for an item")]
    public class GetThemeVideos : IReturn<ThemeVideosResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Library/Refresh", "POST")]
    [Api(Description = "Starts a library scan")]
    public class RefreshLibrary : IReturnVoid
    {
    }

    [Route("/Items/{Id}/Refresh", "POST")]
    [Api(Description = "Refreshes metadata for an item")]
    public class RefreshItem : IReturnVoid
    {
        [ApiMember(Name = "Forced", Description = "Indicates if a normal or forced refresh should occur.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Forced { get; set; }

        [ApiMember(Name = "Recursive", Description = "Indicates if the refresh should occur recursively.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Recursive { get; set; }

        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    [Route("/Items/{Id}", "DELETE")]
    [Api(Description = "Deletes an item from the library and file system")]
    public class DeleteItem : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Items/Counts", "GET")]
    [Api(Description = "Gets counts of various item types")]
    public class GetItemCounts : IReturn<ItemCounts>
    {
        [ApiMember(Name = "UserId", Description = "Optional. Get counts from a specific user's library.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }
    }

    [Route("/Items/{ItemId}", "POST")]
    [Api(("Updates an item"))]
    public class UpdateItem : BaseItemDto, IReturnVoid
    {
        public string ItemId { get; set; }
    }

    /// <summary>
    /// Class LibraryService
    /// </summary>
    public class LibraryService : BaseApiService
    {
        /// <summary>
        /// The _item repo
        /// </summary>
        private readonly IItemRepository _itemRepo;

        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataRepository _userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryService" /> class.
        /// </summary>
        /// <param name="itemRepo">The item repo.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        public LibraryService(IItemRepository itemRepo, ILibraryManager libraryManager, IUserManager userManager,
                              IUserDataRepository userDataRepository)
        {
            _itemRepo = itemRepo;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCriticReviews request)
        {
            var result = GetCriticReviewsAsync(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemCounts request)
        {
            var items = GetItems(request.UserId).ToList();

            var counts = new ItemCounts
            {
                AlbumCount = items.OfType<MusicAlbum>().Count(),
                EpisodeCount = items.OfType<Episode>().Count(),
                GameCount = items.OfType<BaseGame>().Count(),
                MovieCount = items.OfType<Movie>().Count(),
                SeriesCount = items.OfType<Series>().Count(),
                SongCount = items.OfType<Audio>().Count(),
                TrailerCount = items.OfType<Trailer>().Count(),
                MusicVideoCount = items.OfType<MusicVideo>().Count()
            };

            return ToOptimizedResult(counts);
        }

        protected IEnumerable<BaseItem> GetItems(Guid? userId)
        {
            if (userId.HasValue)
            {
                var user = _userManager.GetUserById(userId.Value);

                return _userManager.GetUserById(userId.Value).RootFolder.GetRecursiveChildren(user);
            }

            return _libraryManager.RootFolder.RecursiveChildren;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public async void Post(RefreshLibrary request)
        {
            try
            {
                await
                    _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None)
                                   .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }

        public void Post(UpdateItem request)
        {
            var task = UpdateItem(request);

            Task.WaitAll(task);
        }

        private Task UpdateItem(UpdateItem request)
        {
            var item = DtoBuilder.GetItemByClientId(request.ItemId, _userManager, _libraryManager);

            item.Name = request.Name;
            item.ForcedSortName = request.SortName;
            item.DisplayMediaType = request.DisplayMediaType;
            item.CommunityRating = request.CommunityRating;
            item.HomePageUrl = request.HomePageUrl;
            item.Budget = request.Budget;
            item.Revenue = request.Revenue;
            item.CriticRating = request.CriticRating;
            item.CriticRatingSummary = request.CriticRatingSummary;
            item.IndexNumber = request.IndexNumber;
            item.ParentIndexNumber = request.ParentIndexNumber;
            item.Overview = request.Overview;
            item.Genres = request.Genres;
            item.Tags = request.Tags;
            item.Studios = request.Studios.Select(x=>x.Name).ToList();
            item.People = request.People.Select(x=> new PersonInfo{Name = x.Name,Role = x.Role,Type = x.Type}).ToList();

            item.EndDate = request.EndDate;
            item.PremiereDate = request.PremiereDate;
            item.ProductionYear = request.ProductionYear;
            item.AspectRatio = request.AspectRatio;
            item.Language = request.Language;
            item.OfficialRating = request.OfficialRating;
            item.CustomRating = request.CustomRating;


            foreach (var pair in request.ProviderIds.ToList())
            {
                if (string.IsNullOrEmpty(pair.Value))
                {
                    request.ProviderIds.Remove(pair.Key);
                }
            }

            item.ProviderIds = request.ProviderIds;

            var game = item as BaseGame;

            if (game != null)
            {
                game.PlayersSupported = request.Players;
            }

            var song = item as Audio;

            if (song != null)
            {
                song.Album = request.Album;
                song.AlbumArtist = request.AlbumArtist;
                song.Artist = request.Artists[0];
            }

            var musicAlbum = item as MusicAlbum;

            if (musicAlbum != null)
            {
                musicAlbum.MusicBrainzReleaseGroupId = request.ProviderIds["MusicBrainzReleaseGroupId"];
            }

            var series = item as Series;
            if (series != null)
            {
                series.Status = request.Status;
                series.AirDays = request.AirDays;
                series.AirTime = request.AirTime;
            }
            return _libraryManager.UpdateItem(item, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItem request)
        {
            var task = DeleteItem(request);

            Task.WaitAll(task);
        }

        private async Task DeleteItem(DeleteItem request)
        {
            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager);

            var parent = item.Parent;

            if (item.LocationType == LocationType.FileSystem)
            {
                if (Directory.Exists(item.Path))
                {
                    Directory.Delete(item.Path, true);
                }
                else if (File.Exists(item.Path))
                {
                    File.Delete(item.Path);
                }

                if (parent != null)
                {
                    try
                    {
                        await
                            parent.ValidateChildren(new Progress<double>(), CancellationToken.None)
                                  .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error refreshing item", ex);
                    }
                }
            }
            else if (parent != null)
            {
                try
                {
                    await parent.RemoveChild(item, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error removing item", ex);
                }
            }
            else
            {
                throw new InvalidOperationException("Don't know how to delete " + item.Name);
            }
        }

        /// <summary>
        /// Gets the critic reviews async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemReviewsResult}.</returns>
        private async Task<ItemReviewsResult> GetCriticReviewsAsync(GetCriticReviews request)
        {
            var reviews = await _itemRepo.GetCriticReviews(new Guid(request.Id)).ConfigureAwait(false);

            var reviewsArray = reviews.ToArray();

            var result = new ItemReviewsResult
                {
                    TotalRecordCount = reviewsArray.Length
                };

            if (request.StartIndex.HasValue)
            {
                reviewsArray = reviewsArray.Skip(request.StartIndex.Value).ToArray();
            }
            if (request.Limit.HasValue)
            {
                reviewsArray = reviewsArray.Take(request.Limit.Value).ToArray();
            }

            result.ItemReviews = reviewsArray;

            return result;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeSongs request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (request.UserId.HasValue
                                  ? user.RootFolder
                                  : (Folder)_libraryManager.RootFolder)
                           : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, request.UserId);

            // Get everything
            var fields =
                Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var items =
                _itemRepo.GetItems(item.ThemeSongIds)
                         .OrderBy(i => i.SortName)
                         .Select(i => dtoBuilder.GetBaseItemDto(i, fields, user))
                         .Select(t => t.Result)
                         .ToArray();

            var result = new ThemeSongsResult
                {
                    Items = items,
                    TotalRecordCount = items.Length,
                    OwnerId = DtoBuilder.GetClientItemId(item)
                };

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetThemeVideos request)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id)
                           ? (request.UserId.HasValue
                                  ? user.RootFolder
                                  : (Folder)_libraryManager.RootFolder)
                           : DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager, request.UserId);

            // Get everything
            var fields =
                Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

            var dtoBuilder = new DtoBuilder(Logger, _libraryManager, _userDataRepository);

            var items =
                _itemRepo.GetItems(item.ThemeVideoIds)
                         .OrderBy(i => i.SortName)
                         .Select(i => dtoBuilder.GetBaseItemDto(i, fields, user))
                         .Select(t => t.Result)
                         .ToArray();

            var result = new ThemeVideosResult
                {
                    Items = items,
                    TotalRecordCount = items.Length,
                    OwnerId = DtoBuilder.GetClientItemId(item)
                };

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RefreshItem request)
        {
            var task = RefreshItem(request);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Refreshes the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        private async Task RefreshItem(RefreshItem request)
        {
            var item = DtoBuilder.GetItemByClientId(request.Id, _userManager, _libraryManager);

            var folder = item as Folder;

            try
            {
                await item.RefreshMetadata(CancellationToken.None, forceRefresh: request.Forced).ConfigureAwait(false);

                if (folder != null)
                {
                    await folder.ValidateChildren(new Progress<double>(), CancellationToken.None, request.Recursive,
                                                request.Forced).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error refreshing library", ex);
            }
        }
    }
}
