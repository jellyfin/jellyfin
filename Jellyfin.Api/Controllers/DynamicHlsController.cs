using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.StreamingDtos;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.MediaEncoding.Hls.Playlist;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Dynamic hls controller.
/// </summary>
[Route("")]
[Authorize]
[ApiExplorerSettings(IgnoreApi = true)]
public class DynamicHlsController : BaseJellyfinApiController
{
    private const TranscodingJobType TranscodingJobType = MediaBrowser.Controller.MediaEncoding.TranscodingJobType.Hls;

    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IFileSystem _fileSystem;
    private readonly ITranscodeManager _transcodeManager;
    private readonly ILogger<DynamicHlsController> _logger;
    private readonly EncodingHelper _encodingHelper;
    private readonly IDynamicHlsPlaylistGenerator _dynamicHlsPlaylistGenerator;
    private readonly DynamicHlsHelper _dynamicHlsHelper;
    private readonly EncodingOptions _encodingOptions;
    private readonly HlsCommandBuilder _hlsCommandBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicHlsController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="transcodeManager">Instance of the <see cref="ITranscodeManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{DynamicHlsController}"/> interface.</param>
    /// <param name="dynamicHlsHelper">Instance of <see cref="DynamicHlsHelper"/>.</param>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    /// <param name="dynamicHlsPlaylistGenerator">Instance of <see cref="IDynamicHlsPlaylistGenerator"/>.</param>
    /// <param name="hlsCommandBuilder">Instance of <see cref="HlsCommandBuilder"/>.</param>
    public DynamicHlsController(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IMediaSourceManager mediaSourceManager,
        IServerConfigurationManager serverConfigurationManager,
        IMediaEncoder mediaEncoder,
        IFileSystem fileSystem,
        ITranscodeManager transcodeManager,
        ILogger<DynamicHlsController> logger,
        DynamicHlsHelper dynamicHlsHelper,
        EncodingHelper encodingHelper,
        IDynamicHlsPlaylistGenerator dynamicHlsPlaylistGenerator,
        HlsCommandBuilder hlsCommandBuilder)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _mediaSourceManager = mediaSourceManager;
        _serverConfigurationManager = serverConfigurationManager;
        _mediaEncoder = mediaEncoder;
        _fileSystem = fileSystem;
        _transcodeManager = transcodeManager;
        _logger = logger;
        _dynamicHlsHelper = dynamicHlsHelper;
        _encodingHelper = encodingHelper;
        _dynamicHlsPlaylistGenerator = dynamicHlsPlaylistGenerator;

        _encodingOptions = serverConfigurationManager.GetEncodingOptions();
        _hlsCommandBuilder = hlsCommandBuilder;
    }

    /// <summary>
    /// Gets a hls live stream.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="container">The audio container.</param>
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
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3.</param>
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
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="maxWidth">Optional. The max width.</param>
    /// <param name="maxHeight">Optional. The max height.</param>
    /// <param name="enableSubtitlesInManifest">Optional. Whether to enable subtitles in the manifest.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <param name="alwaysBurnInSubtitleWhenTranscoding">Whether to always burn in subtitles when transcoding.</param>
    /// <response code="200">Hls live stream retrieved.</response>
    /// <returns>A <see cref="FileResult"/> containing the hls file.</returns>
    [HttpGet("Videos/{itemId}/live.m3u8")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesPlaylistFile]
    public async Task<ActionResult> GetLiveHlsStream(
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
        [FromQuery] int? maxWidth,
        [FromQuery] int? maxHeight,
        [FromQuery] bool? enableSubtitlesInManifest,
        [FromQuery] bool enableAudioVbrEncoding = true,
        [FromQuery] bool alwaysBurnInSubtitleWhenTranscoding = false)
    {
        VideoRequestDto streamingRequest = new VideoRequestDto
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
            VideoBitRate = videoBitRate,
            SubtitleStreamIndex = subtitleStreamIndex,
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.External,
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
            MaxHeight = maxHeight,
            MaxWidth = maxWidth,
            EnableSubtitlesInManifest = enableSubtitlesInManifest ?? true,
            EnableAudioVbrEncoding = enableAudioVbrEncoding,
            AlwaysBurnInSubtitleWhenTranscoding = alwaysBurnInSubtitleWhenTranscoding
        };

        // CTS lifecycle is managed internally.
        var cancellationTokenSource = new CancellationTokenSource();
        // Due to CTS.Token calling ThrowIfDisposed (https://github.com/dotnet/runtime/issues/29970) we have to "cache" the token
        // since it gets disposed when ffmpeg exits
        var cancellationToken = cancellationTokenSource.Token;
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
                TranscodingJobType,
                cancellationToken)
            .ConfigureAwait(false);

        TranscodingJob? job = null;
        var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".m3u8");

        if (!System.IO.File.Exists(playlistPath))
        {
            using (await _transcodeManager.LockAsync(playlistPath, cancellationToken).ConfigureAwait(false))
            {
                if (!System.IO.File.Exists(playlistPath))
                {
                    // If the playlist doesn't already exist, startup ffmpeg
                    try
                    {
                        job = await _transcodeManager.StartFfMpeg(
                                state,
                                playlistPath,
                                _hlsCommandBuilder.GetCommandLineArguments(playlistPath, state, _encodingOptions, true, 0),
                                Request.HttpContext.User.GetUserId(),
                                TranscodingJobType,
                                cancellationTokenSource)
                            .ConfigureAwait(false);
                        job.IsLiveOutput = true;
                    }
                    catch
                    {
                        state.Dispose();
                        throw;
                    }

                    minSegments = state.MinSegments;
                    if (minSegments > 0)
                    {
                        await HlsHelpers.WaitForMinimumSegmentCount(playlistPath, minSegments, _logger, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        job ??= _transcodeManager.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);

        if (job is not null)
        {
            _transcodeManager.OnTranscodeEndRequest(job);
        }

        var playlistText = HlsHelpers.GetLivePlaylistText(playlistPath, state);

        return Content(playlistText, MimeTypes.GetMimeType("playlist.m3u8"));
    }

    /// <summary>
    /// Gets a video hls playlist stream.
    /// </summary>
    /// <param name="itemId">The item id.</param>
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
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3.</param>
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
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAdaptiveBitrateStreaming">Enable adaptive bitrate streaming.</param>
    /// <param name="enableTrickplay">Enable trickplay image playlists being added to master playlist.</param>
    /// <param name="enableAudioVbrEncoding">Whether to enable Audio Encoding.</param>
    /// <param name="alwaysBurnInSubtitleWhenTranscoding">Whether to always burn in subtitles when transcoding.</param>
    /// <response code="200">Video stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the playlist file.</returns>
    [HttpGet("Videos/{itemId}/master.m3u8")]
    [HttpHead("Videos/{itemId}/master.m3u8", Name = "HeadMasterHlsVideoPlaylist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesPlaylistFile]
    public async Task<ActionResult> GetMasterHlsVideoPlaylist(
        [FromRoute, Required] Guid itemId,
        [FromQuery] bool? @static,
        [FromQuery] string? @params,
        [FromQuery] string? tag,
        [FromQuery, ParameterObsolete] string? deviceProfileId,
        [FromQuery] string? playSessionId,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? segmentContainer,
        [FromQuery] int? segmentLength,
        [FromQuery] int? minSegments,
        [FromQuery, Required] string mediaSourceId,
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
        [FromQuery] bool enableAdaptiveBitrateStreaming = false,
        [FromQuery] bool enableTrickplay = true,
        [FromQuery] bool enableAudioVbrEncoding = true,
        [FromQuery] bool alwaysBurnInSubtitleWhenTranscoding = false)
    {
        var streamingRequest = new HlsVideoRequestDto
        {
            Id = itemId,
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
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.External,
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
            EnableAdaptiveBitrateStreaming = enableAdaptiveBitrateStreaming,
            EnableTrickplay = enableTrickplay,
            EnableAudioVbrEncoding = enableAudioVbrEncoding,
            AlwaysBurnInSubtitleWhenTranscoding = alwaysBurnInSubtitleWhenTranscoding
        };

        return await _dynamicHlsHelper.GetMasterHlsPlaylist(TranscodingJobType, streamingRequest, enableAdaptiveBitrateStreaming).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an audio hls playlist stream.
    /// </summary>
    /// <param name="itemId">The item id.</param>
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
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3.</param>
    /// <param name="enableAutoStreamCopy">Whether or not to allow automatic stream copy if requested values match the original source. Defaults to true.</param>
    /// <param name="allowVideoStreamCopy">Whether or not to allow copying of the video stream url.</param>
    /// <param name="allowAudioStreamCopy">Whether or not to allow copying of the audio stream url.</param>
    /// <param name="audioSampleRate">Optional. Specify a specific audio sample rate, e.g. 44100.</param>
    /// <param name="maxAudioBitDepth">Optional. The maximum audio bit depth.</param>
    /// <param name="maxStreamingBitrate">Optional. The maximum streaming bitrate.</param>
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
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAdaptiveBitrateStreaming">Enable adaptive bitrate streaming.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <response code="200">Audio stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the playlist file.</returns>
    [HttpGet("Audio/{itemId}/master.m3u8")]
    [HttpHead("Audio/{itemId}/master.m3u8", Name = "HeadMasterHlsAudioPlaylist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesPlaylistFile]
    public async Task<ActionResult> GetMasterHlsAudioPlaylist(
        [FromRoute, Required] Guid itemId,
        [FromQuery] bool? @static,
        [FromQuery] string? @params,
        [FromQuery] string? tag,
        [FromQuery, ParameterObsolete] string? deviceProfileId,
        [FromQuery] string? playSessionId,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? segmentContainer,
        [FromQuery] int? segmentLength,
        [FromQuery] int? minSegments,
        [FromQuery, Required] string mediaSourceId,
        [FromQuery] string? deviceId,
        [FromQuery][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string? audioCodec,
        [FromQuery] bool? enableAutoStreamCopy,
        [FromQuery] bool? allowVideoStreamCopy,
        [FromQuery] bool? allowAudioStreamCopy,
        [FromQuery] int? audioSampleRate,
        [FromQuery] int? maxAudioBitDepth,
        [FromQuery] int? maxStreamingBitrate,
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
        [FromQuery] bool enableAdaptiveBitrateStreaming = false,
        [FromQuery] bool enableAudioVbrEncoding = true)
    {
        var streamingRequest = new HlsAudioRequestDto
        {
            Id = itemId,
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
            AudioBitRate = audioBitRate ?? maxStreamingBitrate,
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
            VideoBitRate = videoBitRate,
            SubtitleStreamIndex = subtitleStreamIndex,
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.External,
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
            EnableAdaptiveBitrateStreaming = enableAdaptiveBitrateStreaming,
            EnableAudioVbrEncoding = enableAudioVbrEncoding,
            AlwaysBurnInSubtitleWhenTranscoding = false
        };

        return await _dynamicHlsHelper.GetMasterHlsPlaylist(TranscodingJobType, streamingRequest, enableAdaptiveBitrateStreaming).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a video stream using HTTP live streaming.
    /// </summary>
    /// <param name="itemId">The item id.</param>
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
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3.</param>
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
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <param name="alwaysBurnInSubtitleWhenTranscoding">Whether to always burn in subtitles when transcoding.</param>
    /// <response code="200">Video stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("Videos/{itemId}/main.m3u8")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesPlaylistFile]
    public async Task<ActionResult> GetVariantHlsVideoPlaylist(
        [FromRoute, Required] Guid itemId,
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
        [FromQuery] bool enableAudioVbrEncoding = true,
        [FromQuery] bool alwaysBurnInSubtitleWhenTranscoding = false)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var streamingRequest = new VideoRequestDto
        {
            Id = itemId,
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
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.External,
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
            EnableAudioVbrEncoding = enableAudioVbrEncoding,
            AlwaysBurnInSubtitleWhenTranscoding = alwaysBurnInSubtitleWhenTranscoding
        };

        return await GetVariantPlaylistInternal(streamingRequest, cancellationTokenSource)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an audio stream using HTTP live streaming.
    /// </summary>
    /// <param name="itemId">The item id.</param>
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
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3.</param>
    /// <param name="enableAutoStreamCopy">Whether or not to allow automatic stream copy if requested values match the original source. Defaults to true.</param>
    /// <param name="allowVideoStreamCopy">Whether or not to allow copying of the video stream url.</param>
    /// <param name="allowAudioStreamCopy">Whether or not to allow copying of the audio stream url.</param>
    /// <param name="audioSampleRate">Optional. Specify a specific audio sample rate, e.g. 44100.</param>
    /// <param name="maxAudioBitDepth">Optional. The maximum audio bit depth.</param>
    /// <param name="maxStreamingBitrate">Optional. The maximum streaming bitrate.</param>
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
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <response code="200">Audio stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("Audio/{itemId}/main.m3u8")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesPlaylistFile]
    public async Task<ActionResult> GetVariantHlsAudioPlaylist(
        [FromRoute, Required] Guid itemId,
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
        [FromQuery] int? maxStreamingBitrate,
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
        using var cancellationTokenSource = new CancellationTokenSource();
        var streamingRequest = new StreamingRequestDto
        {
            Id = itemId,
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
            AudioBitRate = audioBitRate ?? maxStreamingBitrate,
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
            VideoBitRate = videoBitRate,
            SubtitleStreamIndex = subtitleStreamIndex,
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.External,
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
            EnableAudioVbrEncoding = enableAudioVbrEncoding,
            AlwaysBurnInSubtitleWhenTranscoding = false
        };

        return await GetVariantPlaylistInternal(streamingRequest, cancellationTokenSource)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a video stream using HTTP live streaming.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="segmentId">The segment id.</param>
    /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
    /// <param name="runtimeTicks">The position of the requested segment in ticks.</param>
    /// <param name="actualSegmentLengthTicks">The length of the requested segment in ticks.</param>
    /// <param name="static">Optional. If true, the original file will be streamed statically without any encoding. Use either no url extension or the original file extension. true/false.</param>
    /// <param name="params">The streaming parameters.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="deviceProfileId">Optional. The dlna device profile id to utilize.</param>
    /// <param name="playSessionId">The play session id.</param>
    /// <param name="segmentContainer">The segment container.</param>
    /// <param name="segmentLength">The desired segment length.</param>
    /// <param name="minSegments">The minimum number of segments.</param>
    /// <param name="mediaSourceId">The media version id, if playing an alternate version.</param>
    /// <param name="deviceId">The device id of the client requesting. Used to stop encoding processes when needed.</param>
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3.</param>
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
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <param name="alwaysBurnInSubtitleWhenTranscoding">Whether to always burn in subtitles when transcoding.</param>
    /// <response code="200">Video stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("Videos/{itemId}/hls1/{playlistId}/{segmentId}.{container}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesVideoFile]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "playlistId", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult> GetHlsVideoSegment(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] string playlistId,
        [FromRoute, Required] int segmentId,
        [FromRoute, Required][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string container,
        [FromQuery, Required] long runtimeTicks,
        [FromQuery, Required] long actualSegmentLengthTicks,
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
        [FromQuery] bool enableAudioVbrEncoding = true,
        [FromQuery] bool alwaysBurnInSubtitleWhenTranscoding = false)
    {
        var streamingRequest = new VideoRequestDto
        {
            Id = itemId,
            CurrentRuntimeTicks = runtimeTicks,
            ActualSegmentLengthTicks = actualSegmentLengthTicks,
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
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.External,
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
            EnableAudioVbrEncoding = enableAudioVbrEncoding,
            AlwaysBurnInSubtitleWhenTranscoding = alwaysBurnInSubtitleWhenTranscoding
        };

        return await GetDynamicSegment(streamingRequest, segmentId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a video stream using HTTP live streaming.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="playlistId">The playlist id.</param>
    /// <param name="segmentId">The segment id.</param>
    /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
    /// <param name="runtimeTicks">The position of the requested segment in ticks.</param>
    /// <param name="actualSegmentLengthTicks">The length of the requested segment in ticks.</param>
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
    /// <param name="audioCodec">Optional. Specify an audio codec to encode to, e.g. mp3.</param>
    /// <param name="enableAutoStreamCopy">Whether or not to allow automatic stream copy if requested values match the original source. Defaults to true.</param>
    /// <param name="allowVideoStreamCopy">Whether or not to allow copying of the video stream url.</param>
    /// <param name="allowAudioStreamCopy">Whether or not to allow copying of the audio stream url.</param>
    /// <param name="audioSampleRate">Optional. Specify a specific audio sample rate, e.g. 44100.</param>
    /// <param name="maxAudioBitDepth">Optional. The maximum audio bit depth.</param>
    /// <param name="maxStreamingBitrate">Optional. The maximum streaming bitrate.</param>
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
    /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264.</param>
    /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
    /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
    /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
    /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
    /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
    /// <param name="streamOptions">Optional. The streaming options.</param>
    /// <param name="enableAudioVbrEncoding">Optional. Whether to enable Audio Encoding.</param>
    /// <response code="200">Video stream returned.</response>
    /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
    [HttpGet("Audio/{itemId}/hls1/{playlistId}/{segmentId}.{container}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesAudioFile]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "playlistId", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult> GetHlsAudioSegment(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] string playlistId,
        [FromRoute, Required] int segmentId,
        [FromRoute, Required][RegularExpression(EncodingHelper.ContainerValidationRegexStr)] string container,
        [FromQuery, Required] long runtimeTicks,
        [FromQuery, Required] long actualSegmentLengthTicks,
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
        [FromQuery] int? maxStreamingBitrate,
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
        var streamingRequest = new StreamingRequestDto
        {
            Id = itemId,
            Container = container,
            CurrentRuntimeTicks = runtimeTicks,
            ActualSegmentLengthTicks = actualSegmentLengthTicks,
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
            AudioBitRate = audioBitRate ?? maxStreamingBitrate,
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
            VideoBitRate = videoBitRate,
            SubtitleStreamIndex = subtitleStreamIndex,
            SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.External,
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
            EnableAudioVbrEncoding = enableAudioVbrEncoding,
            AlwaysBurnInSubtitleWhenTranscoding = false
        };

        return await GetDynamicSegment(streamingRequest, segmentId)
            .ConfigureAwait(false);
    }

    private async Task<ActionResult> GetVariantPlaylistInternal(StreamingRequestDto streamingRequest, CancellationTokenSource cancellationTokenSource)
    {
        using var state = await StreamingHelpers.GetStreamingState(
                streamingRequest,
                HttpContext,
                _mediaSourceManager,
                _userManager,
                _libraryManager,
                _serverConfigurationManager,
                _mediaEncoder,
                _encodingHelper,
                _transcodeManager,
                TranscodingJobType,
                cancellationTokenSource.Token)
            .ConfigureAwait(false);
        var mediaSourceId = state.BaseRequest.MediaSourceId;
        double fps = state.TargetFramerate ?? 0.0f;
        int segmentLength = state.SegmentLength * 1000;

        // If video is transcoded and framerate is fractional (i.e. 23.976), we need to slightly adjust segment length
        if (!EncodingHelper.IsCopyCodec(state.OutputVideoCodec) && Math.Abs(fps - Math.Floor(fps + 0.001f)) > 0.001)
        {
            double nearestIntFramerate = Math.Ceiling(fps);
            segmentLength = (int)Math.Ceiling(segmentLength * (nearestIntFramerate / fps));
        }

        var request = new CreateMainPlaylistRequest(
            mediaSourceId is null ? null : Guid.Parse(mediaSourceId),
            state.MediaPath,
            segmentLength,
            state.RunTimeTicks ?? 0,
            state.Request.SegmentContainer ?? string.Empty,
            "hls1/main/",
            Request.QueryString.ToString(),
            EncodingHelper.IsCopyCodec(state.OutputVideoCodec));
        var playlist = _dynamicHlsPlaylistGenerator.CreateMainPlaylist(request);

        return new FileContentResult(Encoding.UTF8.GetBytes(playlist), MimeTypes.GetMimeType("playlist.m3u8"));
    }

    private async Task<ActionResult> GetDynamicSegment(StreamingRequestDto streamingRequest, int segmentId)
    {
        if ((streamingRequest.StartTimeTicks ?? 0) > 0)
        {
            throw new ArgumentException("StartTimeTicks is not allowed.");
        }

        var cancellationToken = HttpContext.RequestAborted;

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
                TranscodingJobType,
                cancellationToken)
            .ConfigureAwait(false);

        var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".m3u8");

        var segmentPath = GetSegmentPath(state, playlistPath, segmentId);

        var segmentExtension = EncodingHelper.GetSegmentFileExtension(state.Request.SegmentContainer);

        TranscodingJob? job = null;
        TranscodingJob? jobToWaitFor = null;
        CancellationTokenSource? cancellationTokenSource = null;
        try
        {
            while (true)
            {
                // Readers must be able to acquire the lock while a replacement waits for them to finish.
                await WaitForActiveTranscodingRequests(jobToWaitFor, cancellationToken).ConfigureAwait(false);

                // Selection, reader registration and replacement remain atomic per playlist.
                using (await _transcodeManager.LockAsync(playlistPath, cancellationToken).ConfigureAwait(false))
                {
                    var startTranscoding = false;
                    if (System.IO.File.Exists(segmentPath))
                    {
                        job = _transcodeManager.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
                        _logger.LogDebug("returning {0} [it exists]", segmentPath);
                        break;
                    }

                    var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);
                    var segmentGapRequiringTranscodingChange = 24 / state.SegmentLength;

                    if (segmentId == -1)
                    {
                        _logger.LogDebug("Starting transcoding because fmp4 init file is being requested");
                        startTranscoding = true;
                    }
                    else if (currentTranscodingIndex is null)
                    {
                        _logger.LogDebug("Starting transcoding because currentTranscodingIndex=null");
                        startTranscoding = true;
                    }
                    else if (segmentId < currentTranscodingIndex.Value)
                    {
                        _logger.LogDebug("Starting transcoding because requestedIndex={0} and currentTranscodingIndex={1}", segmentId, currentTranscodingIndex);
                        startTranscoding = true;
                    }
                    else if (segmentId - currentTranscodingIndex.Value > segmentGapRequiringTranscodingChange)
                    {
                        _logger.LogDebug("Starting transcoding because segmentGap is {0} and max allowed gap is {1}. requestedIndex={2}", segmentId - currentTranscodingIndex.Value, segmentGapRequiringTranscodingChange, segmentId);
                        startTranscoding = true;
                    }

                    if (startTranscoding)
                    {
                        jobToWaitFor = _transcodeManager.GetTranscodingJob(playlistPath, TranscodingJobType);
                        if (jobToWaitFor?.ActiveRequestCount > 0)
                        {
                            continue;
                        }

                        try
                        {
                            await _transcodeManager.KillTranscodingJobs(streamingRequest.DeviceId, streamingRequest.PlaySessionId, p => false)
                                .ConfigureAwait(false);

                            if (currentTranscodingIndex.HasValue)
                            {
                                await DeleteLastFile(playlistPath, segmentExtension, 0).ConfigureAwait(false);
                            }

                            // Preserve the init request's identity until replacement is actually selected.
                            if (segmentId == -1)
                            {
                                segmentId = 0;
                            }

                            streamingRequest.StartTimeTicks = streamingRequest.CurrentRuntimeTicks;
                            state.WaitForPath = segmentPath;

                            // The shared FFmpeg job must outlive cancellation of any one HTTP request.
                            cancellationTokenSource = new CancellationTokenSource();
                            job = await _transcodeManager.StartFfMpeg(
                                state,
                                playlistPath,
                                _hlsCommandBuilder.GetCommandLineArguments(playlistPath, state, _encodingOptions, false, segmentId),
                                Request.HttpContext.User.GetUserId(),
                                TranscodingJobType,
                                cancellationTokenSource).ConfigureAwait(false);
                        }
                        catch
                        {
                            if (cancellationTokenSource is not null)
                            {
                                var startedJob = state.TranscodingJob ?? _transcodeManager.GetTranscodingJob(playlistPath, TranscodingJobType);
                                if (startedJob?.CancellationTokenSource == cancellationTokenSource)
                                {
                                    job = startedJob;
                                }
                            }

                            state.Dispose();
                            throw;
                        }
                    }
                    else
                    {
                        job = _transcodeManager.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
                        if (job?.TranscodingThrottler is not null)
                        {
                            await job.TranscodingThrottler.UnpauseTranscoding().ConfigureAwait(false);
                        }
                    }

                    _logger.LogDebug("returning {0} [general case]", segmentPath);
                    job ??= _transcodeManager.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
                    break;
                }
            }

            return await GetSegmentResult(state, playlistPath, segmentPath, segmentExtension, segmentId, job, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // OnCompleted is only registered once a response is ready.
            if (job is not null)
            {
                _transcodeManager.OnTranscodeEndRequest(job);
            }

            throw;
        }
        finally
        {
            if (job is null)
            {
                cancellationTokenSource?.Dispose();
            }
        }
    }

    internal static async Task WaitForActiveTranscodingRequests(TranscodingJob? job, CancellationToken cancellationToken)
    {
        while (job?.ActiveRequestCount > 0)
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }
    }

    private static double[] GetSegmentLengths(StreamState state)
        => GetSegmentLengthsInternal(state.RunTimeTicks ?? 0, state.SegmentLength);

    internal static double[] GetSegmentLengthsInternal(long runtimeTicks, int segmentlength)
    {
        var segmentLengthTicks = TimeSpan.FromSeconds(segmentlength).Ticks;
        var wholeSegments = runtimeTicks / segmentLengthTicks;
        var remainingTicks = runtimeTicks % segmentLengthTicks;

        var segmentsLen = wholeSegments + (remainingTicks == 0 ? 0 : 1);
        var segments = new double[segmentsLen];
        for (int i = 0; i < wholeSegments; i++)
        {
            segments[i] = segmentlength;
        }

        if (remainingTicks != 0)
        {
            segments[^1] = TimeSpan.FromTicks(remainingTicks).TotalSeconds;
        }

        return segments;
    }

    private string GetSegmentPath(StreamState state, string playlist, int index)
    {
        var folder = Path.GetDirectoryName(playlist) ?? throw new ArgumentException($"Provided path ({playlist}) is not valid.", nameof(playlist));
        var filename = Path.GetFileNameWithoutExtension(playlist);

        return Path.Combine(folder, filename + index.ToString(CultureInfo.InvariantCulture) + EncodingHelper.GetSegmentFileExtension(state.Request.SegmentContainer));
    }

    private async Task<ActionResult> GetSegmentResult(
        StreamState state,
        string playlistPath,
        string segmentPath,
        string segmentExtension,
        int segmentIndex,
        TranscodingJob? transcodingJob,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var segmentExists = System.IO.File.Exists(segmentPath);
        if (segmentExists)
        {
            if (transcodingJob is not null && transcodingJob.HasExited)
            {
                // Transcoding job is over, so assume all existing files are ready
                _logger.LogDebug("serving up {0} as transcode is over", segmentPath);
                return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
            }

            var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);

            // If requested segment is less than transcoding position, we can't transcode backwards, so assume it's ready
            if (segmentIndex < currentTranscodingIndex)
            {
                _logger.LogDebug("serving up {0} as transcode index {1} is past requested point {2}", segmentPath, currentTranscodingIndex, segmentIndex);
                return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
            }
        }

        var nextSegmentPath = GetSegmentPath(state, playlistPath, segmentIndex + 1);
        if (transcodingJob is not null)
        {
            while (!cancellationToken.IsCancellationRequested && !transcodingJob.HasExited)
            {
                // To be considered ready, the segment file has to exist AND
                // either the transcoding job should be done or next segment should also exist
                if (segmentExists)
                {
                    if (transcodingJob.HasExited || System.IO.File.Exists(nextSegmentPath))
                    {
                        _logger.LogDebug("Serving up {SegmentPath} as it deemed ready", segmentPath);
                        return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
                    }
                }
                else
                {
                    segmentExists = System.IO.File.Exists(segmentPath);
                    if (segmentExists)
                    {
                        continue; // avoid unnecessary waiting if segment just became available
                    }
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            if (!System.IO.File.Exists(segmentPath))
            {
                _logger.LogWarning("cannot serve {0} as transcoding quit before we got there", segmentPath);
            }
            else
            {
                _logger.LogDebug("serving {0} as it's on disk and transcoding stopped", segmentPath);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
        else
        {
            _logger.LogWarning("cannot serve {0} as it doesn't exist and no transcode is running", segmentPath);
        }

        return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
    }

    private ActionResult GetSegmentResult(StreamState state, string segmentPath, int segmentIndex, TranscodingJob? transcodingJob)
    {
        var segmentEndingPositionTicks = state.Request.CurrentRuntimeTicks + state.Request.ActualSegmentLengthTicks;
        var result = FileStreamResponseHelpers.GetStaticFileResult(segmentPath, MimeTypes.GetMimeType(segmentPath));

        Response.OnCompleted(() =>
        {
            _logger.LogDebug("Finished serving {SegmentPath}", segmentPath);
            if (transcodingJob is not null)
            {
                transcodingJob.DownloadPositionTicks = Math.Max(transcodingJob.DownloadPositionTicks ?? segmentEndingPositionTicks, segmentEndingPositionTicks);
                transcodingJob.ReportSegmentDownloaded(segmentIndex, segmentEndingPositionTicks);
                _transcodeManager.OnTranscodeEndRequest(transcodingJob);
            }

            return Task.CompletedTask;
        });

        return result;
    }

    private int? GetCurrentTranscodingIndex(string playlist, string segmentExtension)
    {
        var job = _transcodeManager.GetTranscodingJob(playlist, TranscodingJobType);

        if (job is null || job.HasExited)
        {
            return null;
        }

        var file = GetLastTranscodingFile(playlist, segmentExtension, _fileSystem);

        if (file is null)
        {
            return null;
        }

        var playlistFilename = Path.GetFileNameWithoutExtension(playlist.AsSpan());

        var indexString = Path.GetFileNameWithoutExtension(file.Name.AsSpan()).Slice(playlistFilename.Length);

        return int.Parse(indexString, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static FileSystemMetadata? GetLastTranscodingFile(string playlist, string segmentExtension, IFileSystem fileSystem)
    {
        var folder = Path.GetDirectoryName(playlist) ?? throw new ArgumentException("Path can't be a root directory.", nameof(playlist));

        var filePrefix = Path.GetFileNameWithoutExtension(playlist);

        try
        {
            return fileSystem.GetFiles(folder, new[] { segmentExtension }, true, false)
                .Where(i => Path.GetFileNameWithoutExtension(i.Name).StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                .MaxBy(fileSystem.GetLastWriteTimeUtc);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private async Task DeleteLastFile(string playlistPath, string segmentExtension, int retryCount)
    {
        var file = GetLastTranscodingFile(playlistPath, segmentExtension, _fileSystem);

        if (file is null)
        {
            return;
        }

        await DeleteFile(file.FullName, retryCount).ConfigureAwait(false);
    }

    private async Task DeleteFile(string path, int retryCount)
    {
        if (retryCount >= 5)
        {
            return;
        }

        _logger.LogDebug("Deleting partial HLS file {Path}", path);

        try
        {
            _fileSystem.DeleteFile(path);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error deleting partial stream file(s) {Path}", path);

            await Task.Delay(100).ConfigureAwait(false);
            await DeleteFile(path, retryCount + 1).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting partial stream file(s) {Path}", path);
        }
    }
}
