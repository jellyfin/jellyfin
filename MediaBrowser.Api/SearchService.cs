using System;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Search;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

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
        public Guid UserId { get; set; }

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

        [ApiMember(Name = "ExcludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ExcludeItemTypes { get; set; }

        [ApiMember(Name = "MediaTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string MediaTypes { get; set; }

        public string ParentId { get; set; }

        [ApiMember(Name = "IsMovie", Description = "Optional filter for movies.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsMovie { get; set; }

        [ApiMember(Name = "IsSeries", Description = "Optional filter for movies.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsSeries { get; set; }

        [ApiMember(Name = "IsNews", Description = "Optional filter for news.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsNews { get; set; }

        [ApiMember(Name = "IsKids", Description = "Optional filter for kids.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsKids { get; set; }

        [ApiMember(Name = "IsSports", Description = "Optional filter for sports.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsSports { get; set; }

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
        public SearchService(
            ILogger<SearchService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ISearchEngine searchEngine,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            IImageProcessor imageProcessor)
            : base(logger, serverConfigurationManager, httpResultFactory)
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
        public object Get(GetSearchHints request)
        {
            var result = GetSearchHintsAsync(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the search hints async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{IEnumerable{SearchHintResult}}.</returns>
        private SearchHintResult GetSearchHintsAsync(GetSearchHints request)
        {
            var result = _searchEngine.GetSearchHints(new SearchQuery
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
                IncludeItemTypes = ApiEntryPoint.Split(request.IncludeItemTypes, ',', true),
                ExcludeItemTypes = ApiEntryPoint.Split(request.ExcludeItemTypes, ',', true),
                MediaTypes = ApiEntryPoint.Split(request.MediaTypes, ',', true),
                ParentId = request.ParentId,

                IsKids = request.IsKids,
                IsMovie = request.IsMovie,
                IsNews = request.IsNews,
                IsSeries = request.IsSeries,
                IsSports = request.IsSports

            });

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
                Id = item.Id,
                Type = item.GetClientTypeName(),
                MediaType = item.MediaType,
                MatchedTerm = hintInfo.MatchedTerm,
                RunTimeTicks = item.RunTimeTicks,
                ProductionYear = item.ProductionYear,
                ChannelId = item.ChannelId,
                EndDate = item.EndDate
            };

            // legacy
            result.ItemId = result.Id;

            if (item.IsFolder)
            {
                result.IsFolder = true;
            }

            var primaryImageTag = _imageProcessor.GetImageCacheTag(item, ImageType.Primary);

            if (primaryImageTag != null)
            {
                result.PrimaryImageTag = primaryImageTag;
                result.PrimaryImageAspectRatio = _dtoService.GetPrimaryImageAspectRatio(item);
            }

            SetThumbImageInfo(result, item);
            SetBackdropImageInfo(result, item);

            switch (item)
            {
                case IHasSeries hasSeries:
                    result.Series = hasSeries.SeriesName;
                    break;
                case LiveTvProgram program:
                    result.StartDate = program.StartDate;
                    break;
                case Series series:
                    if (series.Status.HasValue)
                    {
                        result.Status = series.Status.Value.ToString();
                    }

                    break;
                case MusicAlbum album:
                    result.Artists = album.Artists;
                    result.AlbumArtist = album.AlbumArtist;
                    break;
                case Audio song:
                    result.AlbumArtist = song.AlbumArtists.FirstOrDefault();
                    result.Artists = song.Artists;

                    MusicAlbum musicAlbum = song.AlbumEntity;

                    if (musicAlbum != null)
                    {
                        result.Album = musicAlbum.Name;
                        result.AlbumId = musicAlbum.Id;
                    }
                    else
                    {
                        result.Album = song.Album;
                    }

                    break;
            }

            if (!item.ChannelId.Equals(Guid.Empty))
            {
                var channel = _libraryManager.GetItemById(item.ChannelId);
                result.ChannelName = channel?.Name;
            }

            return result;
        }

        private void SetThumbImageInfo(SearchHint hint, BaseItem item)
        {
            var itemWithImage = item.HasImage(ImageType.Thumb) ? item : null;

            if (itemWithImage == null && item is Episode)
            {
                itemWithImage = GetParentWithImage<Series>(item, ImageType.Thumb);
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
                    hint.ThumbImageItemId = itemWithImage.Id.ToString("N", CultureInfo.InvariantCulture);
                }
            }
        }

        private void SetBackdropImageInfo(SearchHint hint, BaseItem item)
        {
            var itemWithImage = (item.HasImage(ImageType.Backdrop) ? item : null)
                ?? GetParentWithImage<BaseItem>(item, ImageType.Backdrop);

            if (itemWithImage != null)
            {
                var tag = _imageProcessor.GetImageCacheTag(itemWithImage, ImageType.Backdrop);

                if (tag != null)
                {
                    hint.BackdropImageTag = tag;
                    hint.BackdropImageItemId = itemWithImage.Id.ToString("N", CultureInfo.InvariantCulture);
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
