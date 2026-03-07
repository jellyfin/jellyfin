using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Dynamic hls helper.
/// </summary>
public class DynamicHlsHelper
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly ITranscodeManager _transcodeManager;
    private readonly INetworkManager _networkManager;
    private readonly ILogger<DynamicHlsHelper> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EncodingHelper _encodingHelper;
    private readonly ITrickplayManager _trickplayManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicHlsHelper"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="transcodeManager">Instance of <see cref="ITranscodeManager"/>.</param>
    /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{DynamicHlsHelper}"/> interface.</param>
    /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    /// <param name="trickplayManager">Instance of <see cref="ITrickplayManager"/>.</param>
    public DynamicHlsHelper(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IMediaSourceManager mediaSourceManager,
        IServerConfigurationManager serverConfigurationManager,
        IMediaEncoder mediaEncoder,
        ITranscodeManager transcodeManager,
        INetworkManager networkManager,
        ILogger<DynamicHlsHelper> logger,
        IHttpContextAccessor httpContextAccessor,
        EncodingHelper encodingHelper,
        ITrickplayManager trickplayManager)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _mediaSourceManager = mediaSourceManager;
        _serverConfigurationManager = serverConfigurationManager;
        _mediaEncoder = mediaEncoder;
        _transcodeManager = transcodeManager;
        _networkManager = networkManager;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _encodingHelper = encodingHelper;
        _trickplayManager = trickplayManager;
    }

    /// <summary>
    /// Get master hls playlist.
    /// </summary>
    /// <param name="transcodingJobType">Transcoding job type.</param>
    /// <param name="streamingRequest">Streaming request dto.</param>
    /// <param name="enableAdaptiveBitrateStreaming">Enable adaptive bitrate streaming.</param>
    /// <returns>A <see cref="Task"/> containing the resulting <see cref="ActionResult"/>.</returns>
    public async Task<ActionResult> GetMasterHlsPlaylist(
        TranscodingJobType transcodingJobType,
        StreamingRequestDto streamingRequest,
        bool enableAdaptiveBitrateStreaming)
    {
        var isHeadRequest = _httpContextAccessor.HttpContext?.Request.Method == WebRequestMethods.Http.Head;
        // CTS lifecycle is managed internally.
        var cancellationTokenSource = new CancellationTokenSource();
        return await GetMasterPlaylistInternal(
            streamingRequest,
            isHeadRequest,
            enableAdaptiveBitrateStreaming,
            transcodingJobType,
            cancellationTokenSource).ConfigureAwait(false);
    }

    private async Task<ActionResult> GetMasterPlaylistInternal(
        StreamingRequestDto streamingRequest,
        bool isHeadRequest,
        bool enableAdaptiveBitrateStreaming,
        TranscodingJobType transcodingJobType,
        CancellationTokenSource cancellationTokenSource)
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            throw new ResourceNotFoundException(nameof(_httpContextAccessor.HttpContext));
        }

        using var state = await StreamingHelpers.GetStreamingState(
                streamingRequest,
                _httpContextAccessor.HttpContext,
                _mediaSourceManager,
                _userManager,
                _libraryManager,
                _serverConfigurationManager,
                _mediaEncoder,
                _encodingHelper,
                _transcodeManager,
                transcodingJobType,
                cancellationTokenSource.Token)
            .ConfigureAwait(false);

        _httpContextAccessor.HttpContext.Response.Headers.Append(HeaderNames.Expires, "0");
        if (isHeadRequest)
        {
            return new FileContentResult(Array.Empty<byte>(), MimeTypes.GetMimeType("playlist.m3u8"));
        }

        var totalBitrate = (state.OutputAudioBitrate ?? 0) + (state.OutputVideoBitrate ?? 0);

        var builder = new StringBuilder();

        builder.AppendLine("#EXTM3U");

        var isLiveStream = state.IsSegmentedLiveStream;

        var queryString = _httpContextAccessor.HttpContext.Request.QueryString.ToString();

        // from universal audio service, need to override the AudioCodec when the actual request differs from original query
        if (!string.Equals(state.OutputAudioCodec, _httpContextAccessor.HttpContext.Request.Query["AudioCodec"].ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var newQuery = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);
            newQuery["AudioCodec"] = state.OutputAudioCodec;
            queryString = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(string.Empty, newQuery);
        }

        // from universal audio service
        if (!string.IsNullOrWhiteSpace(state.Request.SegmentContainer)
            && !queryString.Contains("SegmentContainer", StringComparison.OrdinalIgnoreCase))
        {
            queryString += "&SegmentContainer=" + state.Request.SegmentContainer;
        }

        // from universal audio service
        if (!string.IsNullOrWhiteSpace(state.Request.TranscodeReasons)
            && !queryString.Contains("TranscodeReasons=", StringComparison.OrdinalIgnoreCase))
        {
            queryString += "&TranscodeReasons=" + state.Request.TranscodeReasons;
        }

        // Video rotation metadata is only supported in fMP4 remuxing
        if (state.VideoStream is not null
            && state.VideoRequest is not null
            && (state.VideoStream?.Rotation ?? 0) != 0
            && EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
            && !string.IsNullOrWhiteSpace(state.Request.SegmentContainer)
            && !string.Equals(state.Request.SegmentContainer, "mp4", StringComparison.OrdinalIgnoreCase))
        {
            queryString += "&AllowVideoStreamCopy=false";
        }

        // Main stream
        var baseUrl = isLiveStream ? "live.m3u8" : "main.m3u8";
        var playlistUrl = baseUrl + queryString;
        var playlistQuery = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(queryString);

        var subtitleStreams = state.MediaSource
            .MediaStreams
            .Where(i => i.IsTextSubtitleStream)
            .ToList();

        var subtitleGroup = subtitleStreams.Count > 0 && (state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Hls || state.VideoRequest!.EnableSubtitlesInManifest)
            ? "subs"
            : null;

        // If we're burning in subtitles then don't add additional subs to the manifest
        if (state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
        {
            subtitleGroup = null;
        }

        if (!string.IsNullOrWhiteSpace(subtitleGroup))
        {
            AddSubtitles(state, subtitleStreams, builder, _httpContextAccessor.HttpContext.User);
        }

        var basicPlaylist = AppendPlaylist(builder, state, playlistUrl, totalBitrate, subtitleGroup);

        if (state.VideoStream is not null && state.VideoRequest is not null)
        {
            var encodingOptions = _serverConfigurationManager.GetEncodingOptions();

            // Provide AV1 and HEVC SDR entrances for backward compatibility.
            foreach (var sdrVideoCodec in new[] { "av1", "hevc" })
            {
                var isAv1EncodingAllowed = encodingOptions.AllowAv1Encoding
                    && string.Equals(sdrVideoCodec, "av1", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(state.ActualOutputVideoCodec, "av1", StringComparison.OrdinalIgnoreCase);
                var isHevcEncodingAllowed = encodingOptions.AllowHevcEncoding
                    && string.Equals(sdrVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase);
                var isEncodingAllowed = isAv1EncodingAllowed || isHevcEncodingAllowed;

                if (isEncodingAllowed
                    && EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
                    && state.VideoStream.VideoRange == VideoRange.HDR)
                {
                    // Force AV1 and HEVC Main Profile and disable video stream copy.
                    state.OutputVideoCodec = sdrVideoCodec;

                    var sdrPlaylistQuery = playlistQuery;
                    sdrPlaylistQuery["VideoCodec"] = sdrVideoCodec;
                    sdrPlaylistQuery[sdrVideoCodec + "-profile"] = "main";
                    sdrPlaylistQuery["AllowVideoStreamCopy"] = "false";

                    var sdrVideoUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(baseUrl, sdrPlaylistQuery);

                    // HACK: Use the same bitrate so that the client can choose by other attributes, such as color range.
                    AppendPlaylist(builder, state, sdrVideoUrl, totalBitrate, subtitleGroup);

                    // Restore the video codec
                    state.OutputVideoCodec = "copy";
                }
            }

            // Provide H.264 SDR entrance for backward compatibility.
            if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
                && state.VideoStream.VideoRange == VideoRange.HDR)
            {
                // Force H.264 and disable video stream copy.
                state.OutputVideoCodec = "h264";

                var sdrPlaylistQuery = playlistQuery;
                sdrPlaylistQuery["VideoCodec"] = "h264";
                sdrPlaylistQuery["AllowVideoStreamCopy"] = "false";

                var sdrVideoUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(baseUrl, sdrPlaylistQuery);

                // HACK: Use the same bitrate so that the client can choose by other attributes, such as color range.
                AppendPlaylist(builder, state, sdrVideoUrl, totalBitrate, subtitleGroup);

                // Restore the video codec
                state.OutputVideoCodec = "copy";
            }

            // Provide Level 5.0 entrance for backward compatibility.
            // e.g. Apple A10 chips refuse the master playlist containing SDR HEVC Main Level 5.1 video,
            // but in fact it is capable of playing videos up to Level 6.1.
            if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
                && state.VideoStream.Level.HasValue
                && state.VideoStream.Level > 150
                && state.VideoStream.VideoRange == VideoRange.SDR
                && string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase))
            {
                var playlistCodecsField = new StringBuilder();
                AppendPlaylistCodecsField(playlistCodecsField, state);

                // Force the video level to 5.0.
                var originalLevel = state.VideoStream.Level;
                state.VideoStream.Level = 150;
                var newPlaylistCodecsField = new StringBuilder();
                AppendPlaylistCodecsField(newPlaylistCodecsField, state);

                // Restore the video level.
                state.VideoStream.Level = originalLevel;
                var newPlaylist = ReplacePlaylistCodecsField(basicPlaylist, playlistCodecsField, newPlaylistCodecsField);
                builder.Append(newPlaylist);
            }
        }

        if (EnableAdaptiveBitrateStreaming(state, isLiveStream, enableAdaptiveBitrateStreaming, _httpContextAccessor.HttpContext.GetNormalizedRemoteIP()))
        {
            var requestedVideoBitrate = state.VideoRequest?.VideoBitRate ?? 0;

            // By default, vary by just 200k
            var variation = GetBitrateVariation(totalBitrate);

            var newBitrate = totalBitrate - variation;
            var variantQuery = playlistQuery;
            variantQuery["VideoBitrate"] = (requestedVideoBitrate - variation).ToString(CultureInfo.InvariantCulture);
            var variantUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(baseUrl, variantQuery);
            AppendPlaylist(builder, state, variantUrl, newBitrate, subtitleGroup);

            variation *= 2;
            newBitrate = totalBitrate - variation;
            variantQuery["VideoBitrate"] = (requestedVideoBitrate - variation).ToString(CultureInfo.InvariantCulture);
            variantUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(baseUrl, variantQuery);
            AppendPlaylist(builder, state, variantUrl, newBitrate, subtitleGroup);
        }

        // Add variants for pre-encoded alternate versions (bitrate ladder support)
        // This enables true ABR switching between different source files mid-stream
        if (!isLiveStream && enableAdaptiveBitrateStreaming)
        {
            await AppendAlternateSourceVariants(
                builder,
                state,
                streamingRequest,
                baseUrl,
                playlistQuery,
                subtitleGroup,
                cancellationTokenSource.Token).ConfigureAwait(false);
        }

        if (!isLiveStream && (state.VideoRequest?.EnableTrickplay ?? false))
        {
            var sourceId = Guid.Parse(state.Request.MediaSourceId);
            var trickplayResolutions = await _trickplayManager.GetTrickplayResolutions(sourceId).ConfigureAwait(false);
            AddTrickplay(state, trickplayResolutions, builder, _httpContextAccessor.HttpContext.User);
        }

        return new FileContentResult(Encoding.UTF8.GetBytes(builder.ToString()), MimeTypes.GetMimeType("playlist.m3u8"));
    }

    private StringBuilder AppendPlaylist(StringBuilder builder, StreamState state, string url, int bitrate, string? subtitleGroup)
    {
        var playlistBuilder = new StringBuilder();
        playlistBuilder.Append("#EXT-X-STREAM-INF:BANDWIDTH=")
            .Append(bitrate.ToString(CultureInfo.InvariantCulture))
            .Append(",AVERAGE-BANDWIDTH=")
            .Append(bitrate.ToString(CultureInfo.InvariantCulture));

        AppendPlaylistVideoRangeField(playlistBuilder, state);

        AppendPlaylistCodecsField(playlistBuilder, state);

        AppendPlaylistSupplementalCodecsField(playlistBuilder, state);

        AppendPlaylistResolutionField(playlistBuilder, state);

        AppendPlaylistFramerateField(playlistBuilder, state);

        if (!string.IsNullOrWhiteSpace(subtitleGroup))
        {
            playlistBuilder.Append(",SUBTITLES=\"")
                .Append(subtitleGroup)
                .Append('"');
        }

        playlistBuilder.Append(Environment.NewLine);
        playlistBuilder.AppendLine(url);
        builder.Append(playlistBuilder);

        return playlistBuilder;
    }

    /// <summary>
    /// Appends HLS variants for pre-encoded alternate versions of the media.
    /// This enables true adaptive bitrate streaming by allowing clients to switch
    /// between different pre-encoded source files mid-stream.
    /// </summary>
    /// <param name="builder">StringBuilder to append variants to.</param>
    /// <param name="state">Current stream state.</param>
    /// <param name="streamingRequest">Original streaming request.</param>
    /// <param name="baseUrl">Base URL for variant playlists.</param>
    /// <param name="baseQuery">Base query parameters.</param>
    /// <param name="subtitleGroup">Subtitle group name if applicable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task AppendAlternateSourceVariants(
        StringBuilder builder,
        StreamState state,
        StreamingRequestDto streamingRequest,
        string baseUrl,
        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> baseQuery,
        string? subtitleGroup,
        CancellationToken cancellationToken)
    {
        // Get the item to retrieve all available media sources
        var item = _libraryManager.GetItemById<MediaBrowser.Controller.Entities.BaseItem>(streamingRequest.Id);
        if (item is null)
        {
            return;
        }

        // Get all playback media sources for this item
        var mediaSources = await _mediaSourceManager.GetPlaybackMediaSources(
            item,
            state.User,
            allowMediaProbe: false,
            enablePathSubstitution: true,
            cancellationToken).ConfigureAwait(false);

        // Skip if there's only one source (no alternates)
        if (mediaSources.Count <= 1)
        {
            return;
        }

        var currentSourceId = state.MediaSource?.Id;

        // Sort sources by bitrate descending for proper ABR ladder
        var sortedSources = mediaSources
            .Where(s => s.Id != currentSourceId) // Exclude current source (already added)
            .Where(s => s.VideoStream is not null) // Only video sources
            .OrderByDescending(s => s.Bitrate ?? 0)
            .ToList();

        foreach (var source in sortedSources)
        {
            var videoStream = source.VideoStream;
            if (videoStream is null)
            {
                continue;
            }

            // Calculate total bitrate for this source
            var videoBitrate = source.Bitrate ?? videoStream.BitRate ?? 0;
            var audioBitrate = source.GetDefaultAudioStream(null)?.BitRate ?? 0;
            var sourceTotalBitrate = (int)(videoBitrate > 0 ? videoBitrate : (audioBitrate * 5)); // Estimate if not available

            if (sourceTotalBitrate <= 0)
            {
                continue;
            }

            // Build variant query with this source's MediaSourceId
            var variantQuery = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(baseQuery)
            {
                ["MediaSourceId"] = source.Id
            };

            var variantUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(baseUrl, variantQuery);

            // Build the variant entry manually to use source's actual properties
            AppendAlternateSourcePlaylistEntry(builder, source, videoStream, variantUrl, sourceTotalBitrate, subtitleGroup);
        }
    }

    /// <summary>
    /// Appends a single HLS variant entry for an alternate source.
    /// </summary>
    private void AppendAlternateSourcePlaylistEntry(
        StringBuilder builder,
        MediaBrowser.Model.Dto.MediaSourceInfo source,
        MediaBrowser.Model.Entities.MediaStream videoStream,
        string url,
        int bitrate,
        string? subtitleGroup)
    {
        var playlistBuilder = new StringBuilder();
        playlistBuilder.Append("#EXT-X-STREAM-INF:BANDWIDTH=")
            .Append(bitrate.ToString(CultureInfo.InvariantCulture))
            .Append(",AVERAGE-BANDWIDTH=")
            .Append(bitrate.ToString(CultureInfo.InvariantCulture));

        // Video range
        if (videoStream.VideoRange != VideoRange.Unknown)
        {
            var videoRange = videoStream.VideoRange;
            var videoRangeType = videoStream.VideoRangeType;

            if (videoRange == VideoRange.SDR)
            {
                playlistBuilder.Append(",VIDEO-RANGE=SDR");
            }
            else if (videoRange == VideoRange.HDR)
            {
                switch (videoRangeType)
                {
                    case VideoRangeType.HLG:
                    case VideoRangeType.DOVIWithHLG:
                        playlistBuilder.Append(",VIDEO-RANGE=HLG");
                        break;
                    default:
                        playlistBuilder.Append(",VIDEO-RANGE=PQ");
                        break;
                }
            }
        }

        // Codecs - build from actual source properties
        var codecString = GetCodecStringForSource(source, videoStream);
        if (!string.IsNullOrEmpty(codecString))
        {
            playlistBuilder.Append(",CODECS=\"")
                .Append(codecString)
                .Append('"');
        }

        // Resolution
        if (videoStream.Width.HasValue && videoStream.Height.HasValue)
        {
            playlistBuilder.Append(",RESOLUTION=")
                .Append(videoStream.Width.Value.ToString(CultureInfo.InvariantCulture))
                .Append('x')
                .Append(videoStream.Height.Value.ToString(CultureInfo.InvariantCulture));
        }

        // Frame rate
        if (videoStream.RealFrameRate.HasValue)
        {
            playlistBuilder.Append(",FRAME-RATE=")
                .Append(videoStream.RealFrameRate.Value.ToString("0.000", CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(subtitleGroup))
        {
            playlistBuilder.Append(",SUBTITLES=\"")
                .Append(subtitleGroup)
                .Append('"');
        }

        playlistBuilder.Append(Environment.NewLine);
        playlistBuilder.AppendLine(url);
        builder.Append(playlistBuilder);
    }

    /// <summary>
    /// Builds an HLS codec string from source properties.
    /// </summary>
    private static string GetCodecStringForSource(
        MediaBrowser.Model.Dto.MediaSourceInfo source,
        MediaBrowser.Model.Entities.MediaStream videoStream)
    {
        var codecs = new List<string>();

        // Video codec
        var videoCodec = videoStream.Codec?.ToLowerInvariant();
        if (!string.IsNullOrEmpty(videoCodec))
        {
            var profile = videoStream.Profile?.ToLowerInvariant() ?? "main";
            var level = videoStream.Level ?? 40;

            switch (videoCodec)
            {
                case "h264":
                case "avc":
                    // avc1.PPCCLL format
                    var avcProfile = profile switch
                    {
                        "high" => "6400",
                        "main" => "4D40",
                        "baseline" => "4240",
                        _ => "4D40"
                    };
                    codecs.Add($"avc1.{avcProfile}{level:X2}");
                    break;

                case "hevc":
                case "h265":
                    // hev1 or hvc1
                    var hevcProfile = profile.Contains("main 10") ? "2" : "1";
                    codecs.Add($"hev1.{hevcProfile}.4.L{level}.B0");
                    break;

                case "av1":
                    // av01.P.LLM.DD
                    var av1Profile = profile.Contains("high") ? "1" : "0";
                    codecs.Add($"av01.{av1Profile}.{level:D2}M.08");
                    break;

                case "vp9":
                    codecs.Add($"vp09.00.{level:D2}.08");
                    break;
            }
        }

        // Audio codec
        var audioStream = source.GetDefaultAudioStream(null);
        if (audioStream is not null)
        {
            var audioCodec = audioStream.Codec?.ToLowerInvariant();
            switch (audioCodec)
            {
                case "aac":
                    codecs.Add("mp4a.40.2");
                    break;
                case "ac3":
                    codecs.Add("ac-3");
                    break;
                case "eac3":
                    codecs.Add("ec-3");
                    break;
                case "opus":
                    codecs.Add("opus");
                    break;
                case "flac":
                    codecs.Add("fLaC");
                    break;
            }
        }

        return string.Join(",", codecs);
    }

    /// <summary>
    /// Appends a VIDEO-RANGE field containing the range of the output video stream.
    /// </summary>
    /// <seealso cref="AppendPlaylist(StringBuilder, StreamState, string, int, string)"/>
    /// <param name="builder">StringBuilder to append the field to.</param>
    /// <param name="state">StreamState of the current stream.</param>
    private void AppendPlaylistVideoRangeField(StringBuilder builder, StreamState state)
    {
        if (state.VideoStream is not null && state.VideoStream.VideoRange != VideoRange.Unknown)
        {
            var videoRange = state.VideoStream.VideoRange;
            var videoRangeType = state.VideoStream.VideoRangeType;
            if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
            {
                if (videoRange == VideoRange.SDR)
                {
                    builder.Append(",VIDEO-RANGE=SDR");
                }

                if (videoRange == VideoRange.HDR)
                {
                    switch (videoRangeType)
                    {
                        case VideoRangeType.HLG:
                        case VideoRangeType.DOVIWithHLG:
                            builder.Append(",VIDEO-RANGE=HLG");
                            break;
                        default:
                            builder.Append(",VIDEO-RANGE=PQ");
                            break;
                    }
                }
            }
            else
            {
                // Currently we only encode to SDR.
                builder.Append(",VIDEO-RANGE=SDR");
            }
        }
    }

    /// <summary>
    /// Appends a CODECS field containing formatted strings of
    /// the active streams output video and audio codecs.
    /// </summary>
    /// <seealso cref="AppendPlaylist(StringBuilder, StreamState, string, int, string)"/>
    /// <seealso cref="GetPlaylistVideoCodecs(StreamState, string, int)"/>
    /// <seealso cref="GetPlaylistAudioCodecs(StreamState)"/>
    /// <param name="builder">StringBuilder to append the field to.</param>
    /// <param name="state">StreamState of the current stream.</param>
    private void AppendPlaylistCodecsField(StringBuilder builder, StreamState state)
    {
        // Video
        string videoCodecs = string.Empty;
        int? videoCodecLevel = GetOutputVideoCodecLevel(state);
        if (!string.IsNullOrEmpty(state.ActualOutputVideoCodec) && videoCodecLevel.HasValue)
        {
            videoCodecs = GetPlaylistVideoCodecs(state, state.ActualOutputVideoCodec, videoCodecLevel.Value);
        }

        // Audio
        string audioCodecs = string.Empty;
        if (!string.IsNullOrEmpty(state.ActualOutputAudioCodec))
        {
            audioCodecs = GetPlaylistAudioCodecs(state);
        }

        StringBuilder codecs = new StringBuilder();

        codecs.Append(videoCodecs);

        if (!string.IsNullOrEmpty(videoCodecs) && !string.IsNullOrEmpty(audioCodecs))
        {
            codecs.Append(',');
        }

        codecs.Append(audioCodecs);

        if (codecs.Length > 1)
        {
            builder.Append(",CODECS=\"")
                .Append(codecs)
                .Append('"');
        }
    }

    /// <summary>
    /// Appends a SUPPLEMENTAL-CODECS field containing formatted strings of
    /// the active streams output Dolby Vision Videos.
    /// </summary>
    /// <seealso cref="AppendPlaylist(StringBuilder, StreamState, string, int, string)"/>
    /// <seealso cref="GetPlaylistVideoCodecs(StreamState, string, int)"/>
    /// <param name="builder">StringBuilder to append the field to.</param>
    /// <param name="state">StreamState of the current stream.</param>
    private void AppendPlaylistSupplementalCodecsField(StringBuilder builder, StreamState state)
    {
        // HDR dynamic metadata currently cannot exist when transcoding
        if (!EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
        {
            return;
        }

        if (EncodingHelper.IsDovi(state.VideoStream) && !_encodingHelper.IsDoviRemoved(state))
        {
            AppendDvString();
        }
        else if (EncodingHelper.IsHdr10Plus(state.VideoStream) && !_encodingHelper.IsHdr10PlusRemoved(state))
        {
            AppendHdr10PlusString();
        }

        return;

        void AppendDvString()
        {
            var dvProfile = state.VideoStream.DvProfile;
            var dvLevel = state.VideoStream.DvLevel;
            var dvRangeString = state.VideoStream.VideoRangeType switch
            {
                VideoRangeType.DOVIWithHDR10 => "db1p",
                VideoRangeType.DOVIWithHLG => "db4h",
                VideoRangeType.DOVIWithHDR10Plus => "db1p", // The HDR10+ metadata would be removed if Dovi metadata is not removed
                _ => string.Empty // Don't label Dovi with EL and SDR due to compatability issues, ignore invalid configurations
            };

            if (dvProfile is null || dvLevel is null || string.IsNullOrEmpty(dvRangeString))
            {
                return;
            }

            var dvFourCc = string.Equals(state.ActualOutputVideoCodec, "av1", StringComparison.OrdinalIgnoreCase) ? "dav1" : "dvh1";
            builder.Append(",SUPPLEMENTAL-CODECS=\"")
                .Append(dvFourCc)
                .Append('.')
                .Append(dvProfile.Value.ToString("D2", CultureInfo.InvariantCulture))
                .Append('.')
                .Append(dvLevel.Value.ToString("D2", CultureInfo.InvariantCulture))
                .Append('/')
                .Append(dvRangeString)
                .Append('"');
        }

        void AppendHdr10PlusString()
        {
            var videoCodecLevel = GetOutputVideoCodecLevel(state);
            if (string.IsNullOrEmpty(state.ActualOutputVideoCodec) || videoCodecLevel is null)
            {
                return;
            }

            var videoCodecString = GetPlaylistVideoCodecs(state, state.ActualOutputVideoCodec, videoCodecLevel.Value);
            builder.Append(",SUPPLEMENTAL-CODECS=\"")
                .Append(videoCodecString)
                .Append('/')
                .Append("cdm4")
                .Append('"');
        }
    }

    /// <summary>
    /// Appends a RESOLUTION field containing the resolution of the output stream.
    /// </summary>
    /// <seealso cref="AppendPlaylist(StringBuilder, StreamState, string, int, string)"/>
    /// <param name="builder">StringBuilder to append the field to.</param>
    /// <param name="state">StreamState of the current stream.</param>
    private void AppendPlaylistResolutionField(StringBuilder builder, StreamState state)
    {
        if (state.OutputWidth.HasValue && state.OutputHeight.HasValue)
        {
            builder.Append(",RESOLUTION=")
                .Append(state.OutputWidth.GetValueOrDefault())
                .Append('x')
                .Append(state.OutputHeight.GetValueOrDefault());
        }
    }

    /// <summary>
    /// Appends a FRAME-RATE field containing the framerate of the output stream.
    /// </summary>
    /// <seealso cref="AppendPlaylist(StringBuilder, StreamState, string, int, string)"/>
    /// <param name="builder">StringBuilder to append the field to.</param>
    /// <param name="state">StreamState of the current stream.</param>
    private void AppendPlaylistFramerateField(StringBuilder builder, StreamState state)
    {
        double? framerate = null;
        if (state.TargetFramerate.HasValue)
        {
            framerate = Math.Round(state.TargetFramerate.GetValueOrDefault(), 3);
        }
        else if (state.VideoStream?.RealFrameRate is not null)
        {
            framerate = Math.Round(state.VideoStream.RealFrameRate.GetValueOrDefault(), 3);
        }

        if (framerate.HasValue)
        {
            builder.Append(",FRAME-RATE=")
                .Append(framerate.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    private bool EnableAdaptiveBitrateStreaming(StreamState state, bool isLiveStream, bool enableAdaptiveBitrateStreaming, IPAddress ipAddress)
    {
        // Within the local network this will likely do more harm than good.
        if (_networkManager.IsInLocalNetwork(ipAddress))
        {
            return false;
        }

        if (!enableAdaptiveBitrateStreaming)
        {
            return false;
        }

        if (isLiveStream || string.IsNullOrWhiteSpace(state.MediaPath))
        {
            // Opening live streams is so slow it's not even worth it
            return false;
        }

        if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
        {
            return false;
        }

        if (EncodingHelper.IsCopyCodec(state.OutputAudioCodec))
        {
            return false;
        }

        if (!state.IsOutputVideo)
        {
            return false;
        }

        return state.VideoRequest?.VideoBitRate.HasValue ?? false;
    }

    private void AddSubtitles(StreamState state, IEnumerable<MediaStream> subtitles, StringBuilder builder, ClaimsPrincipal user)
    {
        if (state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Drop)
        {
            return;
        }

        var selectedIndex = state.SubtitleStream is null || state.SubtitleDeliveryMethod != SubtitleDeliveryMethod.Hls ? (int?)null : state.SubtitleStream.Index;
        const string Format = "#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"{0}\",DEFAULT={1},FORCED={2},AUTOSELECT=YES,URI=\"{3}\",LANGUAGE=\"{4}\"";

        foreach (var stream in subtitles)
        {
            var name = stream.DisplayTitle;

            var isDefault = selectedIndex.HasValue && selectedIndex.Value == stream.Index;
            var isForced = stream.IsForced;

            var url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/Subtitles/{1}/subtitles.m3u8?SegmentLength={2}&ApiKey={3}",
                state.Request.MediaSourceId,
                stream.Index.ToString(CultureInfo.InvariantCulture),
                30.ToString(CultureInfo.InvariantCulture),
                user.GetToken());

            var line = string.Format(
                CultureInfo.InvariantCulture,
                Format,
                name,
                isDefault ? "YES" : "NO",
                isForced ? "YES" : "NO",
                url,
                stream.Language ?? "Unknown");

            builder.AppendLine(line);
        }
    }

    /// <summary>
    /// Appends EXT-X-IMAGE-STREAM-INF playlists for each available trickplay resolution.
    /// </summary>
    /// <param name="state">StreamState of the current stream.</param>
    /// <param name="trickplayResolutions">Dictionary of widths to corresponding tiles info.</param>
    /// <param name="builder">StringBuilder to append the field to.</param>
    /// <param name="user">Http user context.</param>
    private void AddTrickplay(StreamState state, Dictionary<int, TrickplayInfo> trickplayResolutions, StringBuilder builder, ClaimsPrincipal user)
    {
        const string playlistFormat = "#EXT-X-IMAGE-STREAM-INF:BANDWIDTH={0},RESOLUTION={1}x{2},CODECS=\"jpeg\",URI=\"{3}\"";

        foreach (var resolution in trickplayResolutions)
        {
            var width = resolution.Key;
            var trickplayInfo = resolution.Value;

            var url = string.Format(
                CultureInfo.InvariantCulture,
                "Trickplay/{0}/tiles.m3u8?MediaSourceId={1}&ApiKey={2}",
                width.ToString(CultureInfo.InvariantCulture),
                state.Request.MediaSourceId,
                user.GetToken());

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                playlistFormat,
                trickplayInfo.Bandwidth.ToString(CultureInfo.InvariantCulture),
                trickplayInfo.Width.ToString(CultureInfo.InvariantCulture),
                trickplayInfo.Height.ToString(CultureInfo.InvariantCulture),
                url);

            builder.AppendLine();
        }
    }

    /// <summary>
    /// Get the H.26X level of the output video stream.
    /// </summary>
    /// <param name="state">StreamState of the current stream.</param>
    /// <returns>H.26X level of the output video stream.</returns>
    private int? GetOutputVideoCodecLevel(StreamState state)
    {
        string levelString = string.Empty;
        if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
            && state.VideoStream is not null
            && state.VideoStream.Level.HasValue)
        {
            levelString = state.VideoStream.Level.Value.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            if (string.Equals(state.ActualOutputVideoCodec, "h264", StringComparison.OrdinalIgnoreCase))
            {
                levelString = state.GetRequestedLevel(state.ActualOutputVideoCodec) ?? "41";
                levelString = EncodingHelper.NormalizeTranscodingLevel(state, levelString);
            }

            if (string.Equals(state.ActualOutputVideoCodec, "h265", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase))
            {
                levelString = state.GetRequestedLevel("h265") ?? state.GetRequestedLevel("hevc") ?? "120";
                levelString = EncodingHelper.NormalizeTranscodingLevel(state, levelString);
            }

            if (string.Equals(state.ActualOutputVideoCodec, "av1", StringComparison.OrdinalIgnoreCase))
            {
                levelString = state.GetRequestedLevel("av1") ?? "19";
                levelString = EncodingHelper.NormalizeTranscodingLevel(state, levelString);
            }
        }

        if (int.TryParse(levelString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLevel))
        {
            return parsedLevel;
        }

        return null;
    }

    /// <summary>
    /// Get the profile of the output video stream.
    /// </summary>
    /// <param name="state">StreamState of the current stream.</param>
    /// <param name="codec">Video codec.</param>
    /// <returns>Profile of the output video stream.</returns>
    private string GetOutputVideoCodecProfile(StreamState state, string codec)
    {
        string profileString = string.Empty;
        if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
            && !string.IsNullOrEmpty(state.VideoStream.Profile))
        {
            profileString = state.VideoStream.Profile;
        }
        else if (!string.IsNullOrEmpty(codec))
        {
            profileString = state.GetRequestedProfiles(codec).FirstOrDefault() ?? string.Empty;
            if (string.Equals(state.ActualOutputVideoCodec, "h264", StringComparison.OrdinalIgnoreCase))
            {
                profileString ??= "high";
            }

            if (string.Equals(state.ActualOutputVideoCodec, "h265", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state.ActualOutputVideoCodec, "av1", StringComparison.OrdinalIgnoreCase))
            {
                profileString ??= "main";
            }
        }

        return profileString;
    }

    /// <summary>
    /// Gets a formatted string of the output audio codec, for use in the CODECS field.
    /// </summary>
    /// <seealso cref="AppendPlaylistCodecsField(StringBuilder, StreamState)"/>
    /// <seealso cref="GetPlaylistVideoCodecs(StreamState, string, int)"/>
    /// <param name="state">StreamState of the current stream.</param>
    /// <returns>Formatted audio codec string.</returns>
    private string GetPlaylistAudioCodecs(StreamState state)
    {
        if (string.Equals(state.ActualOutputAudioCodec, "aac", StringComparison.OrdinalIgnoreCase))
        {
            string? profile = EncodingHelper.IsCopyCodec(state.OutputAudioCodec)
                ? state.AudioStream?.Profile : state.GetRequestedProfiles("aac").FirstOrDefault();

            return HlsCodecStringHelpers.GetAACString(profile);
        }

        if (string.Equals(state.ActualOutputAudioCodec, "mp3", StringComparison.OrdinalIgnoreCase))
        {
            return HlsCodecStringHelpers.GetMP3String();
        }

        if (string.Equals(state.ActualOutputAudioCodec, "ac3", StringComparison.OrdinalIgnoreCase))
        {
            return HlsCodecStringHelpers.GetAC3String();
        }

        if (string.Equals(state.ActualOutputAudioCodec, "eac3", StringComparison.OrdinalIgnoreCase))
        {
            return HlsCodecStringHelpers.GetEAC3String();
        }

        if (string.Equals(state.ActualOutputAudioCodec, "flac", StringComparison.OrdinalIgnoreCase))
        {
            return HlsCodecStringHelpers.GetFLACString();
        }

        if (string.Equals(state.ActualOutputAudioCodec, "alac", StringComparison.OrdinalIgnoreCase))
        {
            return HlsCodecStringHelpers.GetALACString();
        }

        if (string.Equals(state.ActualOutputAudioCodec, "opus", StringComparison.OrdinalIgnoreCase))
        {
            return HlsCodecStringHelpers.GetOPUSString();
        }

        if (string.Equals(state.ActualOutputAudioCodec, "truehd", StringComparison.OrdinalIgnoreCase))
        {
            return HlsCodecStringHelpers.GetTRUEHDString();
        }

        if (string.Equals(state.ActualOutputAudioCodec, "dts", StringComparison.OrdinalIgnoreCase))
        {
            // lavc only support encoding DTS core profile
            string? profile = EncodingHelper.IsCopyCodec(state.OutputAudioCodec) ? state.AudioStream?.Profile : "DTS";

            return HlsCodecStringHelpers.GetDTSString(profile);
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets a formatted string of the output video codec, for use in the CODECS field.
    /// </summary>
    /// <seealso cref="AppendPlaylistCodecsField(StringBuilder, StreamState)"/>
    /// <seealso cref="GetPlaylistAudioCodecs(StreamState)"/>
    /// <param name="state">StreamState of the current stream.</param>
    /// <param name="codec">Video codec.</param>
    /// <param name="level">Video level.</param>
    /// <returns>Formatted video codec string.</returns>
    private string GetPlaylistVideoCodecs(StreamState state, string codec, int level)
    {
        if (level == 0)
        {
            // This is 0 when there's no requested level in the device profile
            // and the source is not encoded in H.26X or AV1
            _logger.LogError("Got invalid level when building CODECS field for HLS master playlist");
            return string.Empty;
        }

        if (string.Equals(codec, "h264", StringComparison.OrdinalIgnoreCase))
        {
            string profile = GetOutputVideoCodecProfile(state, "h264");
            return HlsCodecStringHelpers.GetH264String(profile, level);
        }

        if (string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)
            || string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase))
        {
            string profile = GetOutputVideoCodecProfile(state, "hevc");
            return HlsCodecStringHelpers.GetH265String(profile, level);
        }

        if (string.Equals(codec, "av1", StringComparison.OrdinalIgnoreCase))
        {
            string profile = GetOutputVideoCodecProfile(state, "av1");

            // Currently we only transcode to 8 bits AV1
            int bitDepth = 8;
            if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
                && state.VideoStream is not null
                && state.VideoStream.BitDepth.HasValue)
            {
                bitDepth = state.VideoStream.BitDepth.Value;
            }

            return HlsCodecStringHelpers.GetAv1String(profile, level, false, bitDepth);
        }

        // VP9 HLS is for video remuxing only, everything is probed from the original video
        if (string.Equals(codec, "vp9", StringComparison.OrdinalIgnoreCase))
        {
            var width = state.VideoStream.Width ?? 0;
            var height = state.VideoStream.Height ?? 0;
            var framerate = state.VideoStream.ReferenceFrameRate ?? 30;
            var bitDepth = state.VideoStream.BitDepth ?? 8;
            return HlsCodecStringHelpers.GetVp9String(
                width,
                height,
                state.VideoStream.PixelFormat,
                framerate,
                bitDepth);
        }

        return string.Empty;
    }

    private int GetBitrateVariation(int bitrate)
    {
        // By default, vary by just 50k
        var variation = 50000;

        if (bitrate >= 10000000)
        {
            variation = 2000000;
        }
        else if (bitrate >= 5000000)
        {
            variation = 1500000;
        }
        else if (bitrate >= 3000000)
        {
            variation = 1000000;
        }
        else if (bitrate >= 2000000)
        {
            variation = 500000;
        }
        else if (bitrate >= 1000000)
        {
            variation = 300000;
        }
        else if (bitrate >= 600000)
        {
            variation = 200000;
        }
        else if (bitrate >= 400000)
        {
            variation = 100000;
        }

        return variation;
    }

    private string ReplacePlaylistCodecsField(StringBuilder playlist, StringBuilder oldValue, StringBuilder newValue)
    {
        var oldPlaylist = playlist.ToString();
        return oldPlaylist.Replace(
            oldValue.ToString(),
            newValue.ToString(),
            StringComparison.Ordinal);
    }
}
