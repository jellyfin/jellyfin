using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Jellyfin.Extensions.Json;
using Jellyfin.MediaEncoding.Keyframes;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.MediaEncoding.Hls.Playlist
{
    /// <inheritdoc />
    public class DynamicHlsPlaylistGenerator : IDynamicHlsPlaylistGenerator
    {
        private const string DefaultContainerExtension = ".ts";

        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IApplicationPaths _applicationPaths;
        private readonly KeyframeExtractor _keyframeExtractor;
        private readonly ILogger<DynamicHlsPlaylistGenerator> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicHlsPlaylistGenerator"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">An instance of the see <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="mediaEncoder">An instance of the see <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="applicationPaths">An instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="loggerFactory">An instance of the see <see cref="ILoggerFactory"/> interface.</param>
        public DynamicHlsPlaylistGenerator(IServerConfigurationManager serverConfigurationManager, IMediaEncoder mediaEncoder, IApplicationPaths applicationPaths, ILoggerFactory loggerFactory)
        {
            _serverConfigurationManager = serverConfigurationManager;
            _mediaEncoder = mediaEncoder;
            _applicationPaths = applicationPaths;
            _keyframeExtractor = new KeyframeExtractor(loggerFactory.CreateLogger<KeyframeExtractor>());
            _logger = loggerFactory.CreateLogger<DynamicHlsPlaylistGenerator>();
        }

        private string KeyframeCachePath => Path.Combine(_applicationPaths.DataPath, "keyframes");

        /// <inheritdoc />
        public string CreateMainPlaylist(CreateMainPlaylistRequest request)
        {
            IReadOnlyList<double> segments;
            if (TryExtractKeyframes(request.FilePath, out var keyframeData))
            {
                segments = ComputeSegments(keyframeData, request.DesiredSegmentLengthMs);
            }
            else
            {
                segments = ComputeEqualLengthSegments(request.DesiredSegmentLengthMs, request.TotalRuntimeTicks);
            }

            var segmentExtension = GetSegmentFileExtension(request.SegmentContainer);

            // http://ffmpeg.org/ffmpeg-all.html#toc-hls-2
            var isHlsInFmp4 = string.Equals(segmentExtension, "mp4", StringComparison.OrdinalIgnoreCase);
            var hlsVersion = isHlsInFmp4 ? "7" : "3";

            var builder = new StringBuilder(128);

            builder.AppendLine("#EXTM3U")
                .AppendLine("#EXT-X-PLAYLIST-TYPE:VOD")
                .Append("#EXT-X-VERSION:")
                .Append(hlsVersion)
                .AppendLine()
                .Append("#EXT-X-TARGETDURATION:")
                .Append(Math.Ceiling(segments.Count > 0 ? segments.Max() : request.DesiredSegmentLengthMs))
                .AppendLine()
                .AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

            var index = 0;

            if (isHlsInFmp4)
            {
                builder.Append("#EXT-X-MAP:URI=\"")
                    .Append(request.EndpointPrefix)
                    .Append("-1")
                    .Append(segmentExtension)
                    .Append(request.QueryString)
                    .Append('"')
                    .AppendLine();
            }

            double currentRuntimeInSeconds = 0;
            foreach (var length in segments)
            {
                builder.Append("#EXTINF:")
                    .Append(length.ToString("0.000000", CultureInfo.InvariantCulture))
                    .AppendLine(", nodesc")
                    .Append(request.EndpointPrefix)
                    .Append(index++)
                    .Append(segmentExtension)
                    .Append(request.QueryString)
                    .Append("&runtimeTicks=")
                    .Append(TimeSpan.FromSeconds(currentRuntimeInSeconds).Ticks)
                    .Append("&actualSegmentLengthTicks=")
                    .Append(TimeSpan.FromSeconds(length).Ticks)
                    .AppendLine();

                currentRuntimeInSeconds += length;
            }

            builder.AppendLine("#EXT-X-ENDLIST");

            return builder.ToString();
        }

        private bool TryExtractKeyframes(string filePath, [NotNullWhen(true)] out KeyframeData? keyframeData)
        {
            keyframeData = null;
            if (!IsExtractionAllowedForFile(filePath, _serverConfigurationManager.GetEncodingOptions().AllowAutomaticKeyframeExtractionForExtensions))
            {
                return false;
            }

            var cachePath = GetCachePath(filePath);
            if (TryReadFromCache(cachePath, out var cachedResult))
            {
                keyframeData = cachedResult;
            }
            else
            {
                try
                {
                    keyframeData = _keyframeExtractor.GetKeyframeData(filePath, _mediaEncoder.ProbePath, string.Empty);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Keyframe extraction failed for path {FilePath}", filePath);
                    return false;
                }

                CacheResult(cachePath, keyframeData);
            }

            return keyframeData.KeyframeTicks.Count > 0;
        }

        private void CacheResult(string cachePath, KeyframeData keyframeData)
        {
            var json = JsonSerializer.Serialize(keyframeData, _jsonOptions);
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath) ?? throw new ArgumentException($"Provided path ({cachePath}) is not valid.", nameof(cachePath)));
            File.WriteAllText(cachePath, json);
        }

        private string GetCachePath(string filePath)
        {
            var lastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
            ReadOnlySpan<char> filename = (filePath + "_" + lastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)).GetMD5() + ".json";
            var prefix = filename.Slice(0, 1);

            return Path.Join(KeyframeCachePath, prefix, filename);
        }

        private bool TryReadFromCache(string cachePath, [NotNullWhen(true)] out KeyframeData? cachedResult)
        {
            if (File.Exists(cachePath))
            {
                var bytes = File.ReadAllBytes(cachePath);
                cachedResult = JsonSerializer.Deserialize<KeyframeData>(bytes, _jsonOptions);
                return cachedResult != null;
            }

            cachedResult = null;
            return false;
        }

        internal static bool IsExtractionAllowedForFile(ReadOnlySpan<char> filePath, string[] allowedExtensions)
        {
            var extension = Path.GetExtension(filePath);
            if (extension.IsEmpty)
            {
                return false;
            }

            // Remove the leading dot
            var extensionWithoutDot = extension[1..];
            for (var i = 0; i < allowedExtensions.Length; i++)
            {
                var allowedExtension = allowedExtensions[i];
                if (extensionWithoutDot.Equals(allowedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IReadOnlyList<double> ComputeSegments(KeyframeData keyframeData, int desiredSegmentLengthMs)
        {
            long lastKeyframe = 0;
            var result = new List<double>();
            // Scale the segment length to ticks to match the keyframes
            var desiredSegmentLengthTicks = TimeSpan.FromMilliseconds(desiredSegmentLengthMs).Ticks;
            var desiredCutTime = desiredSegmentLengthTicks;
            for (var j = 0; j < keyframeData.KeyframeTicks.Count; j++)
            {
                var keyframe = keyframeData.KeyframeTicks[j];
                if (keyframe >= desiredCutTime)
                {
                    var currentSegmentLength = keyframe - lastKeyframe;
                    result.Add(TimeSpan.FromTicks(currentSegmentLength).TotalSeconds);
                    lastKeyframe = keyframe;
                    desiredCutTime += desiredSegmentLengthTicks;
                }
            }

            result.Add(TimeSpan.FromTicks(keyframeData.TotalDuration - lastKeyframe).TotalSeconds);
            return result;
        }

        internal static double[] ComputeEqualLengthSegments(long desiredSegmentLengthMs, long totalRuntimeTicks)
        {
            var segmentLengthTicks = TimeSpan.FromMilliseconds(desiredSegmentLengthMs).Ticks;
            var wholeSegments = totalRuntimeTicks / segmentLengthTicks;
            var remainingTicks = totalRuntimeTicks % segmentLengthTicks;

            var segmentsLen = wholeSegments + (remainingTicks == 0 ? 0 : 1);
            var segments = new double[segmentsLen];
            for (int i = 0; i < wholeSegments; i++)
            {
                segments[i] = desiredSegmentLengthMs;
            }

            if (remainingTicks != 0)
            {
                segments[^1] = TimeSpan.FromTicks(remainingTicks).TotalSeconds;
            }

            return segments;
        }

        // TODO copied from DynamicHlsController
        private static string GetSegmentFileExtension(string segmentContainer)
        {
            if (!string.IsNullOrWhiteSpace(segmentContainer))
            {
                return "." + segmentContainer;
            }

            return DefaultContainerExtension;
        }
    }
}
