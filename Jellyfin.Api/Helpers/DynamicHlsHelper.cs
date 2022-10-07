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
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Dynamic hls helper.
    /// </summary>
    public class DynamicHlsHelper
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IDeviceManager _deviceManager;
        private readonly TranscodingJobHelper _transcodingJobHelper;
        private readonly INetworkManager _networkManager;
        private readonly ILogger<DynamicHlsHelper> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EncodingHelper _encodingHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicHlsHelper"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="transcodingJobHelper">Instance of <see cref="TranscodingJobHelper"/>.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{DynamicHlsHelper}"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
        public DynamicHlsHelper(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDlnaManager dlnaManager,
            IMediaSourceManager mediaSourceManager,
            IServerConfigurationManager serverConfigurationManager,
            IMediaEncoder mediaEncoder,
            IDeviceManager deviceManager,
            TranscodingJobHelper transcodingJobHelper,
            INetworkManager networkManager,
            ILogger<DynamicHlsHelper> logger,
            IHttpContextAccessor httpContextAccessor,
            EncodingHelper encodingHelper)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _mediaSourceManager = mediaSourceManager;
            _serverConfigurationManager = serverConfigurationManager;
            _mediaEncoder = mediaEncoder;
            _deviceManager = deviceManager;
            _transcodingJobHelper = transcodingJobHelper;
            _networkManager = networkManager;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _encodingHelper = encodingHelper;
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
            if (_httpContextAccessor.HttpContext == null)
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
                    _dlnaManager,
                    _deviceManager,
                    _transcodingJobHelper,
                    transcodingJobType,
                    cancellationTokenSource.Token)
                .ConfigureAwait(false);

            _httpContextAccessor.HttpContext.Response.Headers.Add(HeaderNames.Expires, "0");
            if (isHeadRequest)
            {
                return new FileContentResult(Array.Empty<byte>(), MimeTypes.GetMimeType("playlist.m3u8"));
            }

            var totalBitrate = (state.OutputAudioBitrate ?? 0) + (state.OutputVideoBitrate ?? 0);

            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");

            var isLiveStream = state.IsSegmentedLiveStream;

            var queryString = _httpContextAccessor.HttpContext.Request.QueryString.ToString();

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

            // Main stream
            var playlistUrl = isLiveStream ? "live.m3u8" : "main.m3u8";

            playlistUrl += queryString;

            var subtitleStreams = state.MediaSource
                .MediaStreams
                .Where(i => i.IsTextSubtitleStream)
                .ToList();

            var subtitleGroup = subtitleStreams.Count > 0 && (state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Hls || state.VideoRequest!.EnableSubtitlesInManifest)
                ? "subs"
                : null;

            // If we're burning in subtitles then don't add additional subs to the manifest
            if (state.SubtitleStream != null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
            {
                subtitleGroup = null;
            }

            if (!string.IsNullOrWhiteSpace(subtitleGroup))
            {
                AddSubtitles(state, subtitleStreams, builder, _httpContextAccessor.HttpContext.User);
            }

            var basicPlaylist = AppendPlaylist(builder, state, playlistUrl, totalBitrate, subtitleGroup);

            if (state.VideoStream != null && state.VideoRequest != null)
            {
                // Provide SDR HEVC entrance for backward compatibility.
                if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
                    && !string.IsNullOrEmpty(state.VideoStream.VideoRange)
                    && string.Equals(state.VideoStream.VideoRange, "HDR", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase))
                {
                    var requestedVideoProfiles = state.GetRequestedProfiles("hevc");
                    if (requestedVideoProfiles != null && requestedVideoProfiles.Length > 0)
                    {
                        // Force HEVC Main Profile and disable video stream copy.
                        state.OutputVideoCodec = "hevc";
                        var sdrVideoUrl = ReplaceProfile(playlistUrl, "hevc", string.Join(',', requestedVideoProfiles), "main");
                        sdrVideoUrl += "&AllowVideoStreamCopy=false";

                        var sdrOutputVideoBitrate = _encodingHelper.GetVideoBitrateParamValue(state.VideoRequest, state.VideoStream, state.OutputVideoCodec);
                        var sdrOutputAudioBitrate = _encodingHelper.GetAudioBitrateParam(state.VideoRequest, state.AudioStream) ?? 0;
                        var sdrTotalBitrate = sdrOutputAudioBitrate + sdrOutputVideoBitrate;

                        AppendPlaylist(builder, state, sdrVideoUrl, sdrTotalBitrate, subtitleGroup);

                        // Restore the video codec
                        state.OutputVideoCodec = "copy";
                    }
                }

                // Provide Level 5.0 entrance for backward compatibility.
                // e.g. Apple A10 chips refuse the master playlist containing SDR HEVC Main Level 5.1 video,
                // but in fact it is capable of playing videos up to Level 6.1.
                if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
                    && state.VideoStream.Level.HasValue
                    && state.VideoStream.Level > 150
                    && !string.IsNullOrEmpty(state.VideoStream.VideoRange)
                    && string.Equals(state.VideoStream.VideoRange, "SDR", StringComparison.OrdinalIgnoreCase)
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

            if (EnableAdaptiveBitrateStreaming(state, isLiveStream, enableAdaptiveBitrateStreaming, _httpContextAccessor.HttpContext.GetNormalizedRemoteIp()))
            {
                var requestedVideoBitrate = state.VideoRequest == null ? 0 : state.VideoRequest.VideoBitRate ?? 0;

                // By default, vary by just 200k
                var variation = GetBitrateVariation(totalBitrate);

                var newBitrate = totalBitrate - variation;
                var variantUrl = ReplaceVideoBitrate(playlistUrl, requestedVideoBitrate, requestedVideoBitrate - variation);
                AppendPlaylist(builder, state, variantUrl, newBitrate, subtitleGroup);

                variation *= 2;
                newBitrate = totalBitrate - variation;
                variantUrl = ReplaceVideoBitrate(playlistUrl, requestedVideoBitrate, requestedVideoBitrate - variation);
                AppendPlaylist(builder, state, variantUrl, newBitrate, subtitleGroup);
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
        /// Appends a VIDEO-RANGE field containing the range of the output video stream.
        /// </summary>
        /// <seealso cref="AppendPlaylist(StringBuilder, StreamState, string, int, string)"/>
        /// <param name="builder">StringBuilder to append the field to.</param>
        /// <param name="state">StreamState of the current stream.</param>
        private void AppendPlaylistVideoRangeField(StringBuilder builder, StreamState state)
        {
            if (state.VideoStream != null && !string.IsNullOrEmpty(state.VideoStream.VideoRange))
            {
                var videoRange = state.VideoStream.VideoRange;
                if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
                {
                    if (string.Equals(videoRange, "SDR", StringComparison.OrdinalIgnoreCase))
                    {
                        builder.Append(",VIDEO-RANGE=SDR");
                    }

                    if (string.Equals(videoRange, "HDR", StringComparison.OrdinalIgnoreCase))
                    {
                        builder.Append(",VIDEO-RANGE=PQ");
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
            else if (state.VideoStream?.RealFrameRate != null)
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

            // Having problems in android
            return false;
            // return state.VideoRequest.VideoBitRate.HasValue;
        }

        private void AddSubtitles(StreamState state, IEnumerable<MediaStream> subtitles, StringBuilder builder, ClaimsPrincipal user)
        {
            if (state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Drop)
            {
                return;
            }

            var selectedIndex = state.SubtitleStream == null || state.SubtitleDeliveryMethod != SubtitleDeliveryMethod.Hls ? (int?)null : state.SubtitleStream.Index;
            const string Format = "#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"{0}\",DEFAULT={1},FORCED={2},AUTOSELECT=YES,URI=\"{3}\",LANGUAGE=\"{4}\"";

            foreach (var stream in subtitles)
            {
                var name = stream.DisplayTitle;

                var isDefault = selectedIndex.HasValue && selectedIndex.Value == stream.Index;
                var isForced = stream.IsForced;

                var url = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/Subtitles/{1}/subtitles.m3u8?SegmentLength={2}&api_key={3}",
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
        /// Get the H.26X level of the output video stream.
        /// </summary>
        /// <param name="state">StreamState of the current stream.</param>
        /// <returns>H.26X level of the output video stream.</returns>
        private int? GetOutputVideoCodecLevel(StreamState state)
        {
            string levelString = string.Empty;
            if (EncodingHelper.IsCopyCodec(state.OutputVideoCodec)
                && state.VideoStream != null
                && state.VideoStream.Level.HasValue)
            {
                levelString = state.VideoStream.Level.ToString() ?? string.Empty;
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
            }

            if (int.TryParse(levelString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLevel))
            {
                return parsedLevel;
            }

            return null;
        }

        /// <summary>
        /// Get the H.26X profile of the output video stream.
        /// </summary>
        /// <param name="state">StreamState of the current stream.</param>
        /// <param name="codec">Video codec.</param>
        /// <returns>H.26X profile of the output video stream.</returns>
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
                    || string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase))
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
                string? profile = state.GetRequestedProfiles("aac").FirstOrDefault();
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
                // This is 0 when there's no requested H.26X level in the device profile
                // and the source is not encoded in H.26X
                _logger.LogError("Got invalid H.26X level when building CODECS field for HLS master playlist");
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

        private string ReplaceVideoBitrate(string url, int oldValue, int newValue)
        {
            return url.Replace(
                "videobitrate=" + oldValue.ToString(CultureInfo.InvariantCulture),
                "videobitrate=" + newValue.ToString(CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }

        private string ReplaceProfile(string url, string codec, string oldValue, string newValue)
        {
            string profileStr = codec + "-profile=";
            return url.Replace(
                profileStr + oldValue,
                profileStr + newValue,
                StringComparison.OrdinalIgnoreCase);
        }

        private string ReplacePlaylistCodecsField(StringBuilder playlist, StringBuilder oldValue, StringBuilder newValue)
        {
            var oldPlaylist = playlist.ToString();
            return oldPlaylist.Replace(
                oldValue.ToString(),
                newValue.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
