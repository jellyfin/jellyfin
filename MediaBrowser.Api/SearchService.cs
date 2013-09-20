using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Search;
using ServiceStack.ServiceHost;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSearchHints
    /// </summary>
    [Route("/Search/Hints", "GET")]
    [Api(Description = "Gets search hints based on a search term")]
    public class GetSearchHints : IReturn<SearchHintResult>
    {
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

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Supply a user id to search within a user's library or omit to search all.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Search characters used to find items
        /// </summary>
        /// <value>The index by.</value>
        [ApiMember(Name = "SearchTerm", Description = "The search term to filter on", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SearchTerm { get; set; }
    }

    /// <summary>
    /// Class SearchService
    /// </summary>
    public class SearchService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;
        /// <summary>
        /// The _search engine
        /// </summary>
        private readonly ILibrarySearchEngine _searchEngine;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="searchEngine">The search engine.</param>
        /// <param name="libraryManager">The library manager.</param>
        public SearchService(IUserManager userManager, ILibrarySearchEngine searchEngine, ILibraryManager libraryManager, IDtoService dtoService, IImageProcessor imageProcessor)
        {
            _userManager = userManager;
            _searchEngine = searchEngine;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetSearchHints request)
        {
            var result = GetSearchHintsAsync(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the search hints async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{IEnumerable{SearchHintResult}}.</returns>
        private async Task<SearchHintResult> GetSearchHintsAsync(GetSearchHints request)
        {
            IEnumerable<BaseItem> inputItems;

            if (request.UserId.HasValue)
            {
                var user = _userManager.GetUserById(request.UserId.Value);

                inputItems = user.RootFolder.GetRecursiveChildren(user);
            }
            else
            {
                inputItems = _libraryManager.RootFolder.RecursiveChildren;
            }

            var results = await _searchEngine.GetSearchHints(inputItems, request.SearchTerm).ConfigureAwait(false);

            var searchResultArray = results.ToList();

            IEnumerable<SearchHintInfo> returnResults = searchResultArray;

            if (request.StartIndex.HasValue)
            {
                returnResults = returnResults.Skip(request.StartIndex.Value);
            }

            if (request.Limit.HasValue)
            {
                returnResults = returnResults.Take(request.Limit.Value);
            }

            return new SearchHintResult
            {
                TotalRecordCount = searchResultArray.Count,

                SearchHints = returnResults.Select(GetSearchHintResult).ToArray()
            };
        }

        /// <summary>
        /// Gets the search hint result.
        /// </summary>
        /// <param name="hintInfo">The hint info.</param>
        /// <returns>SearchHintResult.</returns>
        private SearchHint GetSearchHintResult(SearchHintInfo hintInfo)
        {
            var item = hintInfo.Item;

            var result = new SearchHint
            {
                Name = item.Name,
                IndexNumber = item.IndexNumber,
                ParentIndexNumber = item.ParentIndexNumber,
                ItemId = _dtoService.GetDtoId(item),
                Type = item.GetType().Name,
                MediaType = item.MediaType,
                MatchedTerm = hintInfo.MatchedTerm,
                DisplayMediaType = item.DisplayMediaType,
                RunTimeTicks = item.RunTimeTicks
            };

            if (item.HasImage(ImageType.Primary))
            {
                result.PrimaryImageTag = _imageProcessor.GetImageCacheTag(item, ImageType.Primary, item.GetImage(ImageType.Primary));
            }

            var episode = item as Episode;

            if (episode != null)
            {
                result.Series = episode.Series.Name;
            }

            var season = item as Season;

            if (season != null)
            {
                result.Series = season.Series.Name;

                result.EpisodeCount = season.RecursiveChildren.OfType<Episode>().Count();
            }

            var series = item as Series;

            if (series != null)
            {
                result.EpisodeCount = series.RecursiveChildren.OfType<Episode>().Count();
            }

            var album = item as MusicAlbum;

            if (album != null)
            {
                var songs = album.RecursiveChildren.OfType<Audio>().ToList();

                result.SongCount = songs.Count;
                
                result.Artists = songs
                    .SelectMany(i => i.Artists)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                result.AlbumArtist = songs.Select(i => i.AlbumArtist).FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            var song = item as Audio;

            if (song != null)
            {
                result.Album = song.Album;
                result.AlbumArtist = song.AlbumArtist;
                result.Artists = song.Artists.ToArray();
            }

            return result;
        }
    }
}
