#pragma warning disable CA1819 // Properties should not return arrays

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Dlna;

/// <summary>
/// Class holding information on a stream.
/// </summary>
public class StreamInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamInfo"/> class.
    /// </summary>
    public StreamInfo()
    {
        AudioCodecs = [];
        VideoCodecs = [];
        SubtitleCodecs = [];
        StreamOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    /// <value>The item id.</value>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the play method.
    /// </summary>
    /// <value>The play method.</value>
    public PlayMethod PlayMethod { get; set; }

    /// <summary>
    /// Gets or sets the encoding context.
    /// </summary>
    /// <value>The encoding context.</value>
    public EncodingContext Context { get; set; }

    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    /// <value>The media type.</value>
    public DlnaProfileType MediaType { get; set; }

    /// <summary>
    /// Gets or sets the container.
    /// </summary>
    /// <value>The container.</value>
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the sub protocol.
    /// </summary>
    /// <value>The sub protocol.</value>
    public MediaStreamProtocol SubProtocol { get; set; }

    /// <summary>
    /// Gets or sets the start position ticks.
    /// </summary>
    /// <value>The start position ticks.</value>
    public long StartPositionTicks { get; set; }

    /// <summary>
    /// Gets or sets the segment length.
    /// </summary>
    /// <value>The segment length.</value>
    public int? SegmentLength { get; set; }

    /// <summary>
    /// Gets or sets the minimum segments count.
    /// </summary>
    /// <value>The minimum segments count.</value>
    public int? MinSegments { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the stream requires AVC.
    /// </summary>
    public bool RequireAvc { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the stream requires AVC.
    /// </summary>
    public bool RequireNonAnamorphic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether timestamps should be copied.
    /// </summary>
    public bool CopyTimestamps { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether timestamps should be copied.
    /// </summary>
    public bool EnableMpegtsM2TsMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the subtitle manifest is enabled.
    /// </summary>
    public bool EnableSubtitlesInManifest { get; set; }

    /// <summary>
    /// Gets or sets the audio codecs.
    /// </summary>
    /// <value>The audio codecs.</value>
    public IReadOnlyList<string> AudioCodecs { get; set; }

    /// <summary>
    /// Gets or sets the video codecs.
    /// </summary>
    /// <value>The video codecs.</value>
    public IReadOnlyList<string> VideoCodecs { get; set; }

    /// <summary>
    /// Gets or sets the audio stream index.
    /// </summary>
    /// <value>The audio stream index.</value>
    public int? AudioStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the video stream index.
    /// </summary>
    /// <value>The subtitle stream index.</value>
    public int? SubtitleStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the maximum transcoding audio channels.
    /// </summary>
    /// <value>The maximum transcoding audio channels.</value>
    public int? TranscodingMaxAudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the global maximum audio channels.
    /// </summary>
    /// <value>The global maximum audio channels.</value>
    public int? GlobalMaxAudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the audio bitrate.
    /// </summary>
    /// <value>The audio bitrate.</value>
    public int? AudioBitrate { get; set; }

    /// <summary>
    /// Gets or sets the audio sample rate.
    /// </summary>
    /// <value>The audio sample rate.</value>
    public int? AudioSampleRate { get; set; }

    /// <summary>
    /// Gets or sets the video bitrate.
    /// </summary>
    /// <value>The video bitrate.</value>
    public int? VideoBitrate { get; set; }

    /// <summary>
    /// Gets or sets the maximum output width.
    /// </summary>
    /// <value>The output width.</value>
    public int? MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum output height.
    /// </summary>
    /// <value>The maximum output height.</value>
    public int? MaxHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum framerate.
    /// </summary>
    /// <value>The maximum framerate.</value>
    public float? MaxFramerate { get; set; }

    /// <summary>
    /// Gets or sets the device profile.
    /// </summary>
    /// <value>The device profile.</value>
    public required DeviceProfile DeviceProfile { get; set; }

    /// <summary>
    /// Gets or sets the device profile id.
    /// </summary>
    /// <value>The device profile id.</value>
    public string? DeviceProfileId { get; set; }

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    /// <value>The device id.</value>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the runtime ticks.
    /// </summary>
    /// <value>The runtime ticks.</value>
    public long? RunTimeTicks { get; set; }

    /// <summary>
    /// Gets or sets the transcode seek info.
    /// </summary>
    /// <value>The transcode seek info.</value>
    public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether content length should be estimated.
    /// </summary>
    public bool EstimateContentLength { get; set; }

    /// <summary>
    /// Gets or sets the media source info.
    /// </summary>
    /// <value>The media source info.</value>
    public MediaSourceInfo? MediaSource { get; set; }

    /// <summary>
    /// Gets or sets the subtitle codecs.
    /// </summary>
    /// <value>The subtitle codecs.</value>
    public IReadOnlyList<string> SubtitleCodecs { get; set; }

    /// <summary>
    /// Gets or sets the subtitle delivery method.
    /// </summary>
    /// <value>The subtitle delivery method.</value>
    public SubtitleDeliveryMethod SubtitleDeliveryMethod { get; set; }

    /// <summary>
    /// Gets or sets the subtitle format.
    /// </summary>
    /// <value>The subtitle format.</value>
    public string? SubtitleFormat { get; set; }

    /// <summary>
    /// Gets or sets the play session id.
    /// </summary>
    /// <value>The play session id.</value>
    public string? PlaySessionId { get; set; }

    /// <summary>
    /// Gets or sets the transcode reasons.
    /// </summary>
    /// <value>The transcode reasons.</value>
    public TranscodeReason TranscodeReasons { get; set; }

    /// <summary>
    /// Gets the stream options.
    /// </summary>
    /// <value>The stream options.</value>
    public Dictionary<string, string> StreamOptions { get; private set; }

    /// <summary>
    /// Gets the media source id.
    /// </summary>
    /// <value>The media source id.</value>
    public string? MediaSourceId => MediaSource?.Id;

    /// <summary>
    /// Gets or sets a value indicating whether audio VBR encoding is enabled.
    /// </summary>
    public bool EnableAudioVbrEncoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether always burn in subtitles when transcoding.
    /// </summary>
    public bool AlwaysBurnInSubtitleWhenTranscoding { get; set; }

    /// <summary>
    /// Gets a value indicating whether the stream is direct.
    /// </summary>
    public bool IsDirectStream => MediaSource?.VideoType is not (VideoType.Dvd or VideoType.BluRay)
        && PlayMethod is PlayMethod.DirectStream or PlayMethod.DirectPlay;

    /// <summary>
    /// Gets the audio stream that will be used in the output stream.
    /// </summary>
    /// <value>The audio stream.</value>
    public MediaStream? TargetAudioStream => MediaSource?.GetDefaultAudioStream(AudioStreamIndex);

    /// <summary>
    /// Gets the video stream that will be used in the output stream.
    /// </summary>
    /// <value>The video stream.</value>
    public MediaStream? TargetVideoStream => MediaSource?.VideoStream;

    /// <summary>
    /// Gets the audio sample rate that will be in the output stream.
    /// </summary>
    /// <value>The target audio sample rate.</value>
    public int? TargetAudioSampleRate
    {
        get
        {
            var stream = TargetAudioStream;
            return AudioSampleRate.HasValue && !IsDirectStream
                ? AudioSampleRate
                : stream?.SampleRate;
        }
    }

    /// <summary>
    /// Gets the audio bit depth that will be in the output stream.
    /// </summary>
    /// <value>The target bit depth.</value>
    public int? TargetAudioBitDepth
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetAudioStream?.BitDepth;
            }

            var targetAudioCodecs = TargetAudioCodec;
            var audioCodec = targetAudioCodecs.Count == 0 ? null : targetAudioCodecs[0];
            if (!string.IsNullOrEmpty(audioCodec))
            {
                return GetTargetAudioBitDepth(audioCodec);
            }

            return TargetAudioStream?.BitDepth;
        }
    }

    /// <summary>
    /// Gets the video bit depth that will be in the output stream.
    /// </summary>
    /// <value>The target video bit depth.</value>
    public int? TargetVideoBitDepth
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetVideoStream?.BitDepth;
            }

            var targetVideoCodecs = TargetVideoCodec;
            var videoCodec = targetVideoCodecs.Count == 0 ? null : targetVideoCodecs[0];
            if (!string.IsNullOrEmpty(videoCodec))
            {
                return GetTargetVideoBitDepth(videoCodec);
            }

            return TargetVideoStream?.BitDepth;
        }
    }

    /// <summary>
    /// Gets the target reference frames that will be in the output stream.
    /// </summary>
    /// <value>The target reference frames.</value>
    public int? TargetRefFrames
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetVideoStream?.RefFrames;
            }

            var targetVideoCodecs = TargetVideoCodec;
            var videoCodec = targetVideoCodecs.Count == 0 ? null : targetVideoCodecs[0];
            if (!string.IsNullOrEmpty(videoCodec))
            {
                return GetTargetRefFrames(videoCodec);
            }

            return TargetVideoStream?.RefFrames;
        }
    }

    /// <summary>
    /// Gets the target framerate that will be in the output stream.
    /// </summary>
    /// <value>The target framerate.</value>
    public float? TargetFramerate
    {
        get
        {
            var stream = TargetVideoStream;
            return MaxFramerate.HasValue && !IsDirectStream
                ? MaxFramerate
                : stream?.ReferenceFrameRate;
        }
    }

    /// <summary>
    /// Gets the target video level that will be in the output stream.
    /// </summary>
    /// <value>The target video level.</value>
    public double? TargetVideoLevel
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetVideoStream?.Level;
            }

            var targetVideoCodecs = TargetVideoCodec;
            var videoCodec = targetVideoCodecs.Count == 0 ? null : targetVideoCodecs[0];
            if (!string.IsNullOrEmpty(videoCodec))
            {
                return GetTargetVideoLevel(videoCodec);
            }

            return TargetVideoStream?.Level;
        }
    }

    /// <summary>
    /// Gets the target packet length that will be in the output stream.
    /// </summary>
    /// <value>The target packet length.</value>
    public int? TargetPacketLength
    {
        get
        {
            var stream = TargetVideoStream;
            return !IsDirectStream
                ? null
                : stream?.PacketLength;
        }
    }

    /// <summary>
    /// Gets the target video profile that will be in the output stream.
    /// </summary>
    /// <value>The target video profile.</value>
    public string? TargetVideoProfile
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetVideoStream?.Profile;
            }

            var targetVideoCodecs = TargetVideoCodec;
            var videoCodec = targetVideoCodecs.Count == 0 ? null : targetVideoCodecs[0];
            if (!string.IsNullOrEmpty(videoCodec))
            {
                return GetOption(videoCodec, "profile");
            }

            return TargetVideoStream?.Profile;
        }
    }

    /// <summary>
    /// Gets the target video range type that will be in the output stream.
    /// </summary>
    /// <value>The video range type.</value>
    public VideoRangeType TargetVideoRangeType
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetVideoStream?.VideoRangeType ?? VideoRangeType.Unknown;
            }

            var targetVideoCodecs = TargetVideoCodec;
            var videoCodec = targetVideoCodecs.Count == 0 ? null : targetVideoCodecs[0];
            if (!string.IsNullOrEmpty(videoCodec)
                && Enum.TryParse(GetOption(videoCodec, "rangetype"), true, out VideoRangeType videoRangeType))
            {
                return videoRangeType;
            }

            return TargetVideoStream?.VideoRangeType ?? VideoRangeType.Unknown;
        }
    }

    /// <summary>
    /// Gets the target video codec tag.
    /// </summary>
    /// <value>The video codec tag.</value>
    public string? TargetVideoCodecTag
    {
        get
        {
            var stream = TargetVideoStream;
            return !IsDirectStream
                ? null
                : stream?.CodecTag;
        }
    }

    /// <summary>
    /// Gets the audio bitrate that will be in the output stream.
    /// </summary>
    /// <value>The audio bitrate.</value>
    public int? TargetAudioBitrate
    {
        get
        {
            var stream = TargetAudioStream;
            return AudioBitrate.HasValue && !IsDirectStream
                ? AudioBitrate
                : stream?.BitRate;
        }
    }

    /// <summary>
    /// Gets the amount of audio channels that will be in the output stream.
    /// </summary>
    /// <value>The target audio channels.</value>
    public int? TargetAudioChannels
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetAudioStream?.Channels;
            }

            var targetAudioCodecs = TargetAudioCodec;
            var codec = targetAudioCodecs.Count == 0 ? null : targetAudioCodecs[0];
            if (!string.IsNullOrEmpty(codec))
            {
                return GetTargetRefFrames(codec);
            }

            return TargetAudioStream?.Channels;
        }
    }

    /// <summary>
    /// Gets the audio codec that will be in the output stream.
    /// </summary>
    /// <value>The audio codec.</value>
    public IReadOnlyList<string> TargetAudioCodec
    {
        get
        {
            var stream = TargetAudioStream;

            string? inputCodec = stream?.Codec;

            if (IsDirectStream)
            {
                return string.IsNullOrEmpty(inputCodec) ? [] : [inputCodec];
            }

            foreach (string codec in AudioCodecs)
            {
                if (string.Equals(codec, inputCodec, StringComparison.OrdinalIgnoreCase))
                {
                    return string.IsNullOrEmpty(codec) ? [] : [codec];
                }
            }

            return AudioCodecs;
        }
    }

    /// <summary>
    /// Gets the video codec that will be in the output stream.
    /// </summary>
    /// <value>The target video codec.</value>
    public IReadOnlyList<string> TargetVideoCodec
    {
        get
        {
            var stream = TargetVideoStream;

            string? inputCodec = stream?.Codec;

            if (IsDirectStream)
            {
                return string.IsNullOrEmpty(inputCodec) ? [] : [inputCodec];
            }

            foreach (string codec in VideoCodecs)
            {
                if (string.Equals(codec, inputCodec, StringComparison.OrdinalIgnoreCase))
                {
                    return string.IsNullOrEmpty(codec) ? [] : [codec];
                }
            }

            return VideoCodecs;
        }
    }

    /// <summary>
    /// Gets the target size of the output stream.
    /// </summary>
    /// <value>The target size.</value>
    public long? TargetSize
    {
        get
        {
            if (IsDirectStream)
            {
                return MediaSource?.Size;
            }

            if (RunTimeTicks.HasValue)
            {
                int? totalBitrate = TargetTotalBitrate;

                double totalSeconds = RunTimeTicks.Value;
                // Convert to ms
                totalSeconds /= 10000;
                // Convert to seconds
                totalSeconds /= 1000;

                return totalBitrate.HasValue ?
                    Convert.ToInt64(totalBitrate.Value * totalSeconds) :
                    null;
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the target video bitrate of the output stream.
    /// </summary>
    /// <value>The video bitrate.</value>
    public int? TargetVideoBitrate
    {
        get
        {
            var stream = TargetVideoStream;

            return VideoBitrate.HasValue && !IsDirectStream
                ? VideoBitrate
                : stream?.BitRate;
        }
    }

    /// <summary>
    /// Gets the target timestamp of the output stream.
    /// </summary>
    /// <value>The target timestamp.</value>
    public TransportStreamTimestamp TargetTimestamp
    {
        get
        {
            var defaultValue = string.Equals(Container, "m2ts", StringComparison.OrdinalIgnoreCase)
                ? TransportStreamTimestamp.Valid
                : TransportStreamTimestamp.None;

            return !IsDirectStream
                ? defaultValue
                : MediaSource is null ? defaultValue : MediaSource.Timestamp ?? TransportStreamTimestamp.None;
        }
    }

    /// <summary>
    /// Gets the target total bitrate of the output stream.
    /// </summary>
    /// <value>The target total bitrate.</value>
    public int? TargetTotalBitrate => (TargetAudioBitrate ?? 0) + (TargetVideoBitrate ?? 0);

    /// <summary>
    /// Gets a value indicating whether the output stream is anamorphic.
    /// </summary>
    public bool? IsTargetAnamorphic
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetVideoStream?.IsAnamorphic;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the output stream is interlaced.
    /// </summary>
    public bool? IsTargetInterlaced
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetVideoStream?.IsInterlaced;
            }

            var targetVideoCodecs = TargetVideoCodec;
            var videoCodec = targetVideoCodecs.Count == 0 ? null : targetVideoCodecs[0];
            if (!string.IsNullOrEmpty(videoCodec))
            {
                if (string.Equals(GetOption(videoCodec, "deinterlace"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return TargetVideoStream?.IsInterlaced;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the output stream is AVC.
    /// </summary>
    public bool? IsTargetAVC
    {
        get
        {
            if (IsDirectStream)
            {
                return TargetVideoStream?.IsAVC;
            }

            return true;
        }
    }

    /// <summary>
    /// Gets the target width of the output stream.
    /// </summary>
    /// <value>The target width.</value>
    public int? TargetWidth
    {
        get
        {
            var videoStream = TargetVideoStream;

            if (videoStream is not null && videoStream.Width.HasValue && videoStream.Height.HasValue)
            {
                ImageDimensions size = new ImageDimensions(videoStream.Width.Value, videoStream.Height.Value);

                size = DrawingUtils.Resize(size, 0, 0, MaxWidth ?? 0, MaxHeight ?? 0);

                return size.Width;
            }

            return MaxWidth;
        }
    }

    /// <summary>
    /// Gets the target height of the output stream.
    /// </summary>
    /// <value>The target height.</value>
    public int? TargetHeight
    {
        get
        {
            var videoStream = TargetVideoStream;

            if (videoStream is not null && videoStream.Width.HasValue && videoStream.Height.HasValue)
            {
                ImageDimensions size = new ImageDimensions(videoStream.Width.Value, videoStream.Height.Value);

                size = DrawingUtils.Resize(size, 0, 0, MaxWidth ?? 0, MaxHeight ?? 0);

                return size.Height;
            }

            return MaxHeight;
        }
    }

    /// <summary>
    /// Gets the target video stream count of the output stream.
    /// </summary>
    /// <value>The target video stream count.</value>
    public int? TargetVideoStreamCount
    {
        get
        {
            if (IsDirectStream)
            {
                return GetMediaStreamCount(MediaStreamType.Video, int.MaxValue);
            }

            return GetMediaStreamCount(MediaStreamType.Video, 1);
        }
    }

    /// <summary>
    /// Gets the target audio stream count of the output stream.
    /// </summary>
    /// <value>The target audio stream count.</value>
    public int? TargetAudioStreamCount
    {
        get
        {
            if (IsDirectStream)
            {
                return GetMediaStreamCount(MediaStreamType.Audio, int.MaxValue);
            }

            return GetMediaStreamCount(MediaStreamType.Audio, 1);
        }
    }

    /// <summary>
    /// Sets a stream option.
    /// </summary>
    /// <param name="qualifier">The qualifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    public void SetOption(string? qualifier, string name, string value)
    {
        if (string.IsNullOrEmpty(qualifier))
        {
            SetOption(name, value);
        }
        else
        {
            SetOption(qualifier + "-" + name, value);
        }
    }

    /// <summary>
    /// Sets a stream option.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    public void SetOption(string name, string value)
    {
        StreamOptions[name] = value;
    }

    /// <summary>
    /// Gets a stream option.
    /// </summary>
    /// <param name="qualifier">The qualifier.</param>
    /// <param name="name">The name.</param>
    /// <returns>The value.</returns>
    public string? GetOption(string? qualifier, string name)
    {
        var value = GetOption(qualifier + "-" + name);

        if (string.IsNullOrEmpty(value))
        {
            value = GetOption(name);
        }

        return value;
    }

    /// <summary>
    /// Gets a stream option.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The value.</returns>
    public string? GetOption(string name)
    {
        if (StreamOptions.TryGetValue(name, out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// Returns this output stream URL for this class.
    /// </summary>
    /// <param name="baseUrl">The base Url.</param>
    /// <param name="accessToken">The access Token.</param>
    /// <param name="query">Optional extra query.</param>
    /// <returns>A querystring representation of this object.</returns>
    public string ToUrl(string? baseUrl, string? accessToken, string? query)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            sb.Append(baseUrl.TrimEnd('/'));
        }

        if (MediaType == DlnaProfileType.Audio)
        {
            sb.Append("/audio/");
        }
        else
        {
            sb.Append("/videos/");
        }

        sb.Append(ItemId);

        if (SubProtocol == MediaStreamProtocol.hls)
        {
            sb.Append("/master.m3u8");
        }
        else
        {
            sb.Append("/stream");

            if (!string.IsNullOrEmpty(Container))
            {
                sb.Append('.');
                sb.Append(Container);
            }
        }

        var queryStart = sb.Length;

        if (!string.IsNullOrEmpty(DeviceProfileId))
        {
            sb.Append("&DeviceProfileId=");
            sb.Append(DeviceProfileId);
        }

        if (!string.IsNullOrEmpty(DeviceId))
        {
            sb.Append("&DeviceId=");
            sb.Append(DeviceId);
        }

        if (!string.IsNullOrEmpty(MediaSourceId))
        {
            sb.Append("&MediaSourceId=");
            sb.Append(MediaSourceId);
        }

        // default true so don't store.
        if (IsDirectStream)
        {
            sb.Append("&Static=true");
        }

        if (VideoCodecs.Count != 0)
        {
            sb.Append("&VideoCodec=");
            sb.AppendJoin(',', VideoCodecs);
        }

        if (AudioCodecs.Count != 0)
        {
            sb.Append("&AudioCodec=");
            sb.AppendJoin(',', AudioCodecs);
        }

        if (AudioStreamIndex.HasValue)
        {
            sb.Append("&AudioStreamIndex=");
            sb.Append(AudioStreamIndex.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (SubtitleStreamIndex.HasValue && (AlwaysBurnInSubtitleWhenTranscoding || SubtitleDeliveryMethod != SubtitleDeliveryMethod.External) && SubtitleStreamIndex != -1)
        {
            sb.Append("&SubtitleStreamIndex=");
            sb.Append(SubtitleStreamIndex.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (VideoBitrate.HasValue)
        {
            sb.Append("&VideoBitrate=");
            sb.Append(VideoBitrate.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (AudioBitrate.HasValue)
        {
            sb.Append("&AudioBitrate=");
            sb.Append(AudioBitrate.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (AudioSampleRate.HasValue)
        {
            sb.Append("&AudioSampleRate=");
            sb.Append(AudioSampleRate.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (MaxFramerate.HasValue)
        {
            sb.Append("&MaxFramerate=");
            sb.Append(MaxFramerate.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (MaxWidth.HasValue)
        {
            sb.Append("&MaxWidth=");
            sb.Append(MaxWidth.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (MaxHeight.HasValue)
        {
            sb.Append("&MaxHeight=");
            sb.Append(MaxHeight.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (SubProtocol == MediaStreamProtocol.hls)
        {
            if (!string.IsNullOrEmpty(Container))
            {
                sb.Append("&SegmentContainer=");
                sb.Append(Container);
            }

            if (SegmentLength.HasValue)
            {
                sb.Append("&SegmentLength=");
                sb.Append(SegmentLength.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (MinSegments.HasValue)
            {
                sb.Append("&MinSegments=");
                sb.Append(MinSegments.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
        else
        {
            if (StartPositionTicks != 0)
            {
                sb.Append("&StartTimeTicks=");
                sb.Append(StartPositionTicks.ToString(CultureInfo.InvariantCulture));
            }
        }

        if (!string.IsNullOrEmpty(PlaySessionId))
        {
            sb.Append("&PlaySessionId=");
            sb.Append(PlaySessionId);
        }

        if (!string.IsNullOrEmpty(accessToken))
        {
            sb.Append("&ApiKey=");
            sb.Append(accessToken);
        }

        var liveStreamId = MediaSource?.LiveStreamId;
        if (!string.IsNullOrEmpty(liveStreamId))
        {
            sb.Append("&LiveStreamId=");
            sb.Append(liveStreamId);
        }

        if (!IsDirectStream)
        {
            if (RequireNonAnamorphic)
            {
                sb.Append("&RequireNonAnamorphic=");
                sb.Append(RequireNonAnamorphic.ToString(CultureInfo.InvariantCulture));
            }

            if (TranscodingMaxAudioChannels.HasValue)
            {
                sb.Append("&TranscodingMaxAudioChannels=");
                sb.Append(TranscodingMaxAudioChannels.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (EnableSubtitlesInManifest)
            {
                sb.Append("&EnableSubtitlesInManifest=");
                sb.Append(EnableSubtitlesInManifest.ToString(CultureInfo.InvariantCulture));
            }

            if (EnableMpegtsM2TsMode)
            {
                sb.Append("&EnableMpegtsM2TsMode=");
                sb.Append(EnableMpegtsM2TsMode.ToString(CultureInfo.InvariantCulture));
            }

            if (EstimateContentLength)
            {
                sb.Append("&EstimateContentLength=");
                sb.Append(EstimateContentLength.ToString(CultureInfo.InvariantCulture));
            }

            if (TranscodeSeekInfo != TranscodeSeekInfo.Auto)
            {
                sb.Append("&TranscodeSeekInfo=");
                sb.Append(TranscodeSeekInfo.ToString());
            }

            if (CopyTimestamps)
            {
                sb.Append("&CopyTimestamps=");
                sb.Append(CopyTimestamps.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append("&RequireAvc=");
            sb.Append(RequireAvc.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

            sb.Append("&EnableAudioVbrEncoding=");
            sb.Append(EnableAudioVbrEncoding.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
        }

        var etag = MediaSource?.ETag;
        if (!string.IsNullOrEmpty(etag))
        {
            sb.Append("&Tag=");
            sb.Append(etag);
        }

        if (SubtitleStreamIndex.HasValue && SubtitleDeliveryMethod != SubtitleDeliveryMethod.External)
        {
            sb.Append("&SubtitleMethod=");
            sb.Append(SubtitleDeliveryMethod);
        }

        if (SubtitleStreamIndex.HasValue && SubtitleDeliveryMethod == SubtitleDeliveryMethod.Embed && SubtitleCodecs.Count != 0)
        {
            sb.Append("&SubtitleCodec=");
            sb.AppendJoin(',', SubtitleCodecs);
        }

        foreach (var pair in StreamOptions)
        {
            // Strip spaces to avoid having to encode h264 profile names
            sb.Append('&');
            sb.Append(pair.Key);
            sb.Append('=');
            sb.Append(pair.Value.Replace(" ", string.Empty, StringComparison.Ordinal));
        }

        var transcodeReasonsValues = TranscodeReasons.GetUniqueFlags().ToArray();
        if (!IsDirectStream && transcodeReasonsValues.Length > 0)
        {
            sb.Append("&TranscodeReasons=");
            sb.AppendJoin(',', transcodeReasonsValues);
        }

        if (!string.IsNullOrEmpty(query))
        {
            sb.Append(query);
        }

        // Replace the first '&' with '?' to form a valid query string.
        if (sb.Length > queryStart)
        {
            sb[queryStart] = '?';
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the subtitle profiles.
    /// </summary>
    /// <param name="transcoderSupport">The transcoder support.</param>
    /// <param name="includeSelectedTrackOnly">If only the selected track should be included.</param>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="accessToken">The access token.</param>
    /// <returns>The <see cref="SubtitleStreamInfo"/> of the profiles.</returns>
    public IEnumerable<SubtitleStreamInfo> GetSubtitleProfiles(ITranscoderSupport transcoderSupport, bool includeSelectedTrackOnly, string baseUrl, string? accessToken)
    {
        return GetSubtitleProfiles(transcoderSupport, includeSelectedTrackOnly, false, baseUrl, accessToken);
    }

    /// <summary>
    /// Gets the subtitle profiles.
    /// </summary>
    /// <param name="transcoderSupport">The transcoder support.</param>
    /// <param name="includeSelectedTrackOnly">If only the selected track should be included.</param>
    /// <param name="enableAllProfiles">If all profiles are enabled.</param>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="accessToken">The access token.</param>
    /// <returns>The <see cref="SubtitleStreamInfo"/> of the profiles.</returns>
    public IEnumerable<SubtitleStreamInfo> GetSubtitleProfiles(ITranscoderSupport transcoderSupport, bool includeSelectedTrackOnly, bool enableAllProfiles, string baseUrl, string? accessToken)
    {
        if (MediaSource is null)
        {
            return [];
        }

        List<SubtitleStreamInfo> list = [];

        // HLS will preserve timestamps so we can just grab the full subtitle stream
        long startPositionTicks = SubProtocol == MediaStreamProtocol.hls
            ? 0
            : (PlayMethod == PlayMethod.Transcode && !CopyTimestamps ? StartPositionTicks : 0);

        // First add the selected track
        if (SubtitleStreamIndex.HasValue)
        {
            foreach (var stream in MediaSource.MediaStreams)
            {
                if (stream.Type == MediaStreamType.Subtitle && stream.Index == SubtitleStreamIndex.Value)
                {
                    AddSubtitleProfiles(list, stream, transcoderSupport, enableAllProfiles, baseUrl, accessToken, startPositionTicks);
                }
            }
        }

        if (!includeSelectedTrackOnly)
        {
            foreach (var stream in MediaSource.MediaStreams)
            {
                if (stream.Type == MediaStreamType.Subtitle && (!SubtitleStreamIndex.HasValue || stream.Index != SubtitleStreamIndex.Value))
                {
                    AddSubtitleProfiles(list, stream, transcoderSupport, enableAllProfiles, baseUrl, accessToken, startPositionTicks);
                }
            }
        }

        return list;
    }

    private void AddSubtitleProfiles(List<SubtitleStreamInfo> list, MediaStream stream, ITranscoderSupport transcoderSupport, bool enableAllProfiles, string baseUrl, string? accessToken, long startPositionTicks)
    {
        if (enableAllProfiles)
        {
            foreach (var profile in DeviceProfile.SubtitleProfiles)
            {
                var info = GetSubtitleStreamInfo(stream, baseUrl, accessToken, startPositionTicks, new[] { profile }, transcoderSupport);
                if (info is not null)
                {
                    list.Add(info);
                }
            }
        }
        else
        {
            var info = GetSubtitleStreamInfo(stream, baseUrl, accessToken, startPositionTicks, DeviceProfile.SubtitleProfiles, transcoderSupport);
            if (info is not null)
            {
                list.Add(info);
            }
        }
    }

    private SubtitleStreamInfo? GetSubtitleStreamInfo(MediaStream stream, string baseUrl, string? accessToken, long startPositionTicks, SubtitleProfile[] subtitleProfiles, ITranscoderSupport transcoderSupport)
    {
        if (MediaSource is null)
        {
            return null;
        }

        var subtitleProfile = StreamBuilder.GetSubtitleProfile(MediaSource, stream, subtitleProfiles, PlayMethod, transcoderSupport, Container, SubProtocol);
        var info = new SubtitleStreamInfo
        {
            IsForced = stream.IsForced,
            Language = stream.Language,
            Name = stream.Language ?? "Unknown",
            Format = subtitleProfile.Format,
            Index = stream.Index,
            DeliveryMethod = subtitleProfile.Method,
            DisplayTitle = stream.DisplayTitle
        };

        if (info.DeliveryMethod == SubtitleDeliveryMethod.External)
        {
            // Default to using the API URL
            info.Url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/Videos/{1}/{2}/Subtitles/{3}/{4}/Stream.{5}",
                baseUrl,
                ItemId,
                MediaSourceId,
                stream.Index.ToString(CultureInfo.InvariantCulture),
                startPositionTicks.ToString(CultureInfo.InvariantCulture),
                subtitleProfile.Format);
            info.IsExternalUrl = false;

            // Check conditions for potentially using the direct path
            if (stream.IsExternal // Must be external
                && stream.SupportsExternalStream
                && string.Equals(stream.Codec, subtitleProfile.Format, StringComparison.OrdinalIgnoreCase) // Format must match (no conversion needed)
                && !string.IsNullOrEmpty(stream.Path) // Path must exist
                && Uri.TryCreate(stream.Path, UriKind.Absolute, out Uri? uriResult) // Path must be an absolute URI
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)) // Scheme must be HTTP or HTTPS
            {
                // All conditions met, override with the direct path
                info.Url = stream.Path;
                info.IsExternalUrl = true;
            }

            // Append ApiKey only if we are using the API URL
            if (!info.IsExternalUrl && !string.IsNullOrEmpty(accessToken))
            {
                // Use "?ApiKey=" as seen in HEAD and other parts of the code
                info.Url += "?ApiKey=" + accessToken;
            }
        }

        return info;
    }

    /// <summary>
    /// Gets the target video bit depth.
    /// </summary>
    /// <param name="codec">The codec.</param>
    /// <returns>The target video bit depth.</returns>
    public int? GetTargetVideoBitDepth(string? codec)
    {
        var value = GetOption(codec, "videobitdepth");

        if (int.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Gets the target audio bit depth.
    /// </summary>
    /// <param name="codec">The codec.</param>
    /// <returns>The target audio bit depth.</returns>
    public int? GetTargetAudioBitDepth(string? codec)
    {
        var value = GetOption(codec, "audiobitdepth");

        if (int.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Gets the target video level.
    /// </summary>
    /// <param name="codec">The codec.</param>
    /// <returns>The target video level.</returns>
    public double? GetTargetVideoLevel(string? codec)
    {
        var value = GetOption(codec, "level");

        if (double.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Gets the target reference frames.
    /// </summary>
    /// <param name="codec">The codec.</param>
    /// <returns>The target reference frames.</returns>
    public int? GetTargetRefFrames(string? codec)
    {
        var value = GetOption(codec, "maxrefframes");

        if (int.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Gets the target audio channels.
    /// </summary>
    /// <param name="codec">The codec.</param>
    /// <returns>The target audio channels.</returns>
    public int? GetTargetAudioChannels(string? codec)
    {
        var defaultValue = GlobalMaxAudioChannels ?? TranscodingMaxAudioChannels;

        var value = GetOption(codec, "audiochannels");
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return Math.Min(result, defaultValue ?? result);
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets the media stream count.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="limit">The limit.</param>
    /// <returns>The media stream count.</returns>
    private int? GetMediaStreamCount(MediaStreamType type, int limit)
    {
        var count = MediaSource?.GetStreamCount(type);

        if (count.HasValue)
        {
            count = Math.Min(count.Value, limit);
        }

        return count;
    }
}
