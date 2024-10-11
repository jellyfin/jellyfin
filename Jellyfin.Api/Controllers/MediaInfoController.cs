using System;
using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.MediaInfoDtos;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The media info controller.
/// </summary>
[Route("")]
[Authorize]
public class MediaInfoController : BaseJellyfinApiController
{
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IDeviceManager _deviceManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<MediaInfoController> _logger;
    private readonly MediaInfoHelper _mediaInfoHelper;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaInfoController"/> class.
    /// </summary>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{MediaInfoController}"/> interface.</param>
    /// <param name="mediaInfoHelper">Instance of the <see cref="MediaInfoHelper"/>.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface..</param>
    public MediaInfoController(
        IMediaSourceManager mediaSourceManager,
        IDeviceManager deviceManager,
        ILibraryManager libraryManager,
        ILogger<MediaInfoController> logger,
        MediaInfoHelper mediaInfoHelper,
        IUserManager userManager)
    {
        _mediaSourceManager = mediaSourceManager;
        _deviceManager = deviceManager;
        _libraryManager = libraryManager;
        _logger = logger;
        _mediaInfoHelper = mediaInfoHelper;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets live playback media info for an item.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="userId">The user id.</param>
    /// <response code="200">Playback info returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="Task"/> containing a <see cref="PlaybackInfoResponse"/> with the playback information.</returns>
    [HttpGet("Items/{itemId}/PlaybackInfo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaybackInfoResponse>> GetPlaybackInfo([FromRoute, Required] Guid itemId, [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);
        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        return await _mediaInfoHelper.GetPlaybackInfo(item, user).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets live playback media info for an item.
    /// </summary>
    /// <remarks>
    /// For backwards compatibility parameters can be sent via Query or Body, with Query having higher precedence.
    /// Query parameters are obsolete.
    /// </remarks>
    /// <param name="itemId">The item id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="maxStreamingBitrate">The maximum streaming bitrate.</param>
    /// <param name="startTimeTicks">The start time in ticks.</param>
    /// <param name="audioStreamIndex">The audio stream index.</param>
    /// <param name="subtitleStreamIndex">The subtitle stream index.</param>
    /// <param name="maxAudioChannels">The maximum number of audio channels.</param>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="liveStreamId">The livestream id.</param>
    /// <param name="autoOpenLiveStream">Whether to auto open the livestream.</param>
    /// <param name="enableDirectPlay">Whether to enable direct play. Default: true.</param>
    /// <param name="enableDirectStream">Whether to enable direct stream. Default: true.</param>
    /// <param name="enableTranscoding">Whether to enable transcoding. Default: true.</param>
    /// <param name="allowVideoStreamCopy">Whether to allow to copy the video stream. Default: true.</param>
    /// <param name="allowAudioStreamCopy">Whether to allow to copy the audio stream. Default: true.</param>
    /// <param name="playbackInfoDto">The playback info.</param>
    /// <response code="200">Playback info returned.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="Task"/> containing a <see cref="PlaybackInfoResponse"/> with the playback info.</returns>
    [HttpPost("Items/{itemId}/PlaybackInfo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlaybackInfoResponse>> GetPostedPlaybackInfo(
        [FromRoute, Required] Guid itemId,
        [FromQuery, ParameterObsolete] Guid? userId,
        [FromQuery, ParameterObsolete] int? maxStreamingBitrate,
        [FromQuery, ParameterObsolete] long? startTimeTicks,
        [FromQuery, ParameterObsolete] int? audioStreamIndex,
        [FromQuery, ParameterObsolete] int? subtitleStreamIndex,
        [FromQuery, ParameterObsolete] int? maxAudioChannels,
        [FromQuery, ParameterObsolete] string? mediaSourceId,
        [FromQuery, ParameterObsolete] string? liveStreamId,
        [FromQuery, ParameterObsolete] bool? autoOpenLiveStream,
        [FromQuery, ParameterObsolete] bool? enableDirectPlay,
        [FromQuery, ParameterObsolete] bool? enableDirectStream,
        [FromQuery, ParameterObsolete] bool? enableTranscoding,
        [FromQuery, ParameterObsolete] bool? allowVideoStreamCopy,
        [FromQuery, ParameterObsolete] bool? allowAudioStreamCopy,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PlaybackInfoDto? playbackInfoDto)
    {
        var profile = playbackInfoDto?.DeviceProfile;
        _logger.LogDebug("GetPostedPlaybackInfo profile: {@Profile}", profile);

        if (profile is null)
        {
            var caps = _deviceManager.GetCapabilities(User.GetDeviceId());
            if (caps is not null)
            {
                profile = caps.DeviceProfile;
            }
        }

        // Copy params from posted body
        // TODO clean up when breaking API compatibility.
        userId ??= playbackInfoDto?.UserId;
        userId = RequestHelpers.GetUserId(User, userId);
        maxStreamingBitrate ??= playbackInfoDto?.MaxStreamingBitrate;
        startTimeTicks ??= playbackInfoDto?.StartTimeTicks;
        audioStreamIndex ??= playbackInfoDto?.AudioStreamIndex;
        subtitleStreamIndex ??= playbackInfoDto?.SubtitleStreamIndex;
        maxAudioChannels ??= playbackInfoDto?.MaxAudioChannels;
        mediaSourceId ??= playbackInfoDto?.MediaSourceId;
        liveStreamId ??= playbackInfoDto?.LiveStreamId;
        autoOpenLiveStream ??= playbackInfoDto?.AutoOpenLiveStream ?? false;
        enableDirectPlay ??= playbackInfoDto?.EnableDirectPlay ?? true;
        enableDirectStream ??= playbackInfoDto?.EnableDirectStream ?? true;
        enableTranscoding ??= playbackInfoDto?.EnableTranscoding ?? true;
        allowVideoStreamCopy ??= playbackInfoDto?.AllowVideoStreamCopy ?? true;
        allowAudioStreamCopy ??= playbackInfoDto?.AllowAudioStreamCopy ?? true;

        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);
        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        var info = await _mediaInfoHelper.GetPlaybackInfo(
                item,
                user,
                mediaSourceId,
                liveStreamId)
            .ConfigureAwait(false);

        if (info.ErrorCode is not null)
        {
            return info;
        }

        if (profile is not null)
        {
            // set device specific data
            foreach (var mediaSource in info.MediaSources)
            {
                _mediaInfoHelper.SetDeviceSpecificData(
                    item,
                    mediaSource,
                    profile,
                    User,
                    maxStreamingBitrate ?? profile.MaxStreamingBitrate,
                    startTimeTicks ?? 0,
                    mediaSourceId ?? string.Empty,
                    audioStreamIndex,
                    subtitleStreamIndex,
                    maxAudioChannels,
                    info.PlaySessionId!,
                    userId ?? Guid.Empty,
                    enableDirectPlay.Value,
                    enableDirectStream.Value,
                    enableTranscoding.Value,
                    allowVideoStreamCopy.Value,
                    allowAudioStreamCopy.Value,
                    playbackInfoDto?.AlwaysBurnInSubtitleWhenTranscoding ?? false,
                    Request.HttpContext.GetNormalizedRemoteIP());
            }

            _mediaInfoHelper.SortMediaSources(info, maxStreamingBitrate);
        }

        if (autoOpenLiveStream.Value)
        {
            var mediaSource = string.IsNullOrWhiteSpace(mediaSourceId) ? info.MediaSources[0] : info.MediaSources.FirstOrDefault(i => string.Equals(i.Id, mediaSourceId, StringComparison.Ordinal));

            if (mediaSource is not null && mediaSource.RequiresOpening && string.IsNullOrWhiteSpace(mediaSource.LiveStreamId))
            {
                var openStreamResult = await _mediaInfoHelper.OpenMediaSource(
                    HttpContext,
                    new LiveStreamRequest
                    {
                        AudioStreamIndex = audioStreamIndex,
                        DeviceProfile = playbackInfoDto?.DeviceProfile,
                        EnableDirectPlay = enableDirectPlay.Value,
                        EnableDirectStream = enableDirectStream.Value,
                        ItemId = itemId,
                        MaxAudioChannels = maxAudioChannels,
                        MaxStreamingBitrate = maxStreamingBitrate,
                        PlaySessionId = info.PlaySessionId,
                        StartTimeTicks = startTimeTicks,
                        SubtitleStreamIndex = subtitleStreamIndex,
                        UserId = userId ?? Guid.Empty,
                        OpenToken = mediaSource.OpenToken,
                        AlwaysBurnInSubtitleWhenTranscoding = playbackInfoDto?.AlwaysBurnInSubtitleWhenTranscoding ?? false
                    }).ConfigureAwait(false);

                info.MediaSources = new[] { openStreamResult.MediaSource };
            }
        }

        return info;
    }

    /// <summary>
    /// Opens a media source.
    /// </summary>
    /// <param name="openToken">The open token.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="playSessionId">The play session id.</param>
    /// <param name="maxStreamingBitrate">The maximum streaming bitrate.</param>
    /// <param name="startTimeTicks">The start time in ticks.</param>
    /// <param name="audioStreamIndex">The audio stream index.</param>
    /// <param name="subtitleStreamIndex">The subtitle stream index.</param>
    /// <param name="maxAudioChannels">The maximum number of audio channels.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="openLiveStreamDto">The open live stream dto.</param>
    /// <param name="enableDirectPlay">Whether to enable direct play. Default: true.</param>
    /// <param name="enableDirectStream">Whether to enable direct stream. Default: true.</param>
    /// <param name="alwaysBurnInSubtitleWhenTranscoding">Always burn-in subtitle when transcoding.</param>
    /// <response code="200">Media source opened.</response>
    /// <returns>A <see cref="Task"/> containing a <see cref="LiveStreamResponse"/>.</returns>
    [HttpPost("LiveStreams/Open")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<LiveStreamResponse>> OpenLiveStream(
        [FromQuery] string? openToken,
        [FromQuery] Guid? userId,
        [FromQuery] string? playSessionId,
        [FromQuery] int? maxStreamingBitrate,
        [FromQuery] long? startTimeTicks,
        [FromQuery] int? audioStreamIndex,
        [FromQuery] int? subtitleStreamIndex,
        [FromQuery] int? maxAudioChannels,
        [FromQuery] Guid? itemId,
        [FromBody] OpenLiveStreamDto? openLiveStreamDto,
        [FromQuery] bool? enableDirectPlay,
        [FromQuery] bool? enableDirectStream,
        [FromQuery] bool? alwaysBurnInSubtitleWhenTranscoding)
    {
        userId ??= openLiveStreamDto?.UserId;
        userId = RequestHelpers.GetUserId(User, userId);
        var request = new LiveStreamRequest
        {
            OpenToken = openToken ?? openLiveStreamDto?.OpenToken,
            UserId = userId.Value,
            PlaySessionId = playSessionId ?? openLiveStreamDto?.PlaySessionId,
            MaxStreamingBitrate = maxStreamingBitrate ?? openLiveStreamDto?.MaxStreamingBitrate,
            StartTimeTicks = startTimeTicks ?? openLiveStreamDto?.StartTimeTicks,
            AudioStreamIndex = audioStreamIndex ?? openLiveStreamDto?.AudioStreamIndex,
            SubtitleStreamIndex = subtitleStreamIndex ?? openLiveStreamDto?.SubtitleStreamIndex,
            MaxAudioChannels = maxAudioChannels ?? openLiveStreamDto?.MaxAudioChannels,
            ItemId = itemId ?? openLiveStreamDto?.ItemId ?? Guid.Empty,
            DeviceProfile = openLiveStreamDto?.DeviceProfile,
            EnableDirectPlay = enableDirectPlay ?? openLiveStreamDto?.EnableDirectPlay ?? true,
            EnableDirectStream = enableDirectStream ?? openLiveStreamDto?.EnableDirectStream ?? true,
            DirectPlayProtocols = openLiveStreamDto?.DirectPlayProtocols ?? new[] { MediaProtocol.Http },
            AlwaysBurnInSubtitleWhenTranscoding = alwaysBurnInSubtitleWhenTranscoding ?? openLiveStreamDto?.AlwaysBurnInSubtitleWhenTranscoding ?? false
        };
        return await _mediaInfoHelper.OpenMediaSource(HttpContext, request).ConfigureAwait(false);
    }

    /// <summary>
    /// Closes a media source.
    /// </summary>
    /// <param name="liveStreamId">The livestream id.</param>
    /// <response code="204">Livestream closed.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("LiveStreams/Close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> CloseLiveStream([FromQuery, Required] string liveStreamId)
    {
        await _mediaSourceManager.CloseLiveStream(liveStreamId).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Tests the network with a request with the size of the bitrate.
    /// </summary>
    /// <param name="size">The bitrate. Defaults to 102400.</param>
    /// <response code="200">Test buffer returned.</response>
    /// <returns>A <see cref="FileResult"/> with specified bitrate.</returns>
    [HttpGet("Playback/BitrateTest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesFile(MediaTypeNames.Application.Octet)]
    public ActionResult GetBitrateTestBytes([FromQuery][Range(1, 100_000_000, ErrorMessage = "The requested size must be greater than or equal to {1} and less than or equal to {2}")] int size = 102400)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            Random.Shared.NextBytes(buffer);
            return File(buffer, MediaTypeNames.Application.Octet);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
