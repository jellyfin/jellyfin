using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Models.Requests;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "V Annoying")]
public class VideoStreamRequest
{
    /// <summary>
    /// Gets or sets the video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv.
    /// </summary>
    [FromQuery]
    [RegularExpression(EncodingHelper.ValidationRegex)]
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets. If true, the original file will be streamed statically without any encoding. Use either no url extension or the original file extension. true/false.
    /// </summary>
    [FromQuery]
    public bool? Static { get; set; }

    /// <summary>
    /// gets or sets the streaming parameters.
    /// </summary>
    [FromQuery]
    public string? Params { get; set; }

    /// <summary>
    /// gets or sets the tag.
    /// </summary>
    [FromQuery]
    public string? Tag { get; set; }

    /// <summary>
    /// gets or sets The dlna device profile id to utilize.
    /// </summary>
    [FromQuery]
    public string? DeviceProfileId { get; set; }

    /// <summary>
    /// gets or sets the play session id.
    /// </summary>
    [FromQuery]
    public string? PlaySessionId { get; set; }

    /// <summary>
    /// gets or sets The segment container.
    /// </summary>
    [FromQuery]
    [RegularExpression(EncodingHelper.ValidationRegex)]
    public string? SegmentContainer { get; set; }

    /// <summary>
    /// gets or sets the egment length.
    /// </summary>
    [FromQuery]
    public int? SegmentLength { get; set; }

    /// <summary>
    /// gets or sets The minimum number of segments.
    /// </summary>
    [FromQuery]
    public int? MinSegments { get; set; }

    /// <summary>
    /// gets or sets The media version id, if playing an alternate version.
    /// </summary>
    [FromQuery]
    public string? MediaSourceId { get; set; }

    /// <summary>
    /// gets or sets The device id of the client requesting. Used to stop encoding processes when needed.
    /// </summary>
    [FromQuery]
    public string? DeviceId { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a audio codec to encode to, e.g. mp3. If omitted the server will auto-select using the url's extension. Options: aac, mp3, vorbis, wma.
    /// </summary>
    [FromQuery]
    [RegularExpression(EncodingHelper.ValidationRegex)]
    public string? AudioCodec { get; set; }

    /// <summary>
    /// gets or sets Whether or not to allow automatic stream copy if requested values match the original source. Defaults to true.
    /// </summary>
    [FromQuery]
    public bool? EnableAutoStreamCopy { get; set; }

    /// <summary>
    /// gets or sets Whether or not to allow copying of the video stream url.
    /// </summary>
    [FromQuery]
    public bool? AllowVideoStreamCopy { get; set; }

    /// <summary>
    /// gets or sets Whether or not to allow copying of the audio stream url.
    /// </summary>
    [FromQuery]
    public bool? AllowAudioStreamCopy { get; set; }

    /// <summary>
    /// gets or sets Optional. Whether to break on non key frames.
    /// </summary>
    [FromQuery]
    public bool? BreakOnNonKeyFrames { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a specific audio sample rate, e.g. 44100.
    /// </summary>
    [FromQuery]
    public int? AudioSampleRate { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a specific number of audio channels to encode to, e.g. 2.
    /// </summary>
    [FromQuery]
    public int? AudioChannels { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a maximum number of audio channels to encode to, e.g. 2.
    /// </summary>
    [FromQuery]
    public int? MaxAudioChannels { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a specific an encoder profile (varies by encoder), e.g. main, baseline, high.
    /// </summary>
    [FromQuery]
    public string? Profile { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a level for the encoder profile (varies by encoder), e.g. 3, 3.1.
    /// </summary>
    [FromQuery]
    public string? Level { get; set; }

    /// <summary>
    /// gets or sets Optional. A specific video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.
    /// </summary>
    [FromQuery]
    public float? Framerate { get; set; }

    /// <summary>
    /// gets or sets Optional. A specific maximum video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.
    /// </summary>
    [FromQuery]
    public float? MaxFramerate { get; set; }

    /// <summary>
    /// gets or sets Whether or not to copy timestamps when transcoding with an offset. Defaults to false.
    /// </summary>
    [FromQuery]
    public bool? CopyTimestamps { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms.
    /// </summary>
    [FromQuery]
    public long? StartTimeTicks { get; set; }

    /// <summary>
    /// gets or sets Optional. The fixed horizontal resolution of the encoded video.
    /// </summary>
    [FromQuery]
    public int? Width { get; set; }

    /// <summary>
    /// gets or sets Optional. The fixed vertical resolution of the encoded video.
    /// </summary>
    [FromQuery]
    public int? Height { get; set; }

    /// <summary>
    /// gets or sets Optional. The maximum horizontal resolution of the encoded video.
    /// </summary>
    [FromQuery]
    public int? MaxWidth { get; set; }

    /// <summary>
    /// gets or sets Optional. The maximum vertical resolution of the encoded video.
    /// </summary>
    [FromQuery]
    public int? MaxHeight { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a video bitrate to encode to, e.g. 500000. If omitted this will be left to encoder defaults.
    /// </summary>
    [FromQuery]
    public int? VideoBitRate { get; set; }

    /// <summary>
    /// gets or sets Optional. The index of the subtitle stream to use. If omitted no subtitles will be used.
    /// </summary>
    [FromQuery]
    public int? SubtitleStreamIndex { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify the subtitle delivery method.
    /// </summary>
    [FromQuery]
    public SubtitleDeliveryMethod? SubtitleDeliveryMethod { get; set; }

    /// <summary>
    /// gets or sets max ref frames.
    /// </summary>
    [FromQuery]
    public int? MaxRefFrames { get; set; }

    /// <summary>
    /// gets or sets Optional. The maximum video bit depth.
    /// </summary>
    [FromQuery]
    public int? MaxVideoBitDepth { get; set; }

    /// <summary>
    /// gets or sets The maximum audio bit depth.
    /// </summary>
    [FromQuery]
    public int? MaxAudioBitDepth { get; set; }

    /// <summary>
    /// gets or sets the audio bitrate to encode.
    /// </summary>
    public int? AudioBitRate { get; set; }

    /// <summary>
    /// gets or sets Optional. Whether to require avc.
    /// </summary>
    [FromQuery]
    public bool? RequireAvc { get; set; }

    /// <summary>
    /// gets or sets Optional. Whether to deinterlace the video.
    /// </summary>
    [FromQuery]
    public bool? DeInterlace { get; set; }

    /// <summary>
    /// gets or sets Optional. Whether to require a non anamorphic stream.
    /// </summary>
    [FromQuery]
    public bool? RequireNoAnamorphic { get; set; }

    /// <summary>
    /// gets or sets Optional. The maximum number of audio channels to transcode.
    /// </summary>
    [FromQuery]
    public int? TranscodingMaxAudioChannels { get; set; }

    /// <summary>
    /// gets or sets Optional. The limit of how many cpu cores to use.
    /// </summary>
    [FromQuery]
    public int? CpuCoreLimit { get; set; }

    /// <summary>
    /// gets or sets The live stream id.
    /// </summary>
    [FromQuery]
    public string? LiveStreamId { get; set; }

    /// <summary>
    /// gets or sets Optional. Whether to enable the MpegtsM2Ts mode.
    /// </summary>
    [FromQuery]
    public bool? EnableMpegM2TsMode { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a video codec to encode to, e.g. h264. If omitted the server will auto-select using the url's extension. Options: h265, h264, mpeg4, theora, vp8, vp9, vpx (deprecated), wmv.
    /// </summary>
    [FromQuery]
    [RegularExpression(EncodingHelper.ValidationRegex)]
    public string? VideoCodec { get; set; }

    /// <summary>
    /// gets or sets Optional. Specify a subtitle codec to encode to.
    /// </summary>
    [FromQuery]
    [RegularExpression(EncodingHelper.ValidationRegex)]
    public string? SubtitleCodec { get; set; }

    /// <summary>
    /// gets or sets  The transcoding reason.
    /// </summary>
    [FromQuery]
    public string? TranscodeReasons { get; set; }

    /// <summary>
    /// gets or sets the audio stream index.
    /// </summary>
    [FromQuery]
    public int? AudioStreamIndex { get; set; }

    /// <summary>
    /// gets or sets the video stream index.
    /// </summary>
    [FromQuery]
    public int? VideoStreamIndex { get; set; }

    /// <summary>
    /// gets or sets the context. <see cref="EncodingContext"/>.
    /// </summary>
    [FromQuery]
    public EncodingContext? Context { get; set; }

    /// <summary>
    /// gets or sets the streaming options.
    /// </summary>
    [FromQuery]
    public Dictionary<string, string> StreamOptions { get; set; } = [];
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
