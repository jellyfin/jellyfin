using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.LiveTvDtos;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Live tv controller.
/// </summary>
public class LiveTvController : BaseJellyfinApiController
{
    private readonly ILiveTvManager _liveTvManager;
    private readonly IGuideManager _guideManager;
    private readonly ITunerHostManager _tunerHostManager;
    private readonly IListingsManager _listingsManager;
    private readonly IRecordingsManager _recordingsManager;
    private readonly IUserManager _userManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IConfigurationManager _configurationManager;
    private readonly ITranscodeManager _transcodeManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveTvController"/> class.
    /// </summary>
    /// <param name="liveTvManager">Instance of the <see cref="ILiveTvManager"/> interface.</param>
    /// <param name="guideManager">Instance of the <see cref="IGuideManager"/> interface.</param>
    /// <param name="tunerHostManager">Instance of the <see cref="ITunerHostManager"/> interface.</param>
    /// <param name="listingsManager">Instance of the <see cref="IListingsManager"/> interface.</param>
    /// <param name="recordingsManager">Instance of the <see cref="IRecordingsManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
    /// <param name="transcodeManager">Instance of the <see cref="ITranscodeManager"/> interface.</param>
    public LiveTvController(
        ILiveTvManager liveTvManager,
        IGuideManager guideManager,
        ITunerHostManager tunerHostManager,
        IListingsManager listingsManager,
        IRecordingsManager recordingsManager,
        IUserManager userManager,
        IHttpClientFactory httpClientFactory,
        ILibraryManager libraryManager,
        IDtoService dtoService,
        IMediaSourceManager mediaSourceManager,
        IConfigurationManager configurationManager,
        ITranscodeManager transcodeManager)
    {
        _liveTvManager = liveTvManager;
        _guideManager = guideManager;
        _tunerHostManager = tunerHostManager;
        _listingsManager = listingsManager;
        _recordingsManager = recordingsManager;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
        _libraryManager = libraryManager;
        _dtoService = dtoService;
        _mediaSourceManager = mediaSourceManager;
        _configurationManager = configurationManager;
        _transcodeManager = transcodeManager;
    }

    /// <summary>
    /// Gets available live tv services.
    /// </summary>
    /// <response code="200">Available live tv services returned.</response>
    /// <returns>
    /// An <see cref="OkResult"/> containing the available live tv services.
    /// </returns>
    [HttpGet("Info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public ActionResult<LiveTvInfo> GetLiveTvInfo()
    {
        return _liveTvManager.GetLiveTvInfo(CancellationToken.None);
    }

    /// <summary>
    /// Gets available live tv channels.
    /// </summary>
    /// <param name="type">Optional. Filter by channel type.</param>
    /// <param name="userId">Optional. Filter by user and attach user data.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="isMovie">Optional. Filter for movies.</param>
    /// <param name="isSeries">Optional. Filter for series.</param>
    /// <param name="isNews">Optional. Filter for news.</param>
    /// <param name="isKids">Optional. Filter for kids.</param>
    /// <param name="isSports">Optional. Filter for sports.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="isFavorite">Optional. Filter by channels that are favorites, or not.</param>
    /// <param name="isLiked">Optional. Filter by channels that are liked, or not.</param>
    /// <param name="isDisliked">Optional. Filter by channels that are disliked, or not.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">"Optional. The image types to include in the output.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="sortBy">Optional. Key to sort by.</param>
    /// <param name="sortOrder">Optional. Sort order.</param>
    /// <param name="enableFavoriteSorting">Optional. Incorporate favorite and like status into channel sorting.</param>
    /// <param name="addCurrentProgram">Optional. Adds current program info to each channel.</param>
    /// <response code="200">Available live tv channels returned.</response>
    /// <returns>
    /// An <see cref="OkResult"/> containing the resulting available live tv channels.
    /// </returns>
    [HttpGet("Channels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public ActionResult<QueryResult<BaseItemDto>> GetLiveTvChannels(
        [FromQuery] ChannelType? type,
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] bool? isMovie,
        [FromQuery] bool? isSeries,
        [FromQuery] bool? isNews,
        [FromQuery] bool? isKids,
        [FromQuery] bool? isSports,
        [FromQuery] int? limit,
        [FromQuery] bool? isFavorite,
        [FromQuery] bool? isLiked,
        [FromQuery] bool? isDisliked,
        [FromQuery] bool? enableImages,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery] bool? enableUserData,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemSortBy[] sortBy,
        [FromQuery] SortOrder? sortOrder,
        [FromQuery] bool enableFavoriteSorting = false,
        [FromQuery] bool addCurrentProgram = true)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        var channelResult = _liveTvManager.GetInternalChannels(
            new LiveTvChannelQuery
            {
                ChannelType = type,
                UserId = userId.Value,
                StartIndex = startIndex,
                Limit = limit,
                IsFavorite = isFavorite,
                IsLiked = isLiked,
                IsDisliked = isDisliked,
                EnableFavoriteSorting = enableFavoriteSorting,
                IsMovie = isMovie,
                IsSeries = isSeries,
                IsNews = isNews,
                IsKids = isKids,
                IsSports = isSports,
                SortBy = sortBy,
                SortOrder = sortOrder ?? SortOrder.Ascending,
                AddCurrentProgram = addCurrentProgram
            },
            dtoOptions,
            CancellationToken.None);

        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var fieldsList = dtoOptions.Fields.ToList();
        fieldsList.Remove(ItemFields.CanDelete);
        fieldsList.Remove(ItemFields.CanDownload);
        fieldsList.Remove(ItemFields.DisplayPreferencesId);
        fieldsList.Remove(ItemFields.Etag);
        dtoOptions.Fields = fieldsList.ToArray();
        dtoOptions.AddCurrentProgram = addCurrentProgram;

        var returnArray = _dtoService.GetBaseItemDtos(channelResult.Items, dtoOptions, user);
        return new QueryResult<BaseItemDto>(
            startIndex,
            channelResult.TotalRecordCount,
            returnArray);
    }

    /// <summary>
    /// Gets a live tv channel.
    /// </summary>
    /// <param name="channelId">Channel id.</param>
    /// <param name="userId">Optional. Attach user data.</param>
    /// <response code="200">Live tv channel returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>An <see cref="OkResult"/> containing the live tv channel.</returns>
    [HttpGet("Channels/{channelId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public ActionResult<BaseItemDto> GetChannel([FromRoute, Required] Guid channelId, [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);
        var item = channelId.IsEmpty()
            ? _libraryManager.GetUserRootFolder()
            : _libraryManager.GetItemById<BaseItem>(channelId, user);

        if (item is null)
        {
            return NotFound();
        }

        var dtoOptions = new DtoOptions()
            .AddClientFields(User);
        return _dtoService.GetBaseItemDto(item, dtoOptions, user);
    }

    /// <summary>
    /// Gets live tv recordings.
    /// </summary>
    /// <param name="channelId">Optional. Filter by channel id.</param>
    /// <param name="userId">Optional. Filter by user and attach user data.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="status">Optional. Filter by recording status.</param>
    /// <param name="isInProgress">Optional. Filter by recordings that are in progress, or not.</param>
    /// <param name="seriesTimerId">Optional. Filter by recordings belonging to a series timer.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="isMovie">Optional. Filter for movies.</param>
    /// <param name="isSeries">Optional. Filter for series.</param>
    /// <param name="isKids">Optional. Filter for kids.</param>
    /// <param name="isSports">Optional. Filter for sports.</param>
    /// <param name="isNews">Optional. Filter for news.</param>
    /// <param name="isLibraryItem">Optional. Filter for is library item.</param>
    /// <param name="enableTotalRecordCount">Optional. Return total record count.</param>
    /// <response code="200">Live tv recordings returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the live tv recordings.</returns>
    [HttpGet("Recordings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetRecordings(
        [FromQuery] string? channelId,
        [FromQuery] Guid? userId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] RecordingStatus? status,
        [FromQuery] bool? isInProgress,
        [FromQuery] string? seriesTimerId,
        [FromQuery] bool? enableImages,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery] bool? enableUserData,
        [FromQuery] bool? isMovie,
        [FromQuery] bool? isSeries,
        [FromQuery] bool? isKids,
        [FromQuery] bool? isSports,
        [FromQuery] bool? isNews,
        [FromQuery] bool? isLibraryItem,
        [FromQuery] bool enableTotalRecordCount = true)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        return await _liveTvManager.GetRecordingsAsync(
            new RecordingQuery
            {
                ChannelId = channelId,
                UserId = userId.Value,
                StartIndex = startIndex,
                Limit = limit,
                Status = status,
                SeriesTimerId = seriesTimerId,
                IsInProgress = isInProgress,
                EnableTotalRecordCount = enableTotalRecordCount,
                IsMovie = isMovie,
                IsNews = isNews,
                IsSeries = isSeries,
                IsKids = isKids,
                IsSports = isSports,
                IsLibraryItem = isLibraryItem,
                Fields = fields,
                ImageTypeLimit = imageTypeLimit,
                EnableImages = enableImages
            },
            dtoOptions).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets live tv recording series.
    /// </summary>
    /// <param name="channelId">Optional. Filter by channel id.</param>
    /// <param name="userId">Optional. Filter by user and attach user data.</param>
    /// <param name="groupId">Optional. Filter by recording group.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="status">Optional. Filter by recording status.</param>
    /// <param name="isInProgress">Optional. Filter by recordings that are in progress, or not.</param>
    /// <param name="seriesTimerId">Optional. Filter by recordings belonging to a series timer.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="enableTotalRecordCount">Optional. Return total record count.</param>
    /// <response code="200">Live tv recordings returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the live tv recordings.</returns>
    [HttpGet("Recordings/Series")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [Obsolete("This endpoint is obsolete.")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "channelId", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "userId", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "groupId", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "startIndex", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "limit", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "status", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "isInProgress", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "seriesTimerId", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "enableImages", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "imageTypeLimit", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "enableImageTypes", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "fields", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "enableUserData", Justification = "Imported from ServiceStack")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "enableTotalRecordCount", Justification = "Imported from ServiceStack")]
    public ActionResult<QueryResult<BaseItemDto>> GetRecordingsSeries(
        [FromQuery] string? channelId,
        [FromQuery] Guid? userId,
        [FromQuery] string? groupId,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] RecordingStatus? status,
        [FromQuery] bool? isInProgress,
        [FromQuery] string? seriesTimerId,
        [FromQuery] bool? enableImages,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery] bool? enableUserData,
        [FromQuery] bool enableTotalRecordCount = true)
    {
        return new QueryResult<BaseItemDto>();
    }

    /// <summary>
    /// Gets live tv recording groups.
    /// </summary>
    /// <param name="userId">Optional. Filter by user and attach user data.</param>
    /// <response code="200">Recording groups returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the recording groups.</returns>
    [HttpGet("Recordings/Groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [Obsolete("This endpoint is obsolete.")]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "userId", Justification = "Imported from ServiceStack")]
    public ActionResult<QueryResult<BaseItemDto>> GetRecordingGroups([FromQuery] Guid? userId)
    {
        return new QueryResult<BaseItemDto>();
    }

    /// <summary>
    /// Gets recording folders.
    /// </summary>
    /// <param name="userId">Optional. Filter by user and attach user data.</param>
    /// <response code="200">Recording folders returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the recording folders.</returns>
    [HttpGet("Recordings/Folders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetRecordingFolders([FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);
        var folders = await _liveTvManager.GetRecordingFoldersAsync(user).ConfigureAwait(false);

        var returnArray = _dtoService.GetBaseItemDtos(folders, new DtoOptions(), user);

        return new QueryResult<BaseItemDto>(returnArray);
    }

    /// <summary>
    /// Gets a live tv recording.
    /// </summary>
    /// <param name="recordingId">Recording id.</param>
    /// <param name="userId">Optional. Attach user data.</param>
    /// <response code="200">Recording returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>An <see cref="OkResult"/> containing the live tv recording.</returns>
    [HttpGet("Recordings/{recordingId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public ActionResult<BaseItemDto> GetRecording([FromRoute, Required] Guid recordingId, [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);
        var item = recordingId.IsEmpty()
            ? _libraryManager.GetUserRootFolder()
            : _libraryManager.GetItemById<BaseItem>(recordingId, user);
        if (item is null)
        {
            return NotFound();
        }

        var dtoOptions = new DtoOptions()
            .AddClientFields(User);

        return _dtoService.GetBaseItemDto(item, dtoOptions, user);
    }

    /// <summary>
    /// Resets a tv tuner.
    /// </summary>
    /// <param name="tunerId">Tuner id.</param>
    /// <response code="204">Tuner reset.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Tuners/{tunerId}/Reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.LiveTvManagement)]
    public async Task<ActionResult> ResetTuner([FromRoute, Required] string tunerId)
    {
        await _liveTvManager.ResetTuner(tunerId, CancellationToken.None).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Gets a timer.
    /// </summary>
    /// <param name="timerId">Timer id.</param>
    /// <response code="200">Timer returned.</response>
    /// <returns>
    /// A <see cref="Task"/> containing an <see cref="OkResult"/> which contains the timer.
    /// </returns>
    [HttpGet("Timers/{timerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public async Task<ActionResult<TimerInfoDto>> GetTimer([FromRoute, Required] string timerId)
    {
        return await _liveTvManager.GetTimer(timerId, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the default values for a new timer.
    /// </summary>
    /// <param name="programId">Optional. To attach default values based on a program.</param>
    /// <response code="200">Default values returned.</response>
    /// <returns>
    /// A <see cref="Task"/> containing an <see cref="OkResult"/> which contains the default values for a timer.
    /// </returns>
    [HttpGet("Timers/Defaults")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public async Task<ActionResult<SeriesTimerInfoDto>> GetDefaultTimer([FromQuery] string? programId)
    {
        return string.IsNullOrEmpty(programId)
            ? await _liveTvManager.GetNewTimerDefaults(CancellationToken.None).ConfigureAwait(false)
            : await _liveTvManager.GetNewTimerDefaults(programId, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the live tv timers.
    /// </summary>
    /// <param name="channelId">Optional. Filter by channel id.</param>
    /// <param name="seriesTimerId">Optional. Filter by timers belonging to a series timer.</param>
    /// <param name="isActive">Optional. Filter by timers that are active.</param>
    /// <param name="isScheduled">Optional. Filter by timers that are scheduled.</param>
    /// <returns>
    /// A <see cref="Task"/> containing an <see cref="OkResult"/> which contains the live tv timers.
    /// </returns>
    [HttpGet("Timers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public async Task<ActionResult<QueryResult<TimerInfoDto>>> GetTimers(
        [FromQuery] string? channelId,
        [FromQuery] string? seriesTimerId,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isScheduled)
    {
        return await _liveTvManager.GetTimers(
            new TimerQuery
            {
                ChannelId = channelId,
                SeriesTimerId = seriesTimerId,
                IsActive = isActive,
                IsScheduled = isScheduled
            },
            CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available live tv epgs.
    /// </summary>
    /// <param name="channelIds">The channels to return guide information for.</param>
    /// <param name="userId">Optional. Filter by user id.</param>
    /// <param name="minStartDate">Optional. The minimum premiere start date.</param>
    /// <param name="hasAired">Optional. Filter by programs that have completed airing, or not.</param>
    /// <param name="isAiring">Optional. Filter by programs that are currently airing, or not.</param>
    /// <param name="maxStartDate">Optional. The maximum premiere start date.</param>
    /// <param name="minEndDate">Optional. The minimum premiere end date.</param>
    /// <param name="maxEndDate">Optional. The maximum premiere end date.</param>
    /// <param name="isMovie">Optional. Filter for movies.</param>
    /// <param name="isSeries">Optional. Filter for series.</param>
    /// <param name="isNews">Optional. Filter for news.</param>
    /// <param name="isKids">Optional. Filter for kids.</param>
    /// <param name="isSports">Optional. Filter for sports.</param>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="sortBy">Optional. Specify one or more sort orders, comma delimited. Options: Name, StartDate.</param>
    /// <param name="sortOrder">Sort Order - Ascending,Descending.</param>
    /// <param name="genres">The genres to return guide information for.</param>
    /// <param name="genreIds">The genre ids to return guide information for.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="enableUserData">Optional. Include user data.</param>
    /// <param name="seriesTimerId">Optional. Filter by series timer id.</param>
    /// <param name="librarySeriesId">Optional. Filter by library series id.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="enableTotalRecordCount">Retrieve total record count.</param>
    /// <response code="200">Live tv epgs returned.</response>
    /// <returns>
    /// A <see cref="Task"/> containing a <see cref="OkResult"/> which contains the live tv epgs.
    /// </returns>
    [HttpGet("Programs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetLiveTvPrograms(
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] channelIds,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? minStartDate,
        [FromQuery] bool? hasAired,
        [FromQuery] bool? isAiring,
        [FromQuery] DateTime? maxStartDate,
        [FromQuery] DateTime? minEndDate,
        [FromQuery] DateTime? maxEndDate,
        [FromQuery] bool? isMovie,
        [FromQuery] bool? isSeries,
        [FromQuery] bool? isNews,
        [FromQuery] bool? isKids,
        [FromQuery] bool? isSports,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemSortBy[] sortBy,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] SortOrder[] sortOrder,
        [FromQuery, ModelBinder(typeof(PipeDelimitedArrayModelBinder))] string[] genres,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] genreIds,
        [FromQuery] bool? enableImages,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] bool? enableUserData,
        [FromQuery] string? seriesTimerId,
        [FromQuery] Guid? librarySeriesId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery] bool enableTotalRecordCount = true)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var query = new InternalItemsQuery(user)
        {
            ChannelIds = channelIds,
            HasAired = hasAired,
            IsAiring = isAiring,
            EnableTotalRecordCount = enableTotalRecordCount,
            MinStartDate = minStartDate,
            MinEndDate = minEndDate,
            MaxStartDate = maxStartDate,
            MaxEndDate = maxEndDate,
            StartIndex = startIndex,
            Limit = limit,
            OrderBy = RequestHelpers.GetOrderBy(sortBy, sortOrder),
            IsNews = isNews,
            IsMovie = isMovie,
            IsSeries = isSeries,
            IsKids = isKids,
            IsSports = isSports,
            SeriesTimerId = seriesTimerId,
            Genres = genres,
            GenreIds = genreIds
        };

        if (!librarySeriesId.IsNullOrEmpty())
        {
            query.IsSeries = true;

            var series = _libraryManager.GetItemById<Series>(librarySeriesId.Value);
            if (series is not null)
            {
                query.Name = series.Name;
            }
        }

        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);
        return await _liveTvManager.GetPrograms(query, dtoOptions, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets available live tv epgs.
    /// </summary>
    /// <param name="body">Request body.</param>
    /// <response code="200">Live tv epgs returned.</response>
    /// <returns>
    /// A <see cref="Task"/> containing a <see cref="OkResult"/> which contains the live tv epgs.
    /// </returns>
    [HttpPost("Programs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = Policies.LiveTvAccess)]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetPrograms([FromBody] GetProgramsDto body)
    {
        var user = body.UserId.IsNullOrEmpty() ? null : _userManager.GetUserById(body.UserId.Value);

        var query = new InternalItemsQuery(user)
        {
            ChannelIds = body.ChannelIds ?? [],
            HasAired = body.HasAired,
            IsAiring = body.IsAiring,
            EnableTotalRecordCount = body.EnableTotalRecordCount,
            MinStartDate = body.MinStartDate,
            MinEndDate = body.MinEndDate,
            MaxStartDate = body.MaxStartDate,
            MaxEndDate = body.MaxEndDate,
            StartIndex = body.StartIndex,
            Limit = body.Limit,
            OrderBy = RequestHelpers.GetOrderBy(body.SortBy ?? [], body.SortOrder ?? []),
            IsNews = body.IsNews,
            IsMovie = body.IsMovie,
            IsSeries = body.IsSeries,
            IsKids = body.IsKids,
            IsSports = body.IsSports,
            SeriesTimerId = body.SeriesTimerId,
            Genres = body.Genres ?? [],
            GenreIds = body.GenreIds ?? []
        };

        if (!body.LibrarySeriesId.IsNullOrEmpty())
        {
            query.IsSeries = true;

            var series = _libraryManager.GetItemById<Series>(body.LibrarySeriesId.Value);
            if (series is not null)
            {
                query.Name = series.Name;
            }
        }

        var dtoOptions = new DtoOptions { Fields = body.Fields ?? [] }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(body.EnableImages, body.EnableUserData, body.ImageTypeLimit, body.EnableImageTypes ?? []);
        return await _liveTvManager.GetPrograms(query, dtoOptions, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets recommended live tv epgs.
    /// </summary>
    /// <param name="userId">Optional. filter by user id.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="isAiring">Optional. Filter by programs that are currently airing, or not.</param>
    /// <param name="hasAired">Optional. Filter by programs that have completed airing, or not.</param>
    /// <param name="isSeries">Optional. Filter for series.</param>
    /// <param name="isMovie">Optional. Filter for movies.</param>
    /// <param name="isNews">Optional. Filter for news.</param>
    /// <param name="isKids">Optional. Filter for kids.</param>
    /// <param name="isSports">Optional. Filter for sports.</param>
    /// <param name="enableImages">Optional. Include image information in output.</param>
    /// <param name="imageTypeLimit">Optional. The max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="genreIds">The genres to return guide information for.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="enableUserData">Optional. include user data.</param>
    /// <param name="enableTotalRecordCount">Retrieve total record count.</param>
    /// <response code="200">Recommended epgs returned.</response>
    /// <returns>A <see cref="OkResult"/> containing the queryresult of recommended epgs.</returns>
    [HttpGet("Programs/Recommended")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetRecommendedPrograms(
        [FromQuery] Guid? userId,
        [FromQuery] int? limit,
        [FromQuery] bool? isAiring,
        [FromQuery] bool? hasAired,
        [FromQuery] bool? isSeries,
        [FromQuery] bool? isMovie,
        [FromQuery] bool? isNews,
        [FromQuery] bool? isKids,
        [FromQuery] bool? isSports,
        [FromQuery] bool? enableImages,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] Guid[] genreIds,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery] bool? enableUserData,
        [FromQuery] bool enableTotalRecordCount = true)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var query = new InternalItemsQuery(user)
        {
            IsAiring = isAiring,
            Limit = limit,
            HasAired = hasAired,
            IsSeries = isSeries,
            IsMovie = isMovie,
            IsKids = isKids,
            IsNews = isNews,
            IsSports = isSports,
            EnableTotalRecordCount = enableTotalRecordCount,
            GenreIds = genreIds
        };

        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);
        return await _liveTvManager.GetRecommendedProgramsAsync(query, dtoOptions, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a live tv program.
    /// </summary>
    /// <param name="programId">Program id.</param>
    /// <param name="userId">Optional. Attach user data.</param>
    /// <response code="200">Program returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the livetv program.</returns>
    [HttpGet("Programs/{programId}")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseItemDto>> GetProgram(
        [FromRoute, Required] string programId,
        [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        return await _liveTvManager.GetProgram(programId, CancellationToken.None, user).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a live tv recording.
    /// </summary>
    /// <param name="recordingId">Recording id.</param>
    /// <response code="204">Recording deleted.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if item not found.</returns>
    [HttpDelete("Recordings/{recordingId}")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DeleteRecording([FromRoute, Required] Guid recordingId)
    {
        var item = _libraryManager.GetItemById<BaseItem>(recordingId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        _libraryManager.DeleteItem(item, new DeleteOptions
        {
            DeleteFileLocation = false
        });

        return NoContent();
    }

    /// <summary>
    /// Cancels a live tv timer.
    /// </summary>
    /// <param name="timerId">Timer id.</param>
    /// <response code="204">Timer deleted.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("Timers/{timerId}")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> CancelTimer([FromRoute, Required] string timerId)
    {
        await _liveTvManager.CancelTimer(timerId).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Updates a live tv timer.
    /// </summary>
    /// <param name="timerId">Timer id.</param>
    /// <param name="timerInfo">New timer info.</param>
    /// <response code="204">Timer updated.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Timers/{timerId}")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "timerId", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult> UpdateTimer([FromRoute, Required] string timerId, [FromBody] TimerInfoDto timerInfo)
    {
        await _liveTvManager.UpdateTimer(timerInfo, CancellationToken.None).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Creates a live tv timer.
    /// </summary>
    /// <param name="timerInfo">New timer info.</param>
    /// <response code="204">Timer created.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Timers")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> CreateTimer([FromBody] TimerInfoDto timerInfo)
    {
        await _liveTvManager.CreateTimer(timerInfo, CancellationToken.None).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Gets a live tv series timer.
    /// </summary>
    /// <param name="timerId">Timer id.</param>
    /// <response code="200">Series timer returned.</response>
    /// <response code="404">Series timer not found.</response>
    /// <returns>A <see cref="OkResult"/> on success, or a <see cref="NotFoundResult"/> if timer not found.</returns>
    [HttpGet("SeriesTimers/{timerId}")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeriesTimerInfoDto>> GetSeriesTimer([FromRoute, Required] string timerId)
    {
        var timer = await _liveTvManager.GetSeriesTimer(timerId, CancellationToken.None).ConfigureAwait(false);
        if (timer is null)
        {
            return NotFound();
        }

        return timer;
    }

    /// <summary>
    /// Gets live tv series timers.
    /// </summary>
    /// <param name="sortBy">Optional. Sort by SortName or Priority.</param>
    /// <param name="sortOrder">Optional. Sort in Ascending or Descending order.</param>
    /// <response code="200">Timers returned.</response>
    /// <returns>An <see cref="OkResult"/> of live tv series timers.</returns>
    [HttpGet("SeriesTimers")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryResult<SeriesTimerInfoDto>>> GetSeriesTimers([FromQuery] string? sortBy, [FromQuery] SortOrder? sortOrder)
    {
        return await _liveTvManager.GetSeriesTimers(
            new SeriesTimerQuery
            {
                SortOrder = sortOrder ?? SortOrder.Ascending,
                SortBy = sortBy
            },
            CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels a live tv series timer.
    /// </summary>
    /// <param name="timerId">Timer id.</param>
    /// <response code="204">Timer cancelled.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("SeriesTimers/{timerId}")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> CancelSeriesTimer([FromRoute, Required] string timerId)
    {
        await _liveTvManager.CancelSeriesTimer(timerId).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Updates a live tv series timer.
    /// </summary>
    /// <param name="timerId">Timer id.</param>
    /// <param name="seriesTimerInfo">New series timer info.</param>
    /// <response code="204">Series timer updated.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("SeriesTimers/{timerId}")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "timerId", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult> UpdateSeriesTimer([FromRoute, Required] string timerId, [FromBody] SeriesTimerInfoDto seriesTimerInfo)
    {
        await _liveTvManager.UpdateSeriesTimer(seriesTimerInfo, CancellationToken.None).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Creates a live tv series timer.
    /// </summary>
    /// <param name="seriesTimerInfo">New series timer info.</param>
    /// <response code="204">Series timer info created.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("SeriesTimers")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> CreateSeriesTimer([FromBody] SeriesTimerInfoDto seriesTimerInfo)
    {
        await _liveTvManager.CreateSeriesTimer(seriesTimerInfo, CancellationToken.None).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Get recording group.
    /// </summary>
    /// <param name="groupId">Group id.</param>
    /// <returns>A <see cref="NotFoundResult"/>.</returns>
    [HttpGet("Recordings/Groups/{groupId}")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Obsolete("This endpoint is obsolete.")]
    public ActionResult<BaseItemDto> GetRecordingGroup([FromRoute, Required] Guid groupId)
    {
        return NotFound();
    }

    /// <summary>
    /// Get guid info.
    /// </summary>
    /// <response code="200">Guid info returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the guide info.</returns>
    [HttpGet("GuideInfo")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<GuideInfo> GetGuideInfo()
        => _guideManager.GetGuideInfo();

    /// <summary>
    /// Adds a tuner host.
    /// </summary>
    /// <param name="tunerHostInfo">New tuner host.</param>
    /// <response code="200">Created tuner host returned.</response>
    /// <returns>A <see cref="OkResult"/> containing the created tuner host.</returns>
    [HttpPost("TunerHosts")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TunerHostInfo>> AddTunerHost([FromBody] TunerHostInfo tunerHostInfo)
        => await _tunerHostManager.SaveTunerHost(tunerHostInfo).ConfigureAwait(false);

    /// <summary>
    /// Deletes a tuner host.
    /// </summary>
    /// <param name="id">Tuner host id.</param>
    /// <response code="204">Tuner host deleted.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("TunerHosts")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult DeleteTunerHost([FromQuery] string? id)
    {
        var config = _configurationManager.GetConfiguration<LiveTvOptions>("livetv");
        config.TunerHosts = config.TunerHosts.Where(i => !string.Equals(id, i.Id, StringComparison.OrdinalIgnoreCase)).ToArray();
        _configurationManager.SaveConfiguration("livetv", config);
        return NoContent();
    }

    /// <summary>
    /// Gets default listings provider info.
    /// </summary>
    /// <response code="200">Default listings provider info returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the default listings provider info.</returns>
    [HttpGet("ListingProviders/Default")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ListingsProviderInfo> GetDefaultListingProvider()
    {
        return new ListingsProviderInfo();
    }

    /// <summary>
    /// Adds a listings provider.
    /// </summary>
    /// <param name="pw">Password.</param>
    /// <param name="listingsProviderInfo">New listings info.</param>
    /// <param name="validateListings">Validate listings.</param>
    /// <param name="validateLogin">Validate login.</param>
    /// <response code="200">Created listings provider returned.</response>
    /// <returns>A <see cref="OkResult"/> containing the created listings provider.</returns>
    [HttpPost("ListingProviders")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SuppressMessage("Microsoft.Performance", "CA5350:RemoveSha1", MessageId = "AddListingProvider", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult<ListingsProviderInfo>> AddListingProvider(
        [FromQuery] string? pw,
        [FromBody] ListingsProviderInfo listingsProviderInfo,
        [FromQuery] bool validateListings = false,
        [FromQuery] bool validateLogin = false)
    {
        if (!string.IsNullOrEmpty(pw))
        {
            // TODO: remove ToLower when Convert.ToHexString supports lowercase
            // Schedules Direct requires the hex to be lowercase
            listingsProviderInfo.Password = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(pw))).ToLowerInvariant();
        }

        return await _listingsManager.SaveListingProvider(listingsProviderInfo, validateLogin, validateListings).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete listing provider.
    /// </summary>
    /// <param name="id">Listing provider id.</param>
    /// <response code="204">Listing provider deleted.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("ListingProviders")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult DeleteListingProvider([FromQuery] string? id)
    {
        _listingsManager.DeleteListingsProvider(id);
        return NoContent();
    }

    /// <summary>
    /// Gets available lineups.
    /// </summary>
    /// <param name="id">Provider id.</param>
    /// <param name="type">Provider type.</param>
    /// <param name="location">Location.</param>
    /// <param name="country">Country.</param>
    /// <response code="200">Available lineups returned.</response>
    /// <returns>A <see cref="OkResult"/> containing the available lineups.</returns>
    [HttpGet("ListingProviders/Lineups")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NameIdPair>>> GetLineups(
        [FromQuery] string? id,
        [FromQuery] string? type,
        [FromQuery] string? location,
        [FromQuery] string? country)
        => await _listingsManager.GetLineups(type, id, country, location).ConfigureAwait(false);

    /// <summary>
    /// Gets available countries.
    /// </summary>
    /// <response code="200">Available countries returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the available countries.</returns>
    [HttpGet("ListingProviders/SchedulesDirect/Countries")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesFile(MediaTypeNames.Application.Json)]
    public async Task<ActionResult> GetSchedulesDirectCountries()
    {
        var client = _httpClientFactory.CreateClient(NamedClient.Default);
        // https://json.schedulesdirect.org/20141201/available/countries
        // Can't dispose the response as it's required up the call chain.
        var response = await client.GetAsync(new Uri("https://json.schedulesdirect.org/20141201/available/countries"))
            .ConfigureAwait(false);

        return File(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), MediaTypeNames.Application.Json);
    }

    /// <summary>
    /// Get channel mapping options.
    /// </summary>
    /// <param name="providerId">Provider id.</param>
    /// <response code="200">Channel mapping options returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the channel mapping options.</returns>
    [HttpGet("ChannelMappingOptions")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ChannelMappingOptionsDto> GetChannelMappingOptions([FromQuery] string? providerId)
        => _listingsManager.GetChannelMappingOptions(providerId);

    /// <summary>
    /// Set channel mappings.
    /// </summary>
    /// <param name="dto">The set channel mapping dto.</param>
    /// <response code="200">Created channel mapping returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the created channel mapping.</returns>
    [HttpPost("ChannelMappings")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<TunerChannelMapping> SetChannelMapping([FromBody, Required] SetChannelMappingDto dto)
        => _listingsManager.SetChannelMapping(dto.ProviderId, dto.TunerChannelId, dto.ProviderChannelId);

    /// <summary>
    /// Get tuner host types.
    /// </summary>
    /// <response code="200">Tuner host types returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the tuner host types.</returns>
    [HttpGet("TunerHosts/Types")]
    [Authorize(Policy = Policies.LiveTvAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<NameIdPair> GetTunerHostTypes()
        => _tunerHostManager.GetTunerHostTypes();

    /// <summary>
    /// Discover tuners.
    /// </summary>
    /// <param name="newDevicesOnly">Only discover new tuners.</param>
    /// <response code="200">Tuners returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the tuners.</returns>
    [HttpGet("Tuners/Discvover", Name = "DiscvoverTuners")]
    [HttpGet("Tuners/Discover")]
    [Authorize(Policy = Policies.LiveTvManagement)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IAsyncEnumerable<TunerHostInfo> DiscoverTuners([FromQuery] bool newDevicesOnly = false)
        => _tunerHostManager.DiscoverTuners(newDevicesOnly);

    /// <summary>
    /// Gets a live tv recording stream.
    /// </summary>
    /// <param name="recordingId">Recording id.</param>
    /// <response code="200">Recording stream returned.</response>
    /// <response code="404">Recording not found.</response>
    /// <returns>
    /// An <see cref="OkResult"/> containing the recording stream on success,
    /// or a <see cref="NotFoundResult"/> if recording not found.
    /// </returns>
    [HttpGet("LiveRecordings/{recordingId}/stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesVideoFile]
    public ActionResult GetLiveRecordingFile([FromRoute, Required] string recordingId)
    {
        var path = _recordingsManager.GetActiveRecordingPath(recordingId);
        if (string.IsNullOrWhiteSpace(path))
        {
            return NotFound();
        }

        var stream = new ProgressiveFileStream(path, null, _transcodeManager);
        return new FileStreamResult(stream, MimeTypes.GetMimeType(path));
    }

    /// <summary>
    /// Gets a live tv channel stream.
    /// </summary>
    /// <param name="streamId">Stream id.</param>
    /// <param name="container">Container type.</param>
    /// <response code="200">Stream returned.</response>
    /// <response code="404">Stream not found.</response>
    /// <returns>
    /// An <see cref="OkResult"/> containing the channel stream on success,
    /// or a <see cref="NotFoundResult"/> if stream not found.
    /// </returns>
    [HttpGet("LiveStreamFiles/{streamId}/stream.{container}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesVideoFile]
    public ActionResult GetLiveStreamFile(
        [FromRoute, Required] string streamId,
        [FromRoute, Required] [RegularExpression(EncodingHelper.ContainerValidationRegex)] string container)
    {
        var liveStreamInfo = _mediaSourceManager.GetLiveStreamInfoByUniqueId(streamId);
        if (liveStreamInfo is null)
        {
            return NotFound();
        }

        var liveStream = new ProgressiveFileStream(liveStreamInfo.GetStream());
        return new FileStreamResult(liveStream, MimeTypes.GetMimeType("file." + container));
    }
}
