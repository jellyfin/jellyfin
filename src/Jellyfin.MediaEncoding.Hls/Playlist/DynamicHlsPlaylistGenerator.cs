using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Jellyfin.MediaEncoding.Hls.Extractors;
using Jellyfin.MediaEncoding.Keyframes;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;

namespace Jellyfin.MediaEncoding.Hls.Playlist;

/// <inheritdoc />
public class DynamicHlsPlaylistGenerator : IDynamicHlsPlaylistGenerator
{
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IKeyframeExtractor[] _extractors;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicHlsPlaylistGenerator"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">An instance of the see <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="extractors">An instance of <see cref="IEnumerable{IKeyframeExtractor}"/>.</param>
    public DynamicHlsPlaylistGenerator(IServerConfigurationManager serverConfigurationManager, IEnumerable<IKeyframeExtractor> extractors)
    {
        _serverConfigurationManager = serverConfigurationManager;
        _extractors = extractors.Where(e => e.IsMetadataBased).ToArray();
    }

    /// <inheritdoc />
    public string CreateMainPlaylist(CreateMainPlaylistRequest request)
    {
        IReadOnlyList<double> segments;
        // For video transcodes it is sufficient with equal length segments as ffmpeg will create new keyframes
        if (request.IsRemuxingVideo && TryExtractKeyframes(request.FilePath, out var keyframeData))
        {
            segments = ComputeSegments(keyframeData, request.DesiredSegmentLengthMs);
        }
        else
        {
            segments = ComputeEqualLengthSegments(request.DesiredSegmentLengthMs, request.TotalRuntimeTicks);
        }

        var segmentExtension = EncodingHelper.GetSegmentFileExtension(request.SegmentContainer);

        // http://ffmpeg.org/ffmpeg-all.html#toc-hls-2
        var isHlsInFmp4 = string.Equals(segmentExtension, ".mp4", StringComparison.OrdinalIgnoreCase);
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
            // Init file that only includes fMP4 headers
            builder.Append("#EXT-X-MAP:URI=\"")
                .Append(request.EndpointPrefix)
                .Append("-1")
                .Append(segmentExtension)
                .Append(request.QueryString)
                .Append("&runtimeTicks=0")
                .Append("&actualSegmentLengthTicks=0")
                .Append('"')
                .AppendLine();
        }

        long currentRuntimeInSeconds = 0;
        foreach (var length in segments)
        {
            // Manually convert to ticks to avoid precision loss when converting double
            var lengthTicks = Convert.ToInt64(length * TimeSpan.TicksPerSecond);
            builder.Append("#EXTINF:")
                .Append(length.ToString("0.000000", CultureInfo.InvariantCulture))
                .AppendLine(", nodesc")
                .Append(request.EndpointPrefix)
                .Append(index++)
                .Append(segmentExtension)
                .Append(request.QueryString)
                .Append("&runtimeTicks=")
                .Append(currentRuntimeInSeconds)
                .Append("&actualSegmentLengthTicks=")
                .Append(lengthTicks)
                .AppendLine();

            currentRuntimeInSeconds += lengthTicks;
        }

        builder.AppendLine("#EXT-X-ENDLIST");

        return builder.ToString();
    }

    private bool TryExtractKeyframes(string filePath, [NotNullWhen(true)] out KeyframeData? keyframeData)
    {
        keyframeData = null;
        if (!IsExtractionAllowedForFile(filePath, _serverConfigurationManager.GetEncodingOptions().AllowOnDemandMetadataBasedKeyframeExtractionForExtensions))
        {
            return false;
        }

        var len = _extractors.Length;
        for (var i = 0; i < len; i++)
        {
            var extractor = _extractors[i];
            if (!extractor.TryExtractKeyframes(filePath, out var result))
            {
                continue;
            }

            keyframeData = result;
            return true;
        }

        return false;
    }

    internal static bool IsExtractionAllowedForFile(ReadOnlySpan<char> filePath, IReadOnlyList<string> allowedExtensions)
    {
        var extension = Path.GetExtension(filePath);
        if (extension.IsEmpty)
        {
            return false;
        }

        // Remove the leading dot
        var extensionWithoutDot = extension[1..];
        for (var i = 0; i < allowedExtensions.Count; i++)
        {
            var allowedExtension = allowedExtensions[i].AsSpan().TrimStart('.');
            if (extensionWithoutDot.Equals(allowedExtension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static IReadOnlyList<double> ComputeSegments(KeyframeData keyframeData, int desiredSegmentLengthMs)
    {
        if (keyframeData.KeyframeTicks.Count > 0 && keyframeData.TotalDuration < keyframeData.KeyframeTicks[^1])
        {
            throw new ArgumentException("Invalid duration in keyframe data", nameof(keyframeData));
        }

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

    internal static double[] ComputeEqualLengthSegments(int desiredSegmentLengthMs, long totalRuntimeTicks)
    {
        if (desiredSegmentLengthMs == 0 || totalRuntimeTicks == 0)
        {
            throw new InvalidOperationException($"Invalid segment length ({desiredSegmentLengthMs}) or runtime ticks ({totalRuntimeTicks})");
        }

        var desiredSegmentLength = TimeSpan.FromMilliseconds(desiredSegmentLengthMs);

        var segmentLengthTicks = desiredSegmentLength.Ticks;
        var wholeSegments = totalRuntimeTicks / segmentLengthTicks;
        var remainingTicks = totalRuntimeTicks % segmentLengthTicks;

        var segmentsLen = wholeSegments + (remainingTicks == 0 ? 0 : 1);
        var segments = new double[segmentsLen];
        for (int i = 0; i < wholeSegments; i++)
        {
            segments[i] = desiredSegmentLength.TotalSeconds;
        }

        if (remainingTicks != 0)
        {
            segments[^1] = TimeSpan.FromTicks(remainingTicks).TotalSeconds;
        }

        return segments;
    }
}
