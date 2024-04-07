using System.Collections.Generic;
using MediaBrowser.Model.Dlna;

namespace Jellyfin.Api.Models.Requests;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "V Annoying")]
public class VideoStreamRequest
{
    public string? Container { get; set; }

    public bool? Static { get; set; }

    public string? Params { get; set; }

    public string? Tag { get; set; }

    public string? DeviceProfileId { get; set; }

    public string? PlaySessionId { get; set; }

    public string? SegmentContainer { get; set; }

    public int? SegmentLength { get; set; }

    public int? MinSegments { get; set; }

    public string? MediaSourceId { get; set; }

    public string? DeviceId { get; set; }

    public string? AudioCodec { get; set; }

    public bool? EnableAutoStreamCopy { get; set; }

    public bool? AllowVideoStreamCopy { get; set; }

    public bool? AllowAudioStreamCopy { get; set; }

    public bool? BreakOnNonKeyFrames { get; set; }

    public int? AudioSampleRate { get; set; }

    public int? AudioChannels { get; set; }

    public int? MaxAudioChannels { get; set; }

    public string? Profile { get; set; }

    public string? Level { get; set; }

    public float? Framerate { get; set; }

    public float? MaxFramerate { get; set; }

    public bool? CopyTimestamps { get; set; }

    public long? StartTimeTicks { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public int? MaxWidth { get; set; }

    public int? MaxHeight { get; set; }

    public int? VideoBitRate { get; set; }

    public int? SubtitleStreamIndex { get; set; }

    public SubtitleDeliveryMethod? SubtitleDeliveryMethod { get; set; }

    public int? MaxRefFrames { get; set; }

    public int? MaxVideoBitDepth { get; set; }

    public int? MaxAudioBitDepth { get; set; }

    public int? AudioBitRate { get; set; }

    public bool? RequireAvc { get; set; }

    public bool? DeInterlace { get; set; }

    public bool? RequireNoAnamorphic { get; set; }

    public int? TranscodingMaxAudioChannels { get; set; }

    public int? CpuCoreLimit { get; set; }

    public string? LiveStreamId { get; set; }

    public bool? EnableMpegM2TsMode { get; set; }

    public string? VideoCodec { get; set; }

    public string? SubtitleCodec { get; set; }

    public string? TranscodeReasons { get; set; }

    public int? AudioStreamIndex { get; set; }

    public int? VideoStreamIndex { get; set; }

    public EncodingContext? Context { get; set; }

    public Dictionary<string, string> StreamOptions { get; set; } = [];
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
