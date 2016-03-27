using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Search;
using ServiceStack;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetSearchHints
    /// </summary>
    [Route("/Search/Hints", "GET", Summary = "Gets search hints based on a search term")]
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
        public string UserId { get; set; }

        /// <summary>
        /// Search characters used to find items
        /// </summary>
        /// <value>The index by.</value>
        [ApiMember(Name = "SearchTerm", Description = "The search term to filter on", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SearchTerm { get; set; }


        [ApiMember(Name = "IncludePeople", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool IncludePeople { get; set; }

        [ApiMember(Name = "IncludeMedia", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool IncludeMedia { get; set; }

        [ApiMember(Name = "IncludeGenres", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool IncludeGenres { get; set; }

        [ApiMember(Name = "IncludeStudios", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool IncludeStudios { get; set; }

        [ApiMember(Name = "IncludeArtists", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool IncludeArtists { get; set; }

        [ApiMember(Name = "IncludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string IncludeItemTypes { get; set; }

        public GetSearchHints()
        {
            IncludeArtists = true;
            IncludeGenres = true;
            IncludeMedia = true;
            IncludePeople = true;
            IncludeStudios = true;
        }
    }

    /// <summary>
    /// Class SearchService
    /// </summary>
    [Authenticated]
    public class SearchService : BaseApiService
    {
        /// <summary>
        /// The _search engine
        /// </summary>
        private readonly ISearchEngine _searchEngine;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchService" /> class.
        /// </summary>
        /// <param name="searchEngine">The search engine.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="dtoService">The dto service.</param>
        /// <param name="imageProcessor">The image processor.</param>
        public SearchService(ISearchEngine searchEngine, ILibraryManager libraryManager, IDtoService dtoService, IImageProcessor imageProcessor)
        {
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
        public async Task<object> Get(GetSearchHints request)
        {
            var result = await GetSearchHintsAsync(request).ConfigureAwait(false);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the search hints async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{IEnumerable{SearchHintResult}}.</returns>
        private async Task<SearchHintResult> GetSearchHintsAsync(GetSearchHints request)
        {
            var result = await _searchEngine.GetSearchHints(new SearchQuery
            {
                Limit = request.Limit,
                SearchTerm = request.SearchTerm,
                IncludeArtists = request.IncludeArtists,
                IncludeGenres = request.IncludeGenres,
                IncludeMedia = request.IncludeMedia,
                IncludePeople = request.IncludePeople,
                IncludeStudios = request.IncludeStudios,
                StartIndex = request.StartIndex,
                UserId = request.UserId,
                IncludeItemTypes = (request.IncludeItemTypes ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray()

            }).ConfigureAwait(false);

            return new SearchHintResult
            {
                TotalRecordCount = result.TotalRecordCount,

                SearchHints = result.Items.Select(GetSearchHintResult).ToArray()
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
                Type = item.GetClientTypeName(),
                MediaType = item.MediaType,
                MatchedTerm = hintInfo.MatchedTerm,
                DisplayMediaType = item.DisplayMediaType,
                RunTimeTicks = item.RunTimeTicks,
                ProductionYear = item.ProductionYear
            };

            result.ChannelId = item.ChannelId;

            var primaryImageTag = _imageProcessor.GetImageCacheTag(item, ImageType.Primary);

            if (primaryImageTag != null)
            {
                result.PrimaryImageTag = primaryImageTag;
                result.PrimaryImageAspectRatio = _dtoService.GetPrimaryImageAspectRatio(item);
            }

            SetThumbImageInfo(result, item);
            SetBackdropImageInfo(result, item);

            var hasSeries = item as IHasSeries;
            if (hasSeries != null)
            {
                result.Series = hasSeries.SeriesName;
            }

            var season = item as Season;
            if (season != null)
            {
                result.EpisodeCount = season.GetRecursiveChildren(i => i is Episode).Count;
            }

            var series = item as Series;
            if (series != null)
            {
                result.EpisodeCount = series.GetRecursiveChildren(i => i is Episode).Count;
            }

            var album = item as MusicAlbum;

            if (album != null)
            {
                result.SongCount = album.Tracks.Count();

                result.Artists = album.Artists.ToArray();
                result.AlbumArtist = album.AlbumArtist;
            }

            var song = item as Audio;

            if (song != null)
            {
                result.Album = song.Album;
                result.AlbumArtist = song.AlbumArtists.FirstOrDefault();
                result.Artists = song.Artists.ToArray();
            }

            if (!string.IsNullOrWhiteSpace(item.ChannelId))
            {
                var channel = _libraryManager.GetItemById(item.ChannelId);
                result.ChannelName = channel == null ? null : channel.Name;
            }

            return result;
        }

        private void SetThumbImageInfo(SearchHint hint, BaseItem item)
        {
            var itemWithImage = item.HasImage(ImageType.Thumb) ? item : null;

            if (itemWithImage == null)
            {
                if (item is Episode)
                {
                    itemWithImage = GetParentWithImage<Series>(item, ImageType.Thumb);
                }
            }

            if (itemWithImage == null)
            {
                itemWithImage = GetParentWithImage<BaseItem>(item, ImageType.Thumb);
            }

            if (itemWithImage != null)
            {
                var tag = _imageProcessor.GetImageCacheTag(itemWithImage, ImageType.Thumb);

                if (tag != null)
                {
                    hint.ThumbImageTag = tag;
                    hint.ThumbImageItemId = itemWithImage.Id.ToString("N");
                }
            }
        }

        private void SetBackdropImageInfo(SearchHint hint, BaseItem item)
        {
            var itemWithImage = item.HasImage(ImageType.Backdrop) ? item : null;

            if (itemWithImage == null)
            {
                itemWithImage = GetParentWithImage<BaseItem>(item, ImageType.Backdrop);
            }

            if (itemWithImage != null)
            {
                var tag = _imageProcessor.GetImageCacheTag(itemWithImage, ImageType.Backdrop);

                if (tag != null)
                {
                    hint.BackdropImageTag = tag;
                    hint.BackdropImageItemId = itemWithImage.Id.ToString("N");
                }
            }
        }

        private T GetParentWithImage<T>(BaseItem item, ImageType type)
            where T : BaseItem
        {
            return item.GetParents().OfType<T>().FirstOrDefault(i => i.HasImage(type));
        }
    }
}
