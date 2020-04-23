using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MediaBrowser.Api.LiveTv
{
    /// <summary>
    /// This is insecure right now to avoid windows phone refactoring
    /// </summary>
    [Route("/LiveTv/Info", "GET", Summary = "Gets available live tv services.")]
    [Authenticated]
    public class GetLiveTvInfo : IReturn<LiveTvInfo>
    {
    }

    [Route("/LiveTv/Channels", "GET", Summary = "Gets available live tv channels.")]
    [Authenticated]
    public class GetChannels : IReturn<QueryResult<BaseItemDto>>, IHasDtoOptions
    {
        [ApiMember(Name = "Type", Description = "Optional filter by channel type.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public ChannelType? Type { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional filter by user and attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

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

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        [ApiMember(Name = "IsFavorite", Description = "Filter by channels that are favorites, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsFavorite { get; set; }

        [ApiMember(Name = "IsLiked", Description = "Filter by channels that are liked, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsLiked { get; set; }

        [ApiMember(Name = "IsDisliked", Description = "Filter by channels that are disliked, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsDisliked { get; set; }

        [ApiMember(Name = "EnableFavoriteSorting", Description = "Incorporate favorite and like status into channel sorting.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool EnableFavoriteSorting { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "AddCurrentProgram", Description = "Optional. Adds current program info to each channel", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool AddCurrentProgram { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

        public string SortBy { get; set; }

        public SortOrder? SortOrder { get; set; }

        /// <summary>
        /// Gets the order by.
        /// </summary>
        /// <returns>IEnumerable{ItemSortBy}.</returns>
        public string[] GetOrderBy()
        {
            var val = SortBy;

            if (string.IsNullOrEmpty(val))
            {
                return Array.Empty<string>();
            }

            return val.Split(',');
        }

        public GetChannels()
        {
            AddCurrentProgram = true;
        }
    }

    [Route("/LiveTv/Channels/{Id}", "GET", Summary = "Gets a live tv channel")]
    [Authenticated]
    public class GetChannel : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Channel Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    [Route("/LiveTv/Recordings", "GET", Summary = "Gets live tv recordings")]
    [Authenticated]
    public class GetRecordings : IReturn<QueryResult<BaseItemDto>>, IHasDtoOptions
    {
        [ApiMember(Name = "ChannelId", Description = "Optional filter by channel id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ChannelId { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional filter by user and attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        [ApiMember(Name = "Status", Description = "Optional filter by recording status.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public RecordingStatus? Status { get; set; }

        [ApiMember(Name = "Status", Description = "Optional filter by recordings that are in progress, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsInProgress { get; set; }

        [ApiMember(Name = "SeriesTimerId", Description = "Optional filter by recordings belonging to a series timer", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SeriesTimerId { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        public bool EnableTotalRecordCount { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

        public bool? IsMovie { get; set; }
        public bool? IsSeries { get; set; }
        public bool? IsKids { get; set; }
        public bool? IsSports { get; set; }
        public bool? IsNews { get; set; }
        public bool? IsLibraryItem { get; set; }

        public GetRecordings()
        {
            EnableTotalRecordCount = true;
        }
    }

    [Route("/LiveTv/Recordings/Series", "GET", Summary = "Gets live tv recordings")]
    [Authenticated]
    public class GetRecordingSeries : IReturn<QueryResult<BaseItemDto>>, IHasDtoOptions
    {
        [ApiMember(Name = "ChannelId", Description = "Optional filter by channel id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ChannelId { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional filter by user and attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "GroupId", Description = "Optional filter by recording group.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string GroupId { get; set; }

        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        [ApiMember(Name = "Status", Description = "Optional filter by recording status.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public RecordingStatus? Status { get; set; }

        [ApiMember(Name = "Status", Description = "Optional filter by recordings that are in progress, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsInProgress { get; set; }

        [ApiMember(Name = "SeriesTimerId", Description = "Optional filter by recordings belonging to a series timer", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SeriesTimerId { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        public bool EnableTotalRecordCount { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

        public GetRecordingSeries()
        {
            EnableTotalRecordCount = true;
        }
    }

    [Route("/LiveTv/Recordings/Groups", "GET", Summary = "Gets live tv recording groups")]
    [Authenticated]
    public class GetRecordingGroups : IReturn<QueryResult<BaseItemDto>>
    {
        [ApiMember(Name = "UserId", Description = "Optional filter by user and attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/LiveTv/Recordings/Folders", "GET", Summary = "Gets recording folders")]
    [Authenticated]
    public class GetRecordingFolders : IReturn<BaseItemDto[]>
    {
        [ApiMember(Name = "UserId", Description = "Optional filter by user and attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    [Route("/LiveTv/Recordings/{Id}", "GET", Summary = "Gets a live tv recording")]
    [Authenticated]
    public class GetRecording : IReturn<BaseItemDto>
    {
        [ApiMember(Name = "Id", Description = "Recording Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    [Route("/LiveTv/Tuners/{Id}/Reset", "POST", Summary = "Resets a tv tuner")]
    [Authenticated]
    public class ResetTuner : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Tuner Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Timers/{Id}", "GET", Summary = "Gets a live tv timer")]
    [Authenticated]
    public class GetTimer : IReturn<TimerInfoDto>
    {
        [ApiMember(Name = "Id", Description = "Timer Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Timers/Defaults", "GET", Summary = "Gets default values for a new timer")]
    [Authenticated]
    public class GetDefaultTimer : IReturn<SeriesTimerInfoDto>
    {
        [ApiMember(Name = "ProgramId", Description = "Optional, to attach default values based on a program.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ProgramId { get; set; }
    }

    [Route("/LiveTv/Timers", "GET", Summary = "Gets live tv timers")]
    [Authenticated]
    public class GetTimers : IReturn<QueryResult<TimerInfoDto>>
    {
        [ApiMember(Name = "ChannelId", Description = "Optional filter by channel id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ChannelId { get; set; }

        [ApiMember(Name = "SeriesTimerId", Description = "Optional filter by timers belonging to a series timer", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SeriesTimerId { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsScheduled { get; set; }
    }

    [Route("/LiveTv/Programs", "GET,POST", Summary = "Gets available live tv epgs..")]
    [Authenticated]
    public class GetPrograms : IReturn<QueryResult<BaseItemDto>>, IHasDtoOptions
    {
        [ApiMember(Name = "ChannelIds", Description = "The channels to return guide information for.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public string ChannelIds { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional filter by user id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "MinStartDate", Description = "Optional. The minimum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public string MinStartDate { get; set; }

        [ApiMember(Name = "HasAired", Description = "Optional. Filter by programs that have completed airing, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasAired { get; set; }
        public bool? IsAiring { get; set; }

        [ApiMember(Name = "MaxStartDate", Description = "Optional. The maximum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public string MaxStartDate { get; set; }

        [ApiMember(Name = "MinEndDate", Description = "Optional. The minimum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public string MinEndDate { get; set; }

        [ApiMember(Name = "MaxEndDate", Description = "Optional. The maximum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public string MaxEndDate { get; set; }

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

        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        [ApiMember(Name = "SortBy", Description = "Optional. Specify one or more sort orders, comma delimeted. Options: Name, StartDate", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SortBy { get; set; }

        [ApiMember(Name = "SortOrder", Description = "Sort Order - Ascending,Descending", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SortOrder { get; set; }

        [ApiMember(Name = "Genres", Description = "The genres to return guide information for.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public string Genres { get; set; }

        [ApiMember(Name = "GenreIds", Description = "The genres to return guide information for.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public string GenreIds { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        public bool EnableTotalRecordCount { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

        public string SeriesTimerId { get; set; }
        public Guid LibrarySeriesId { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        public GetPrograms()
        {
            EnableTotalRecordCount = true;
        }
    }

    [Route("/LiveTv/Programs/Recommended", "GET", Summary = "Gets available live tv epgs..")]
    [Authenticated]
    public class GetRecommendedPrograms : IReturn<QueryResult<BaseItemDto>>, IHasDtoOptions
    {
        public bool EnableTotalRecordCount { get; set; }

        public GetRecommendedPrograms()
        {
            EnableTotalRecordCount = true;
        }

        [ApiMember(Name = "UserId", Description = "Optional filter by user id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        [ApiMember(Name = "IsAiring", Description = "Optional. Filter by programs that are currently airing, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsAiring { get; set; }

        [ApiMember(Name = "HasAired", Description = "Optional. Filter by programs that have completed airing, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasAired { get; set; }

        [ApiMember(Name = "IsSeries", Description = "Optional filter for movies.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsSeries { get; set; }

        [ApiMember(Name = "IsMovie", Description = "Optional filter for movies.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsMovie { get; set; }

        [ApiMember(Name = "IsNews", Description = "Optional filter for news.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsNews { get; set; }

        [ApiMember(Name = "IsKids", Description = "Optional filter for kids.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsKids { get; set; }

        [ApiMember(Name = "IsSports", Description = "Optional filter for sports.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET,POST")]
        public bool? IsSports { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        [ApiMember(Name = "GenreIds", Description = "The genres to return guide information for.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public string GenreIds { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }
    }

    [Route("/LiveTv/Programs/{Id}", "GET", Summary = "Gets a live tv program")]
    [Authenticated]
    public class GetProgram : IReturn<BaseItemDto>
    {
        [ApiMember(Name = "Id", Description = "Program Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }
    }


    [Route("/LiveTv/Recordings/{Id}", "DELETE", Summary = "Deletes a live tv recording")]
    [Authenticated]
    public class DeleteRecording : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Recording Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid Id { get; set; }
    }

    [Route("/LiveTv/Timers/{Id}", "DELETE", Summary = "Cancels a live tv timer")]
    [Authenticated]
    public class CancelTimer : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Timer Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Timers/{Id}", "POST", Summary = "Updates a live tv timer")]
    [Authenticated]
    public class UpdateTimer : TimerInfoDto, IReturnVoid
    {
    }

    [Route("/LiveTv/Timers", "POST", Summary = "Creates a live tv timer")]
    [Authenticated]
    public class CreateTimer : TimerInfoDto, IReturnVoid
    {
    }

    [Route("/LiveTv/SeriesTimers/{Id}", "GET", Summary = "Gets a live tv series timer")]
    [Authenticated]
    public class GetSeriesTimer : IReturn<TimerInfoDto>
    {
        [ApiMember(Name = "Id", Description = "Timer Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/SeriesTimers", "GET", Summary = "Gets live tv series timers")]
    [Authenticated]
    public class GetSeriesTimers : IReturn<QueryResult<SeriesTimerInfoDto>>
    {
        [ApiMember(Name = "SortBy", Description = "Optional. Sort by SortName or Priority", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public string SortBy { get; set; }

        [ApiMember(Name = "SortOrder", Description = "Optional. Sort in Ascending or Descending order", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET,POST")]
        public SortOrder SortOrder { get; set; }
    }

    [Route("/LiveTv/SeriesTimers/{Id}", "DELETE", Summary = "Cancels a live tv series timer")]
    [Authenticated]
    public class CancelSeriesTimer : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Timer Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/SeriesTimers/{Id}", "POST", Summary = "Updates a live tv series timer")]
    [Authenticated]
    public class UpdateSeriesTimer : SeriesTimerInfoDto, IReturnVoid
    {
    }

    [Route("/LiveTv/SeriesTimers", "POST", Summary = "Creates a live tv series timer")]
    [Authenticated]
    public class CreateSeriesTimer : SeriesTimerInfoDto, IReturnVoid
    {
    }

    [Route("/LiveTv/Recordings/Groups/{Id}", "GET", Summary = "Gets a recording group")]
    [Authenticated]
    public class GetRecordingGroup : IReturn<BaseItemDto>
    {
        [ApiMember(Name = "Id", Description = "Recording group Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/GuideInfo", "GET", Summary = "Gets guide info")]
    [Authenticated]
    public class GetGuideInfo : IReturn<GuideInfo>
    {
    }

    [Route("/LiveTv/TunerHosts", "POST", Summary = "Adds a tuner host")]
    [Authenticated]
    public class AddTunerHost : TunerHostInfo, IReturn<TunerHostInfo>
    {
    }

    [Route("/LiveTv/TunerHosts", "DELETE", Summary = "Deletes a tuner host")]
    [Authenticated]
    public class DeleteTunerHost : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Tuner host id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/ListingProviders/Default", "GET")]
    [Authenticated]
    public class GetDefaultListingProvider : ListingsProviderInfo, IReturn<ListingsProviderInfo>
    {
    }

    [Route("/LiveTv/ListingProviders", "POST", Summary = "Adds a listing provider")]
    [Authenticated]
    public class AddListingProvider : ListingsProviderInfo, IReturn<ListingsProviderInfo>
    {
        public bool ValidateLogin { get; set; }
        public bool ValidateListings { get; set; }
        public string Pw { get; set; }
    }

    [Route("/LiveTv/ListingProviders", "DELETE", Summary = "Deletes a listing provider")]
    [Authenticated]
    public class DeleteListingProvider : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Provider id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/ListingProviders/Lineups", "GET", Summary = "Gets available lineups")]
    [Authenticated]
    public class GetLineups : IReturn<List<NameIdPair>>
    {
        [ApiMember(Name = "Id", Description = "Provider id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Type", Description = "Provider Type", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Type { get; set; }

        [ApiMember(Name = "Location", Description = "Location", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Location { get; set; }

        [ApiMember(Name = "Country", Description = "Country", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Country { get; set; }
    }

    [Route("/LiveTv/ListingProviders/SchedulesDirect/Countries", "GET", Summary = "Gets available lineups")]
    [Authenticated]
    public class GetSchedulesDirectCountries
    {
    }

    [Route("/LiveTv/ChannelMappingOptions")]
    [Authenticated]
    public class GetChannelMappingOptions
    {
        [ApiMember(Name = "Id", Description = "Provider id", IsRequired = true, DataType = "string", ParameterType = "query")]
        public string ProviderId { get; set; }
    }

    [Route("/LiveTv/ChannelMappings")]
    [Authenticated]
    public class SetChannelMapping
    {
        [ApiMember(Name = "Id", Description = "Provider id", IsRequired = true, DataType = "string", ParameterType = "query")]
        public string ProviderId { get; set; }
        public string TunerChannelId { get; set; }
        public string ProviderChannelId { get; set; }
    }

    public class ChannelMappingOptions
    {
        public List<TunerChannelMapping> TunerChannels { get; set; }
        public List<NameIdPair> ProviderChannels { get; set; }
        public NameValuePair[] Mappings { get; set; }
        public string ProviderName { get; set; }
    }

    [Route("/LiveTv/LiveStreamFiles/{Id}/stream.{Container}", "GET", Summary = "Gets a live tv channel")]
    public class GetLiveStreamFile
    {
        public string Id { get; set; }
        public string Container { get; set; }
    }

    [Route("/LiveTv/LiveRecordings/{Id}/stream", "GET", Summary = "Gets a live tv channel")]
    public class GetLiveRecordingFile
    {
        public string Id { get; set; }
    }

    [Route("/LiveTv/TunerHosts/Types", "GET")]
    [Authenticated]
    public class GetTunerHostTypes : IReturn<List<NameIdPair>>
    {

    }

    [Route("/LiveTv/Tuners/Discvover", "GET")]
    [Authenticated]
    public class DiscoverTuners : IReturn<List<TunerHostInfo>>
    {
        public bool NewDevicesOnly { get; set; }
    }

    public class LiveTvService : BaseApiService
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IUserManager _userManager;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;
        private readonly ISessionContext _sessionContext;
        private readonly IStreamHelper _streamHelper;
        private readonly IMediaSourceManager _mediaSourceManager;

        public LiveTvService(
            ILogger<LiveTvService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IMediaSourceManager mediaSourceManager,
            IStreamHelper streamHelper,
            ILiveTvManager liveTvManager,
            IUserManager userManager,
            IHttpClient httpClient,
            ILibraryManager libraryManager,
            IDtoService dtoService,
            IAuthorizationContext authContext,
            ISessionContext sessionContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _mediaSourceManager = mediaSourceManager;
            _streamHelper = streamHelper;
            _liveTvManager = liveTvManager;
            _userManager = userManager;
            _httpClient = httpClient;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
            _authContext = authContext;
            _sessionContext = sessionContext;
        }

        public object Get(GetTunerHostTypes request)
        {
            var list = _liveTvManager.GetTunerHostTypes();
            return ToOptimizedResult(list);
        }

        public object Get(GetRecordingFolders request)
        {
            var user = request.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(request.UserId);
            var folders = _liveTvManager.GetRecordingFolders(user);

            var returnArray = _dtoService.GetBaseItemDtos(folders, new DtoOptions(), user);

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnArray,
                TotalRecordCount = returnArray.Count
            };

            return ToOptimizedResult(result);
        }

        public object Get(GetLiveRecordingFile request)
        {
            var path = _liveTvManager.GetEmbyTvActiveRecordingPath(request.Id);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new FileNotFoundException();
            }

            var outputHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [HeaderNames.ContentType] = Model.Net.MimeTypes.GetMimeType(path)
            };

            return new ProgressiveFileCopier(_streamHelper, path, outputHeaders, Logger)
            {
                AllowEndOfFile = false
            };
        }

        public async Task<object> Get(DiscoverTuners request)
        {
            var result = await _liveTvManager.DiscoverTuners(request.NewDevicesOnly, CancellationToken.None).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetLiveStreamFile request)
        {
            var liveStreamInfo = await _mediaSourceManager.GetDirectStreamProviderByUniqueId(request.Id, CancellationToken.None).ConfigureAwait(false);

            var directStreamProvider = liveStreamInfo;

            var outputHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [HeaderNames.ContentType] = Model.Net.MimeTypes.GetMimeType("file." + request.Container)
            };

            return new ProgressiveFileCopier(directStreamProvider, _streamHelper, outputHeaders, Logger)
            {
                AllowEndOfFile = false
            };
        }

        public object Get(GetDefaultListingProvider request)
        {
            return ToOptimizedResult(new ListingsProviderInfo());
        }

        public async Task<object> Post(SetChannelMapping request)
        {
            return await _liveTvManager.SetChannelMapping(request.ProviderId, request.TunerChannelId, request.ProviderChannelId).ConfigureAwait(false);
        }

        public async Task<object> Get(GetChannelMappingOptions request)
        {
            var config = GetConfiguration();

            var listingsProviderInfo = config.ListingProviders.First(i => string.Equals(request.ProviderId, i.Id, StringComparison.OrdinalIgnoreCase));

            var listingsProviderName = _liveTvManager.ListingProviders.First(i => string.Equals(i.Type, listingsProviderInfo.Type, StringComparison.OrdinalIgnoreCase)).Name;

            var tunerChannels = await _liveTvManager.GetChannelsForListingsProvider(request.ProviderId, CancellationToken.None)
                        .ConfigureAwait(false);

            var providerChannels = await _liveTvManager.GetChannelsFromListingsProviderData(request.ProviderId, CancellationToken.None)
                     .ConfigureAwait(false);

            var mappings = listingsProviderInfo.ChannelMappings;

            var result = new ChannelMappingOptions
            {
                TunerChannels = tunerChannels.Select(i => _liveTvManager.GetTunerChannelMapping(i, mappings, providerChannels)).ToList(),

                ProviderChannels = providerChannels.Select(i => new NameIdPair
                {
                    Name = i.Name,
                    Id = i.Id

                }).ToList(),

                Mappings = mappings,

                ProviderName = listingsProviderName
            };

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetSchedulesDirectCountries request)
        {
            // https://json.schedulesdirect.org/20141201/available/countries

            var response = await _httpClient.Get(new HttpRequestOptions
            {
                Url = "https://json.schedulesdirect.org/20141201/available/countries",
                BufferContent = false

            }).ConfigureAwait(false);

            return ResultFactory.GetResult(Request, response, "application/json");
        }

        private void AssertUserCanManageLiveTv()
        {
            var user = _sessionContext.GetUser(Request);

            if (user == null)
            {
                throw new SecurityException("Anonymous live tv management is not allowed.");
            }

            if (!user.Policy.EnableLiveTvManagement)
            {
                throw new SecurityException("The current user does not have permission to manage live tv.");
            }
        }

        public async Task<object> Post(AddListingProvider request)
        {
            if (request.Pw != null)
            {
                request.Password = GetHashedString(request.Pw);
            }

            request.Pw = null;

            var result = await _liveTvManager.SaveListingProvider(request, request.ValidateLogin, request.ValidateListings).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the hashed string.
        /// </summary>
        private string GetHashedString(string str)
        {
            // SchedulesDirect requires a SHA1 hash of the user's password
            // https://github.com/SchedulesDirect/JSON-Service/wiki/API-20141201#obtain-a-token
            using SHA1 sha = SHA1.Create();

            return Hex.Encode(
                sha.ComputeHash(Encoding.UTF8.GetBytes(str)));
        }

        public void Delete(DeleteListingProvider request)
        {
            _liveTvManager.DeleteListingsProvider(request.Id);
        }

        public async Task<object> Post(AddTunerHost request)
        {
            var result = await _liveTvManager.SaveTunerHost(request).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        public void Delete(DeleteTunerHost request)
        {
            var config = GetConfiguration();

            config.TunerHosts = config.TunerHosts.Where(i => !string.Equals(request.Id, i.Id, StringComparison.OrdinalIgnoreCase)).ToArray();

            ServerConfigurationManager.SaveConfiguration("livetv", config);
        }

        private LiveTvOptions GetConfiguration()
        {
            return ServerConfigurationManager.GetConfiguration<LiveTvOptions>("livetv");
        }

        private void UpdateConfiguration(LiveTvOptions options)
        {
            ServerConfigurationManager.SaveConfiguration("livetv", options);
        }

        public async Task<object> Get(GetLineups request)
        {
            var info = await _liveTvManager.GetLineups(request.Type, request.Id, request.Country, request.Location).ConfigureAwait(false);

            return ToOptimizedResult(info);
        }

        public object Get(GetLiveTvInfo request)
        {
            var info = _liveTvManager.GetLiveTvInfo(CancellationToken.None);

            return ToOptimizedResult(info);
        }

        public object Get(GetChannels request)
        {
            var options = GetDtoOptions(_authContext, request);

            var channelResult = _liveTvManager.GetInternalChannels(new LiveTvChannelQuery
            {
                ChannelType = request.Type,
                UserId = request.UserId,
                StartIndex = request.StartIndex,
                Limit = request.Limit,
                IsFavorite = request.IsFavorite,
                IsLiked = request.IsLiked,
                IsDisliked = request.IsDisliked,
                EnableFavoriteSorting = request.EnableFavoriteSorting,
                IsMovie = request.IsMovie,
                IsSeries = request.IsSeries,
                IsNews = request.IsNews,
                IsKids = request.IsKids,
                IsSports = request.IsSports,
                SortBy = request.GetOrderBy(),
                SortOrder = request.SortOrder ?? SortOrder.Ascending,
                AddCurrentProgram = request.AddCurrentProgram

            }, options, CancellationToken.None);

            var user = request.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(request.UserId);

            RemoveFields(options);

            options.AddCurrentProgram = request.AddCurrentProgram;

            var returnArray = _dtoService.GetBaseItemDtos(channelResult.Items, options, user);

            var result = new QueryResult<BaseItemDto>
            {
                Items = returnArray,
                TotalRecordCount = channelResult.TotalRecordCount
            };

            return ToOptimizedResult(result);
        }

        private void RemoveFields(DtoOptions options)
        {
            var fields = options.Fields.ToList();

            fields.Remove(ItemFields.CanDelete);
            fields.Remove(ItemFields.CanDownload);
            fields.Remove(ItemFields.DisplayPreferencesId);
            fields.Remove(ItemFields.Etag);
            options.Fields = fields.ToArray();
        }

        public object Get(GetChannel request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? _libraryManager.GetUserRootFolder() : _libraryManager.GetItemById(request.Id);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var result = _dtoService.GetBaseItemDto(item, dtoOptions, user);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetPrograms request)
        {
            var user = request.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(request.UserId);

            var query = new InternalItemsQuery(user)
            {
                ChannelIds = ApiEntryPoint.Split(request.ChannelIds, ',', true).Select(i => new Guid(i)).ToArray(),
                HasAired = request.HasAired,
                IsAiring = request.IsAiring,
                EnableTotalRecordCount = request.EnableTotalRecordCount
            };

            if (!string.IsNullOrEmpty(request.MinStartDate))
            {
                query.MinStartDate = DateTime.Parse(request.MinStartDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MinEndDate))
            {
                query.MinEndDate = DateTime.Parse(request.MinEndDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MaxStartDate))
            {
                query.MaxStartDate = DateTime.Parse(request.MaxStartDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MaxEndDate))
            {
                query.MaxEndDate = DateTime.Parse(request.MaxEndDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            query.StartIndex = request.StartIndex;
            query.Limit = request.Limit;
            query.OrderBy = BaseItemsRequest.GetOrderBy(request.SortBy, request.SortOrder);
            query.IsNews = request.IsNews;
            query.IsMovie = request.IsMovie;
            query.IsSeries = request.IsSeries;
            query.IsKids = request.IsKids;
            query.IsSports = request.IsSports;
            query.SeriesTimerId = request.SeriesTimerId;
            query.Genres = (request.Genres ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            query.GenreIds = GetGuids(request.GenreIds);

            if (!request.LibrarySeriesId.Equals(Guid.Empty))
            {
                query.IsSeries = true;

                if (_libraryManager.GetItemById(request.LibrarySeriesId) is Series series)
                {
                    query.Name = series.Name;
                }
            }

            var result = await _liveTvManager.GetPrograms(query, GetDtoOptions(_authContext, request), CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public object Get(GetRecommendedPrograms request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var query = new InternalItemsQuery(user)
            {
                IsAiring = request.IsAiring,
                Limit = request.Limit,
                HasAired = request.HasAired,
                IsSeries = request.IsSeries,
                IsMovie = request.IsMovie,
                IsKids = request.IsKids,
                IsNews = request.IsNews,
                IsSports = request.IsSports,
                EnableTotalRecordCount = request.EnableTotalRecordCount
            };

            query.GenreIds = GetGuids(request.GenreIds);

            var result = _liveTvManager.GetRecommendedPrograms(query, GetDtoOptions(_authContext, request), CancellationToken.None);

            return ToOptimizedResult(result);
        }

        public object Post(GetPrograms request)
        {
            return Get(request);
        }

        public object Get(GetRecordings request)
        {
            var options = GetDtoOptions(_authContext, request);

            var result = _liveTvManager.GetRecordings(new RecordingQuery
            {
                ChannelId = request.ChannelId,
                UserId = request.UserId,
                StartIndex = request.StartIndex,
                Limit = request.Limit,
                Status = request.Status,
                SeriesTimerId = request.SeriesTimerId,
                IsInProgress = request.IsInProgress,
                EnableTotalRecordCount = request.EnableTotalRecordCount,
                IsMovie = request.IsMovie,
                IsNews = request.IsNews,
                IsSeries = request.IsSeries,
                IsKids = request.IsKids,
                IsSports = request.IsSports,
                IsLibraryItem = request.IsLibraryItem,
                Fields = request.GetItemFields(),
                ImageTypeLimit = request.ImageTypeLimit,
                EnableImages = request.EnableImages

            }, options);

            return ToOptimizedResult(result);
        }

        public object Get(GetRecordingSeries request)
        {
            return ToOptimizedResult(new QueryResult<BaseItemDto>());
        }

        public object Get(GetRecording request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var item = string.IsNullOrEmpty(request.Id) ? _libraryManager.GetUserRootFolder() : _libraryManager.GetItemById(request.Id);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var result = _dtoService.GetBaseItemDto(item, dtoOptions, user);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetTimer request)
        {
            var result = await _liveTvManager.GetTimer(request.Id, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetTimers request)
        {
            var result = await _liveTvManager.GetTimers(new TimerQuery
            {
                ChannelId = request.ChannelId,
                SeriesTimerId = request.SeriesTimerId,
                IsActive = request.IsActive,
                IsScheduled = request.IsScheduled

            }, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public void Delete(DeleteRecording request)
        {
            AssertUserCanManageLiveTv();

            _libraryManager.DeleteItem(_libraryManager.GetItemById(request.Id), new DeleteOptions
            {
                DeleteFileLocation = false
            });
        }

        public Task Delete(CancelTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.CancelTimer(request.Id);
        }

        public Task Post(UpdateTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.UpdateTimer(request, CancellationToken.None);
        }

        public async Task<object> Get(GetSeriesTimers request)
        {
            var result = await _liveTvManager.GetSeriesTimers(new SeriesTimerQuery
            {
                SortOrder = request.SortOrder,
                SortBy = request.SortBy

            }, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetSeriesTimer request)
        {
            var result = await _liveTvManager.GetSeriesTimer(request.Id, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public Task Delete(CancelSeriesTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.CancelSeriesTimer(request.Id);
        }

        public Task Post(UpdateSeriesTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.UpdateSeriesTimer(request, CancellationToken.None);
        }

        public async Task<object> Get(GetDefaultTimer request)
        {
            if (string.IsNullOrEmpty(request.ProgramId))
            {
                var result = await _liveTvManager.GetNewTimerDefaults(CancellationToken.None).ConfigureAwait(false);

                return ToOptimizedResult(result);
            }
            else
            {
                var result = await _liveTvManager.GetNewTimerDefaults(request.ProgramId, CancellationToken.None).ConfigureAwait(false);

                return ToOptimizedResult(result);
            }
        }

        public async Task<object> Get(GetProgram request)
        {
            var user = request.UserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(request.UserId);

            var result = await _liveTvManager.GetProgram(request.Id, CancellationToken.None, user).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public Task Post(CreateSeriesTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.CreateSeriesTimer(request, CancellationToken.None);
        }

        public Task Post(CreateTimer request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.CreateTimer(request, CancellationToken.None);
        }

        public object Get(GetRecordingGroups request)
        {
            return ToOptimizedResult(new QueryResult<BaseItemDto>());
        }

        public object Get(GetRecordingGroup request)
        {
            throw new FileNotFoundException();
        }

        public object Get(GetGuideInfo request)
        {
            return ToOptimizedResult(_liveTvManager.GetGuideInfo());
        }

        public Task Post(ResetTuner request)
        {
            AssertUserCanManageLiveTv();

            return _liveTvManager.ResetTuner(request.Id, CancellationToken.None);
        }
    }
}
