using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api
{
    [Route("/Channels", "GET", Summary = "Gets available channels")]
    public class GetChannels : IReturn<QueryResult<BaseItemDto>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

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

        [ApiMember(Name = "SupportsLatestItems", Description = "Optional. Filter by channels that support getting latest items.", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? SupportsLatestItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>null</c> if [is favorite] contains no value, <c>true</c> if [is favorite]; otherwise, <c>false</c>.</value>
        public bool? IsFavorite { get; set; }
    }

    [Route("/Channels/{Id}/Features", "GET", Summary = "Gets features for a channel")]
    public class GetChannelFeatures : IReturn<ChannelFeatures>
    {
        [ApiMember(Name = "Id", Description = "Channel Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Channels/Features", "GET", Summary = "Gets features for a channel")]
    public class GetAllChannelFeatures : IReturn<List<ChannelFeatures>>
    {
    }

    [Route("/Channels/{Id}/Items", "GET", Summary = "Gets channel items")]
    public class GetChannelItems : IReturn<QueryResult<BaseItemDto>>, IHasItemFields
    {
        [ApiMember(Name = "Id", Description = "Channel Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "FolderId", Description = "Folder Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string FolderId { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

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

        [ApiMember(Name = "SortOrder", Description = "Sort Order - Ascending,Descending", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public SortOrder? SortOrder { get; set; }

        [ApiMember(Name = "Filters", Description = "Optional. Specify additional filters to apply. This allows multiple, comma delimeted. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Filters { get; set; }

        [ApiMember(Name = "SortBy", Description = "Optional. Specify one or more sort orders, comma delimeted. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SortBy { get; set; }

        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }
        
        /// <summary>
        /// Gets the filters.
        /// </summary>
        /// <returns>IEnumerable{ItemFilter}.</returns>
        public IEnumerable<ItemFilter> GetFilters()
        {
            var val = Filters;

            if (string.IsNullOrEmpty(val))
            {
                return new ItemFilter[] { };
            }

            return val.Split(',').Select(v => (ItemFilter)Enum.Parse(typeof(ItemFilter), v, true));
        }
    }

    [Route("/Channels/Items/Latest", "GET", Summary = "Gets channel items")]
    public class GetLatestChannelItems : IReturn<QueryResult<BaseItemDto>>, IHasItemFields
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

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

        [ApiMember(Name = "Filters", Description = "Optional. Specify additional filters to apply. This allows multiple, comma delimeted. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Filters { get; set; }

        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "ChannelIds", Description = "Optional. Specify one or more channel id's, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ChannelIds { get; set; }
        
        /// <summary>
        /// Gets the filters.
        /// </summary>
        /// <returns>IEnumerable{ItemFilter}.</returns>
        public IEnumerable<ItemFilter> GetFilters()
        {
            var val = Filters;

            if (string.IsNullOrEmpty(val))
            {
                return new ItemFilter[] { };
            }

            return val.Split(',').Select(v => (ItemFilter)Enum.Parse(typeof(ItemFilter), v, true));
        }
    }
    
    [Route("/Channels/Folder", "GET", Summary = "Gets the users channel folder, along with configured images")]
    public class GetChannelFolder : IReturn<BaseItemDto>
    {
        [ApiMember(Name = "UserId", Description = "Optional attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Authenticated]
    public class ChannelService : BaseApiService
    {
        private readonly IChannelManager _channelManager;

        public ChannelService(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public object Get(GetAllChannelFeatures request)
        {
            var result = _channelManager.GetAllChannelFeatures().ToList();

            return ToOptimizedResult(result);
        }

        public object Get(GetChannelFeatures request)
        {
            var result = _channelManager.GetChannelFeatures(request.Id);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetChannelFolder request)
        {
            return ToOptimizedResult(await _channelManager.GetChannelFolder(request.UserId, CancellationToken.None).ConfigureAwait(false));
        }

        public async Task<object> Get(GetChannels request)
        {
            var result = await _channelManager.GetChannels(new ChannelQuery
            {
                Limit = request.Limit,
                StartIndex = request.StartIndex,
                UserId = request.UserId,
                SupportsLatestItems = request.SupportsLatestItems,
                IsFavorite = request.IsFavorite

            }, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetChannelItems request)
        {
            var result = await _channelManager.GetChannelItems(new ChannelItemQuery
            {
                Limit = request.Limit,
                StartIndex = request.StartIndex,
                UserId = request.UserId,
                ChannelId = request.Id,
                FolderId = request.FolderId,
                SortOrder = request.SortOrder,
                SortBy = (request.SortBy ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray(),
                Filters = request.GetFilters().ToArray(),
                Fields = request.GetItemFields().ToArray()

            }, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetLatestChannelItems request)
        {
            var result = await _channelManager.GetLatestChannelItems(new AllChannelMediaQuery
            {
                Limit = request.Limit,
                StartIndex = request.StartIndex,
                ChannelIds = (request.ChannelIds ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray(),
                UserId = request.UserId,
                Filters = request.GetFilters().ToArray(),
                Fields = request.GetItemFields().ToList()

            }, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }
    }
}
