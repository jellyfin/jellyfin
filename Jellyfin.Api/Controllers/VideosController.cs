using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data;
using Jellyfin.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The videos controller.
/// </summary>
[Tags("Video")]
public class VideosController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ITranscodeManager _transcodeManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EncodingHelper _encodingHelper;

    private readonly TranscodingJobType _transcodingJobType = TranscodingJobType.Progressive;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideosController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="transcodeManager">Instance of the <see cref="ITranscodeManager"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    public VideosController(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDtoService dtoService,
        IMediaSourceManager mediaSourceManager,
        IServerConfigurationManager serverConfigurationManager,
        IMediaEncoder mediaEncoder,
        ITranscodeManager transcodeManager,
        IHttpClientFactory httpClientFactory,
        EncodingHelper encodingHelper)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
        _mediaSourceManager = mediaSourceManager;
        _serverConfigurationManager = serverConfigurationManager;
        _mediaEncoder = mediaEncoder;
        _transcodeManager = transcodeManager;
        _httpClientFactory = httpClientFactory;
        _encodingHelper = encodingHelper;
    }

    /// <summary>
    /// Downloads a bounded-quality H.264/AAC MP4 using the existing progressive pipeline.
    /// </summary>
    /// <param name="itemId">The video item id.</param>
    /// <param name="quality">A whitelisted offline preset: 540p, 720p, or 1080p.</param>
    /// <param name="audioStreamIndex">An audio stream index from the item's metadata, or the default.</param>
    /// <param name="subtitleStreamIndex">A subtitle stream index from the item's metadata.</param>
    /// <param name="subtitleMode">none, embed (switchable text), or burn (permanent).</param>
    /// <returns>A transcoded attachment, not a persistent library copy.</returns>
    [HttpGet("{itemId}/download.mp4")]
    [Authorize(Policy = Policies.Download)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesVideoFile]
    public async Task<ActionResult> DownloadTranscoded(
        [FromRoute, Required] Guid itemId,
        [FromQuery] string quality = "720p",
        [FromQuery] int? audioStreamIndex = null,
        [FromQuery] int? subtitleStreamIndex = null,
        [FromQuery] string subtitleMode = "none")
    {
        var invalid = ValidateOfflineRequest(itemId, quality, audioStreamIndex, subtitleStreamIndex, subtitleMode, out var item);
        if (invalid is not null)
        {
            return invalid;
        }

        var preset = OfflineDownloadProfiles.Get(quality)!;
        var sessionId = HttpContext.Items.TryGetValue(OfflineDownloadContext.SessionKey, out var session) && session is string sessionValue
            ? sessionValue
            : Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var deviceId = "offline-download-" + sessionId;
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        // The fixed offline preset must not inherit codec-specific StreamOptions from arbitrary query keys.
        var originalQuery = Request.QueryString;
        Request.QueryString = QueryString.Empty;
        ActionResult result;
        try
        {
            result = await GetVideoStream(
                itemId: itemId,
                container: "mp4",
                @static: false,
                @params: null,
                tag: null,
                deviceProfileId: null,
                playSessionId: sessionId,
                segmentContainer: null,
                segmentLength: null,
                minSegments: null,
                mediaSourceId: null,
                deviceId: deviceId,
                audioCodec: "aac",
                enableAutoStreamCopy: false,
                allowVideoStreamCopy: false,
                allowAudioStreamCopy: false,
                audioSampleRate: 48_000,
                maxAudioBitDepth: null,
                audioBitRate: preset.AudioBitRate,
                audioChannels: 2,
                maxAudioChannels: 2,
                profile: "main",
                level: "41",
                framerate: null,
                maxFramerate: 30,
                copyTimestamps: false,
                startTimeTicks: 0,
                width: null,
                height: null,
                maxWidth: preset.MaxWidth,
                maxHeight: preset.MaxHeight,
                videoBitRate: preset.VideoBitRate,
                subtitleStreamIndex: subtitleMode == "none" ? -1 : subtitleStreamIndex,
                subtitleMethod: subtitleMode == "embed" ? SubtitleDeliveryMethod.Embed : subtitleMode == "burn" ? SubtitleDeliveryMethod.Encode : SubtitleDeliveryMethod.Drop,
                maxRefFrames: null,
                maxVideoBitDepth: 8,
                requireAvc: true,
                deInterlace: false,
                requireNonAnamorphic: true,
                transcodingMaxAudioChannels: 2,
                cpuCoreLimit: 2,
                liveStreamId: null,
                enableMpegtsM2TsMode: false,
                videoCodec: "h264",
                subtitleCodec: subtitleMode == "embed" ? "mov_text" : null,
                transcodeReasons: "VideoBitrateNotSupported",
                audioStreamIndex: audioStreamIndex,
                videoStreamIndex: null,
                context: EncodingContext.Streaming,
                streamOptions: new Dictionary<string, string>(),
                enableAudioVbrEncoding: true).ConfigureAwait(false);
        }
        finally
        {
            Request.QueryString = originalQuery;
        }

        if (result is FileStreamResult file)
        {
            file.FileDownloadName = OfflineDownloadProfiles.CreateFileName(item!.Path, quality);
            file.FileStream = new OfflineDownloadStream(
                file.FileStream,
                () => _transcodeManager.KillTranscodingJobs(deviceId, sessionId, _ => true),
                () =>
                {
                    var job = _transcodeManager.GetTranscodingJob(sessionId);
                    if (job is null || !job.HasExited || job.ExitCode != 0)
                    {
                        throw new System.IO.IOException("Offline transcoding did not complete successfully.");
                    }
                });
        }

        return result;
    }

    internal ActionResult? ValidateOfflineRequest(Guid itemId, string quality, int? audioStreamIndex, int? subtitleStreamIndex, string subtitleMode, out BaseItem? item)
    {
        item = null;
        var preset = OfflineDownloadProfiles.Get(quality);
        if (preset is null)
        {
            return BadRequest("Unsupported offline download quality.");
        }

        var userId = User.GetUserId();
        if (userId.IsEmpty())
        {
            return Unauthorized();
        }

        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        if (!item.CanDownload(user)
            || !user.HasPermission(Jellyfin.Database.Implementations.Enums.PermissionKind.EnableVideoPlaybackTranscoding)
            || !user.HasPermission(Jellyfin.Database.Implementations.Enums.PermissionKind.EnableAudioPlaybackTranscoding))
        {
            return Forbid();
        }

        if (item is not Video || !item.IsFileProtocol)
        {
            return BadRequest("Offline transcoded downloads support local video files only.");
        }

        // The progressive encoder cannot apply a resolution ceiling without probed dimensions.
        // Newly indexed items can be visible before the asynchronous metadata scan finishes.
        if (!item.GetMediaStreams().Any(stream => stream.Type == MediaStreamType.Video && stream.Width > 0 && stream.Height > 0))
        {
            return BadRequest("Video metadata is not ready. Wait for the library scan or refresh this item's metadata before downloading.");
        }

        var streams = item.GetMediaStreams();
        if (audioStreamIndex.HasValue && !streams.Any(stream => stream.Type == MediaStreamType.Audio && stream.Index == audioStreamIndex.Value))
        {
            return BadRequest("The selected audio track is not available.");
        }

        var trackError = OfflineDownloadTracks.Validate(streams, subtitleStreamIndex, subtitleMode);
        if (trackError is not null)
        {
            return BadRequest(trackError);
        }

        return null;
    }

    /// <summary>
    /// Lists selectable offline audio and subtitle tracks without disclosing source paths.
    /// </summary>
    /// <param name="itemId">The visible, downloadable local video.</param>
    /// <returns>The selectable stream metadata.</returns>
    [HttpGet("{itemId}/OfflineDownloadOptions")]
    [Authorize(Policy = Policies.Download)]
    public ActionResult GetOfflineDownloadOptions([FromRoute] Guid itemId)
    {
        var invalid = ValidateOfflineRequest(itemId, "720p", null, null, "none", out var item);
        if (invalid is not null)
        {
            return invalid;
        }

        Response.Headers["Cache-Control"] = "no-store";
        var streams = item!.GetMediaStreams();
        return new OkObjectResult(new
        {
            AudioTracks = streams.Where(stream => stream.Type == MediaStreamType.Audio).Select(stream => new
            {
                stream.Index,
                stream.DisplayTitle,
                stream.Language,
                stream.Codec,
                stream.IsDefault
            }),
            SubtitleTracks = streams.Where(stream => stream.Type == MediaStreamType.Subtitle).Select(stream => new
            {
                stream.Index,
                stream.DisplayTitle,
                stream.Language,
                stream.Codec,
                stream.IsDefault,
                CanEmbed = OfflineDownloadTracks.CanEmbed(stream),
                CanBurn = OfflineDownloadTracks.CanBurn(stream)
            })
        });
    }

    /// <summary>
    /// Gets additional parts for a video.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
    /// <response code="200">Additional parts returned.</response>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the parts.</returns>
    [HttpGet("{itemId}/AdditionalParts")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetAdditionalPart([FromRoute, Required] Guid itemId, [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var item = itemId.IsEmpty()
            ? (userId.IsNullOrEmpty()
                ? _libraryManager.RootFolder
                : _libraryManager.GetUserRootFolder())
            : _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        var dtoOptions = new DtoOptions();

        BaseItemDto[] items;
        if (item is Video video)
        {
            items = video.GetAdditionalParts(user)
                .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user, video))
                .ToArray();
        }
        else
        {
            items = Array.Empty<BaseItemDto>();
        }

        var result = new QueryResult<BaseItemDto>(items);
        return result;
    }

    /// <summary>
    /// Removes alternate video sources.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <response code="204">Alternate sources deleted.</response>
    /// <response code="404">Video not found.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success, or a <see cref="NotFoundResult"/> if the video doesn't exist.</returns>
    [HttpDelete("{itemId}/AlternateSources")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAlternateSources([FromRoute, Required] Guid itemId)
    {
        var item = _libraryManager.GetItemById<Video>(itemId, User.GetUserId());
        if (item is null)
        {
            return NotFound();
        }

        if (item.LinkedAlternateVersions.Length == 0 && item.PrimaryVersionId.HasValue)
        {
            item = _libraryManager.GetItemById<Video>(item.PrimaryVersionId.Value);
        }

        if (item is null)
        {
            return NotFound();
        }

        foreach (var link in _libraryManager.GetLinkedAlternateVersions(item))
        {
            link.SetPrimaryVersionId(null);
            link.LinkedAlternateVersions = Array.Empty<LinkedChild>();

            await link.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        item.LinkedAlternateVersions = Array.Empty<LinkedChild>();
        item.SetPrimaryVersionId(null);
        await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Merges videos into a single record.
    /// </summary>
    /// <param name="ids">Item id list. This allows multiple, comma delimited.</param>
    /// <response code="204">Videos merged.</response>
    /// <response code="400">Supply at least 2 video ids.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success, or a <see cref="BadRequestResult"/> if less than two ids were supplied.</returns>
    [HttpPost("MergeVersions")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> MergeVersions([FromQuery, Required, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] Guid[] ids)
    {
        var userId = User.GetUserId();
        var items = ids
            .Select(i => _libraryManager.GetItemById<BaseItem>(i, userId))
            .OfType<Video>()
            .OrderBy(i => i.Id)
            .ToList();

        if (items.Count < 2)
        {
            return BadRequest("Please supply at least two videos to merge.");
        }

        var primaryVersion = items.FirstOrDefault(i => i.MediaSourceCount > 1 && !i.PrimaryVersionId.HasValue);
        if (primaryVersion is null)
        {
            primaryVersion = items
                .OrderBy(i =>
                {
                    if (i.Video3DFormat.HasValue || i.VideoType != VideoType.VideoFile)
                    {
                        return 1;
                    }

                    return 0;
                })
                .ThenByDescending(i => i.GetDefaultVideoStream()?.Width ?? 0)
                .First();
        }

        var alternateVersionsOfPrimary = primaryVersion.LinkedAlternateVersions.ToList();

        foreach (var item in items.Where(i => !i.Id.Equals(primaryVersion.Id)))
        {
            item.SetPrimaryVersionId(primaryVersion.Id);

            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            // Re-route any playlist/collection references from this item to the primary
            await _libraryManager.RerouteLinkedChildReferencesAsync(item.Id, primaryVersion.Id).ConfigureAwait(false);

            if (!alternateVersionsOfPrimary.Any(i => i.ItemId.HasValue && i.ItemId.Value.Equals(item.Id)))
            {
                alternateVersionsOfPrimary.Add(new LinkedChild
                {
                    ItemId = item.Id,
                    Type = LinkedChildType.LinkedAlternateVersion
                });
            }

            foreach (var linkedItem in item.LinkedAlternateVersions)
            {
                if (linkedItem.ItemId.HasValue && !alternateVersionsOfPrimary.Any(i => i.ItemId.HasValue && i.ItemId.Value.Equals(linkedItem.ItemId.Value)))
                {
                    alternateVersionsOfPrimary.Add(linkedItem);
                }
            }

            if (item.LinkedAlternateVersions.Length > 0)
            {
                item.LinkedAlternateVersions = Array.Empty<LinkedChild>();
                await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            }
        }

        primaryVersion.LinkedAlternateVersions = alternateVersionsOfPrimary.ToArray();
        await primaryVersion.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Gets a video stream.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
    /// <param name="static">Optional. If true, the original file will be streamed statically without any encoding. Use either no url extension or the original file extension. true/false.</param>
    /// <param name="params">The streaming parameters.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="deviceProfileId">Optional. The dlna device profile id to utilize.</param>
    /// <param name="playSessionId">The play session id.</param>
    /// <param name="segmentContainer">The segment container.</param>
    /// <param name="segmentLength">The segment length.</param>
    /// <param name="minSegments">The minimum number of segments.</param>
    /// <param name="mediaSourceId">The media version id, if playing an alternate version.</param>
    /// <param name="deviceId">The device id of the client requesting. Used to stop encoding processes when needed.</param>
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3. If omitted the server will auto-select using the url's extension.</param>
    /// <param name="enableAutoStreamCopy">Whether or not to allow automatic stream copy if requested values match the original source. Defaults to true.</param>
    /// <param name="allowVideoStreamCopy">Whether or not to allow copying of the video stream url.</param>
    /// <param name="allowAudioStreamCopy">Whether or not to allow copying of the audio stream url.</param>
    /// <param name="audioSampleRate">Optional. Specify a specific audio sample rate, e.g. 44100.</param>
    /// <param name="maxAudioBitDepth">Optional. The maximum audio bit depth.</param>
    /// <param name="audioBitRate">Optional. Specify an audio bitrate to encode to, e.g. 128000. If omitted this will be left to encoder defaults.</param>
    /// <param name="audioChannels">Optional. Specify a specific number of audio channels to encode to, e.g. 2.</param>
    /// <param name="maxAudioChannels">Optional. Specify a maximum number of audio channels to encode to, e.g. 2.</param>
    /// <param name="profile">Optional. Specify a specific an encoder profile (varies by encoder), e.g. main, baseline, high.</param>
    /// <param name="level">Optional. Specify a level for the encoder profile (varies by encoder), e.g. 3, 3.1.</param>
    /// <param name="framerate">Optional. A specific video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
    /// <param name="maxFramerate">Optional. A specific maximum video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
    /// <param name="copyTimestamps">Whether or not to copy timestamps when transcoding with an offset. Defaults to false.</param>
    /// <param name="startTimeTicks">Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms.</param>
    /// <param name="width">Optional. The fixed horizontal resolution of the encoded video.</param>
    /// <param name="height">Optional. The fixed vertical resolution of the encoded video.</param>
    /// <param name="maxWidth">Optional. The maximum horizontal resolution of the encoded video.</param>
    /// <param name="maxHeight">Optional. The maximum vertical resolution of the encoded video.</param>
    /// <param name="videoBitRate">Optional. Specify a video bitrate to encode to, e.g. 500000. If omitted this will be left to encoder defaults.</param>
    /// <param name="subtitleStreamIndex">Optional. The index of the subtitle stream to use. If omitted no subtitles will be used.</param>
    /// <param name="subtitleMethod">Optional. Specify the subtitle delivery method.</param>
    /// <param name="maxRefFrames">Optional.</param>
    /// <param name="maxVideoBitDepth">Optional. The maximum video bit depth.</param>
    /// <param name="requireAvc">Optional. Whether to require avc.</param>
    /// <param name="deInterlace">Optional. Whether to deinterlace the video.</param>
    /// <param name="requireNonAnamorphic">Optional. Whether to require a non anamorphic stream.</param>
    /// <param name="transcodingMaxAudioChannels">Optional. The maximum number of audio channels to transcode.</param>
    /// <param name="cpuCoreLimit">Optional. The limit of how many cpu cores to use.</param>
    /// <param name="liveStreamId">The live stream id.</param>
    /// <param name="enableMpegtsM2TsMode">Optional. Whether to enable the MpegtsM2Ts mode.</param>
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264. If omitted the server will auto-select using the url's extension.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <response code="200">Video stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("{itemId}/stream")]
    [HttpHead("{itemId}/stream", Name = "HeadVideoStream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesVideoFile]
    public async Task<ActionResult> GetVideoStream(
        [FromRoute, Required] Guid itemId,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? container,
        [FromQuery] bool? @static,
        [FromQuery] string? @params,
        [FromQuery] string? tag,
        [FromQuery, ParameterObsolete] string? deviceProfileId,
        [FromQuery] string? playSessionId,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? segmentContainer,
        [FromQuery] int? segmentLength,
        [FromQuery] int? minSegments,
        [FromQuery] string? mediaSourceId,
        [FromQuery] string? deviceId,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? audioCodec,
        [FromQuery] bool? enableAutoStreamCopy,
        [FromQuery] bool? allowVideoStreamCopy,
        [FromQuery] bool? allowAudioStreamCopy,
        [FromQuery] int? audioSampleRate,
        [FromQuery] int? maxAudioBitDepth,
        [FromQuery] int? audioBitRate,
        [FromQuery] int? audioChannels,
        [FromQuery] int? maxAudioChannels,
        [FromQuery] string? profile,
        [FromQuery][RegularExpression(EncodingHelper.LevelValidationRegexStr)] string? level,
        [FromQuery] float? framerate,
        [FromQuery] float? maxFramerate,
        [FromQuery] bool? copyTimestamps,
        [FromQuery] long? startTimeTicks,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] int? videoBitRate,
        [FromQuery] int? subtitleStreamIndex,
        [FromQuery] SubtitleDeliveryMethod? subtitleMethod,
        [FromQuery] int? maxRefFrames,
        [FromQuery] int? maxVideoBitDepth,
        [FromQuery] bool? requireAvc,
        [FromQuery] bool? deInterlace,
        [FromQuery] bool? requireNonAnamorphic,
        [FromQuery] int? transcodingMaxAudioChannels,
        [FromQuery] int? cpuCoreLimit,
        [FromQuery] string? liveStreamId,
        [FromQuery] bool? enableMpegtsM2TsMode,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? videoCodec,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? subtitleCodec,
        [FromQuery] string? transcodeReasons,
        [FromQuery] int? audioStreamIndex,
        [FromQuery] int? videoStreamIndex,
        [FromQuery] EncodingContext? context,
        [FromQuery] Dictionary<string, string> streamOptions,
        [FromQuery] bool enableAudioVbrEncoding = true)
    {
        var isHeadRequest = Request.Method == System.Net.WebRequestMethods.Http.Head;
        // Offline preparation injects only its own job token. Ordinary playback retains the original lifecycle.
        var cancellationTokenSource = HttpContext.Items.TryGetValue(OfflineDownloadContext.CancellationKey, out var offlineToken) && offlineToken is CancellationToken token
            ? CancellationTokenSource.CreateLinkedTokenSource(token)
            : new CancellationTokenSource();
        var streamingRequest = new VideoRequestDto
        {
            Id = itemId,
            Container = container,
            Static = @static ?? false,
            Params = @params,
            Tag = tag,
            PlaySessionId = playSessionId,
            SegmentContainer = segmentContainer,
            SegmentLength = segmentLength,
            MinSegments = minSegments,
            MediaSourceId = mediaSourceId,
            DeviceId = deviceId,
            AudioCodec = audioCodec,
            EnableAutoStreamCopy = enableAutoStreamCopy ?? true,
            AllowAudioStreamCopy = allowAudioStreamCopy ?? true,
            AllowVideoStreamCopy = allowVideoStreamCopy ?? true,
            AudioSampleRate = audioSampleRate,
            MaxAudioChannels = maxAudioChannels,
            AudioBitRate = audioBitRate,
            MaxAudioBitDepth = maxAudioBitDepth,
            AudioChannels = audioChannels,
            Profile = profile,
            Level = level,
            Framerate = framerate,
            MaxFramerate = maxFramerate,
            CopyTimestamps = copyTimestamps ?? false,
            StartTimeTicks = startTimeTicks,
            Width = width,
            Height = height,
            MaxWidth = maxWidth,
            MaxHeight = maxHeight,
            VideoBitRate = videoBitRate,
            SubtitleStreamIndex = subtitleStreamIndex,
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.Encode,
            MaxRefFrames = maxRefFrames,
            MaxVideoBitDepth = maxVideoBitDepth,
            RequireAvc = requireAvc ?? false,
            DeInterlace = deInterlace ?? false,
            RequireNonAnamorphic = requireNonAnamorphic ?? false,
            TranscodingMaxAudioChannels = transcodingMaxAudioChannels,
            CpuCoreLimit = cpuCoreLimit,
            LiveStreamId = liveStreamId,
            EnableMpegtsM2TsMode = enableMpegtsM2TsMode ?? false,
            VideoCodec = videoCodec,
            SubtitleCodec = subtitleCodec,
            TranscodeReasons = transcodeReasons,
            AudioStreamIndex = audioStreamIndex,
            VideoStreamIndex = videoStreamIndex,
            Context = context ?? EncodingContext.Streaming,
            StreamOptions = streamOptions,
            EnableAudioVbrEncoding = enableAudioVbrEncoding
        };

        var state = await StreamingHelpers.GetStreamingState(
                streamingRequest,
                HttpContext,
                _mediaSourceManager,
                _userManager,
                _libraryManager,
                _serverConfigurationManager,
                _mediaEncoder,
                _encodingHelper,
                _transcodeManager,
                _transcodingJobType,
                cancellationTokenSource.Token)
            .ConfigureAwait(false);

        if (@static.HasValue && @static.Value && state.DirectStreamProvider is not null)
        {
            var liveStreamInfo = _mediaSourceManager.GetLiveStreamInfo(streamingRequest.LiveStreamId);
            if (liveStreamInfo is null)
            {
                return NotFound();
            }

            var liveStream = new ProgressiveFileStream(liveStreamInfo.GetStream());
            // TODO (moved from MediaBrowser.Api): Don't hardcode contentType
            return File(liveStream, MimeTypes.GetMimeType("file.ts"));
        }

        // Static remote stream
        if (@static.HasValue && @static.Value && state.InputProtocol == MediaProtocol.Http)
        {
            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            return await FileStreamResponseHelpers.GetStaticRemoteStreamResult(state, httpClient, HttpContext).ConfigureAwait(false);
        }

        if (@static.HasValue && @static.Value && state.InputProtocol != MediaProtocol.File)
        {
            return BadRequest($"Input protocol {state.InputProtocol} cannot be streamed statically");
        }

        // Static stream
        if (@static.HasValue && @static.Value && !(state.MediaSource.VideoType == VideoType.BluRay || state.MediaSource.VideoType == VideoType.Dvd))
        {
            var contentType = state.GetMimeType("." + state.OutputContainer, false) ?? state.GetMimeType(state.MediaPath);

            if (state.MediaSource.IsInfiniteStream)
            {
                var liveStream = new ProgressiveFileStream(state.MediaPath, null, _transcodeManager);
                return File(liveStream, contentType);
            }

            return FileStreamResponseHelpers.GetStaticFileResult(
                state.MediaPath,
                contentType);
        }

        // Need to start ffmpeg (because media can't be returned directly)
        var encodingOptions = _serverConfigurationManager.GetEncodingOptions();
        var ffmpegCommandLineArguments = _encodingHelper.GetProgressiveVideoFullCommandLine(state, encodingOptions, EncoderPreset.superfast);
        if (HttpContext.Items.ContainsKey(OfflineDownloadContext.SessionKey))
        {
            // Fixed offline-only output guard. Keep room for the final mux packet; normal playback is unchanged.
            var outputMarker = ffmpegCommandLineArguments.LastIndexOf(" -y \"", StringComparison.Ordinal);
            if (outputMarker < 0)
            {
                throw new InvalidOperationException("Cannot safely bound the offline encoder output.");
            }

            ffmpegCommandLineArguments = ffmpegCommandLineArguments.Insert(outputMarker, " -fs " + OfflineDownloadJobs.EncoderFileLimit.ToString(CultureInfo.InvariantCulture));
        }

        return await FileStreamResponseHelpers.GetTranscodedFile(
            state,
            isHeadRequest,
            HttpContext,
            _transcodeManager,
            ffmpegCommandLineArguments,
            _transcodingJobType,
            cancellationTokenSource).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a video stream.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
    /// <param name="static">Optional. If true, the original file will be streamed statically without any encoding. Use either no url extension or the original file extension. true/false.</param>
    /// <param name="params">The streaming parameters.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="deviceProfileId">Optional. The dlna device profile id to utilize.</param>
    /// <param name="playSessionId">The play session id.</param>
    /// <param name="segmentContainer">The segment container.</param>
    /// <param name="segmentLength">The segment length.</param>
    /// <param name="minSegments">The minimum number of segments.</param>
    /// <param name="mediaSourceId">The media version id, if playing an alternate version.</param>
    /// <param name="deviceId">The device id of the client requesting. Used to stop encoding processes when needed.</param>
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3. If omitted the server will auto-select using the url's extension.</param>
    /// <param name="enableAutoStreamCopy">Whether or not to allow automatic stream copy if requested values match the original source. Defaults to true.</param>
    /// <param name="allowVideoStreamCopy">Whether or not to allow copying of the video stream url.</param>
    /// <param name="allowAudioStreamCopy">Whether or not to allow copying of the audio stream url.</param>
    /// <param name="audioSampleRate">Optional. Specify a specific audio sample rate, e.g. 44100.</param>
    /// <param name="maxAudioBitDepth">Optional. The maximum audio bit depth.</param>
    /// <param name="audioBitRate">Optional. Specify an audio bitrate to encode to, e.g. 128000. If omitted this will be left to encoder defaults.</param>
    /// <param name="audioChannels">Optional. Specify a specific number of audio channels to encode to, e.g. 2.</param>
    /// <param name="maxAudioChannels">Optional. Specify a maximum number of audio channels to encode to, e.g. 2.</param>
    /// <param name="profile">Optional. Specify a specific an encoder profile (varies by encoder), e.g. main, baseline, high.</param>
    /// <param name="level">Optional. Specify a level for the encoder profile (varies by encoder), e.g. 3, 3.1.</param>
    /// <param name="framerate">Optional. A specific video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
    /// <param name="maxFramerate">Optional. A specific maximum video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
    /// <param name="copyTimestamps">Whether or not to copy timestamps when transcoding with an offset. Defaults to false.</param>
    /// <param name="startTimeTicks">Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms.</param>
    /// <param name="width">Optional. The fixed horizontal resolution of the encoded video.</param>
    /// <param name="height">Optional. The fixed vertical resolution of the encoded video.</param>
    /// <param name="maxWidth">Optional. The maximum horizontal resolution of the encoded video.</param>
    /// <param name="maxHeight">Optional. The maximum vertical resolution of the encoded video.</param>
    /// <param name="videoBitRate">Optional. Specify a video bitrate to encode to, e.g. 500000. If omitted this will be left to encoder defaults.</param>
    /// <param name="subtitleStreamIndex">Optional. The index of the subtitle stream to use. If omitted no subtitles will be used.</param>
    /// <param name="subtitleMethod">Optional. Specify the subtitle delivery method.</param>
    /// <param name="maxRefFrames">Optional.</param>
    /// <param name="maxVideoBitDepth">Optional. The maximum video bit depth.</param>
    /// <param name="requireAvc">Optional. Whether to require avc.</param>
    /// <param name="deInterlace">Optional. Whether to deinterlace the video.</param>
    /// <param name="requireNonAnamorphic">Optional. Whether to require a non anamorphic stream.</param>
    /// <param name="transcodingMaxAudioChannels">Optional. The maximum number of audio channels to transcode.</param>
    /// <param name="cpuCoreLimit">Optional. The limit of how many cpu cores to use.</param>
    /// <param name="liveStreamId">The live stream id.</param>
    /// <param name="enableMpegtsM2TsMode">Optional. Whether to enable the MpegtsM2Ts mode.</param>
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264. If omitted the server will auto-select using the url's extension.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <response code="200">Video stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("{itemId}/stream.{container}")]
    [HttpHead("{itemId}/stream.{container}", Name = "HeadVideoStreamByContainer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesVideoFile]
    public Task<ActionResult> GetVideoStreamByContainer(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string container,
        [FromQuery] bool? @static,
        [FromQuery] string? @params,
        [FromQuery] string? tag,
        [FromQuery] string? deviceProfileId,
        [FromQuery] string? playSessionId,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? segmentContainer,
        [FromQuery] int? segmentLength,
        [FromQuery] int? minSegments,
        [FromQuery] string? mediaSourceId,
        [FromQuery] string? deviceId,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? audioCodec,
        [FromQuery] bool? enableAutoStreamCopy,
        [FromQuery] bool? allowVideoStreamCopy,
        [FromQuery] bool? allowAudioStreamCopy,
        [FromQuery] int? audioSampleRate,
        [FromQuery] int? maxAudioBitDepth,
        [FromQuery] int? audioBitRate,
        [FromQuery] int? audioChannels,
        [FromQuery] int? maxAudioChannels,
        [FromQuery] string? profile,
        [FromQuery][RegularExpression(EncodingHelper.LevelValidationRegexStr)] string? level,
        [FromQuery] float? framerate,
        [FromQuery] float? maxFramerate,
        [FromQuery] bool? copyTimestamps,
        [FromQuery] long? startTimeTicks,
        [FromQuery] int? width,
        [FromQuery] int? height,
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] int? videoBitRate,
        [FromQuery] int? subtitleStreamIndex,
        [FromQuery] SubtitleDeliveryMethod? subtitleMethod,
        [FromQuery] int? maxRefFrames,
        [FromQuery] int? maxVideoBitDepth,
        [FromQuery] bool? requireAvc,
        [FromQuery] bool? deInterlace,
        [FromQuery] bool? requireNonAnamorphic,
        [FromQuery] int? transcodingMaxAudioChannels,
        [FromQuery] int? cpuCoreLimit,
        [FromQuery] string? liveStreamId,
        [FromQuery] bool? enableMpegtsM2TsMode,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? videoCodec,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? subtitleCodec,
        [FromQuery] string? transcodeReasons,
        [FromQuery] int? audioStreamIndex,
        [FromQuery] int? videoStreamIndex,
        [FromQuery] EncodingContext? context,
        [FromQuery] Dictionary<string, string> streamOptions,
        [FromQuery] bool enableAudioVbrEncoding = true)
    {
        return GetVideoStream(
            itemId,
            container,
            @static,
            @params,
            tag,
            deviceProfileId,
            playSessionId,
            segmentContainer,
            segmentLength,
            minSegments,
            mediaSourceId,
            deviceId,
            audioCodec,
            enableAutoStreamCopy,
            allowVideoStreamCopy,
            allowAudioStreamCopy,
            audioSampleRate,
            maxAudioBitDepth,
            audioBitRate,
            audioChannels,
            maxAudioChannels,
            profile,
            level,
            framerate,
            maxFramerate,
            copyTimestamps,
            startTimeTicks,
            width,
            height,
            maxWidth,
            maxHeight,
            videoBitRate,
            subtitleStreamIndex,
            subtitleMethod,
            maxRefFrames,
            maxVideoBitDepth,
            requireAvc,
            deInterlace,
            requireNonAnamorphic,
            transcodingMaxAudioChannels,
            cpuCoreLimit,
            liveStreamId,
            enableMpegtsM2TsMode,
            videoCodec,
            subtitleCodec,
            transcodeReasons,
            audioStreamIndex,
            videoStreamIndex,
            context,
            streamOptions,
            enableAudioVbrEncoding);
    }
}
