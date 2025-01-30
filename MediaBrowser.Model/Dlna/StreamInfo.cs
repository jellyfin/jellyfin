using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Data.Enums;
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
    /// Gets or sets a value indicating whether the stream can be broken on non-keyframes.
    /// </summary>
    public bool BreakOnNonKeyFrames { get; set; }

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
    /// <returns>A querystring representation of this object.</returns>
    public string ToUrl(string baseUrl, string? accessToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);

        List<string> list = [];
        foreach (NameValuePair pair in BuildParams(this, accessToken))
        {
            if (string.IsNullOrEmpty(pair.Value))
            {
                continue;
            }

            // Try to keep the url clean by omitting defaults
            if (string.Equals(pair.Name, "StartTimeTicks", StringComparison.OrdinalIgnoreCase)
                && string.Equals(pair.Value, "0", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(pair.Name, "SubtitleStreamIndex", StringComparison.OrdinalIgnoreCase)
                && string.Equals(pair.Value, "-1", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(pair.Name, "Static", StringComparison.OrdinalIgnoreCase)
                && string.Equals(pair.Value, "false", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var encodedValue = pair.Value.Replace(" ", "%20", StringComparison.Ordinal);

            list.Add(string.Format(CultureInfo.InvariantCulture, "{0}={1}", pair.Name, encodedValue));
        }

        string queryString = string.Join('&', list);

        return GetUrl(baseUrl, queryString);
    }

    private string GetUrl(string baseUrl, string queryString)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);

        string extension = string.IsNullOrEmpty(Container) ? string.Empty : "." + Container;

        baseUrl = baseUrl.TrimEnd('/');

        if (MediaType == DlnaProfileType.Audio)
        {
            if (SubProtocol == MediaStreamProtocol.hls)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}/audio/{1}/master.m3u8?{2}", baseUrl, ItemId, queryString);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}/audio/{1}/stream{2}?{3}", baseUrl, ItemId, extension, queryString);
        }

        if (SubProtocol == MediaStreamProtocol.hls)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}/videos/{1}/master.m3u8?{2}", baseUrl, ItemId, queryString);
        }

        return string.Format(CultureInfo.InvariantCulture, "{0}/videos/{1}/stream{2}?{3}", baseUrl, ItemId, extension, queryString);
    }

    private static List<NameValuePair> BuildParams(StreamInfo item, string? accessToken)
    {
        List<NameValuePair> list = [];

        string audioCodecs = item.AudioCodecs.Count == 0 ?
            string.Empty :
            string.Join(',', item.AudioCodecs);

        string videoCodecs = item.VideoCodecs.Count == 0 ?
            string.Empty :
            string.Join(',', item.VideoCodecs);

        list.Add(new NameValuePair("DeviceProfileId", item.DeviceProfileId ?? string.Empty));
        list.Add(new NameValuePair("DeviceId", item.DeviceId ?? string.Empty));
        list.Add(new NameValuePair("MediaSourceId", item.MediaSourceId ?? string.Empty));
        list.Add(new NameValuePair("Static", item.IsDirectStream.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
        list.Add(new NameValuePair("VideoCodec", videoCodecs));
        list.Add(new NameValuePair("AudioCodec", audioCodecs));
        list.Add(new NameValuePair("AudioStreamIndex", item.AudioStreamIndex.HasValue ? item.AudioStreamIndex.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
        list.Add(new NameValuePair("SubtitleStreamIndex", item.SubtitleStreamIndex.HasValue && (item.AlwaysBurnInSubtitleWhenTranscoding || item.SubtitleDeliveryMethod != SubtitleDeliveryMethod.External) ? item.SubtitleStreamIndex.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
        list.Add(new NameValuePair("VideoBitrate", item.VideoBitrate.HasValue ? item.VideoBitrate.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
        list.Add(new NameValuePair("AudioBitrate", item.AudioBitrate.HasValue ? item.AudioBitrate.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
        list.Add(new NameValuePair("AudioSampleRate", item.AudioSampleRate.HasValue ? item.AudioSampleRate.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));

        list.Add(new NameValuePair("MaxFramerate", item.MaxFramerate.HasValue ? item.MaxFramerate.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
        list.Add(new NameValuePair("MaxWidth", item.MaxWidth.HasValue ? item.MaxWidth.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));
        list.Add(new NameValuePair("MaxHeight", item.MaxHeight.HasValue ? item.MaxHeight.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));

        long startPositionTicks = item.StartPositionTicks;

        if (item.SubProtocol == MediaStreamProtocol.hls)
        {
            list.Add(new NameValuePair("StartTimeTicks", string.Empty));
        }
        else
        {
            list.Add(new NameValuePair("StartTimeTicks", startPositionTicks.ToString(CultureInfo.InvariantCulture)));
        }

        list.Add(new NameValuePair("PlaySessionId", item.PlaySessionId ?? string.Empty));
        list.Add(new NameValuePair("ApiKey", accessToken ?? string.Empty));

        string? liveStreamId = item.MediaSource?.LiveStreamId;
        list.Add(new NameValuePair("LiveStreamId", liveStreamId ?? string.Empty));

        list.Add(new NameValuePair("SubtitleMethod", item.SubtitleStreamIndex.HasValue && item.SubtitleDeliveryMethod != SubtitleDeliveryMethod.External ? item.SubtitleDeliveryMethod.ToString() : string.Empty));

        if (!item.IsDirectStream)
        {
            if (item.RequireNonAnamorphic)
            {
                list.Add(new NameValuePair("RequireNonAnamorphic", item.RequireNonAnamorphic.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
            }

            list.Add(new NameValuePair("TranscodingMaxAudioChannels", item.TranscodingMaxAudioChannels.HasValue ? item.TranscodingMaxAudioChannels.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));

            if (item.EnableSubtitlesInManifest)
            {
                list.Add(new NameValuePair("EnableSubtitlesInManifest", item.EnableSubtitlesInManifest.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
            }

            if (item.EnableMpegtsM2TsMode)
            {
                list.Add(new NameValuePair("EnableMpegtsM2TsMode", item.EnableMpegtsM2TsMode.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
            }

            if (item.EstimateContentLength)
            {
                list.Add(new NameValuePair("EstimateContentLength", item.EstimateContentLength.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
            }

            if (item.TranscodeSeekInfo != TranscodeSeekInfo.Auto)
            {
                list.Add(new NameValuePair("TranscodeSeekInfo", item.TranscodeSeekInfo.ToString().ToLowerInvariant()));
            }

            if (item.CopyTimestamps)
            {
                list.Add(new NameValuePair("CopyTimestamps", item.CopyTimestamps.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
            }

            list.Add(new NameValuePair("RequireAvc", item.RequireAvc.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));

            list.Add(new NameValuePair("EnableAudioVbrEncoding", item.EnableAudioVbrEncoding.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
        }

        list.Add(new NameValuePair("Tag", item.MediaSource?.ETag ?? string.Empty));

        string subtitleCodecs = item.SubtitleCodecs.Count == 0 ?
            string.Empty :
            string.Join(",", item.SubtitleCodecs);

        list.Add(new NameValuePair("SubtitleCodec", item.SubtitleStreamIndex.HasValue && item.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Embed ? subtitleCodecs : string.Empty));

        if (item.SubProtocol == MediaStreamProtocol.hls)
        {
            list.Add(new NameValuePair("SegmentContainer", item.Container ?? string.Empty));

            if (item.SegmentLength.HasValue)
            {
                list.Add(new NameValuePair("SegmentLength", item.SegmentLength.Value.ToString(CultureInfo.InvariantCulture)));
            }

            if (item.MinSegments.HasValue)
            {
                list.Add(new NameValuePair("MinSegments", item.MinSegments.Value.ToString(CultureInfo.InvariantCulture)));
            }

            list.Add(new NameValuePair("BreakOnNonKeyFrames", item.BreakOnNonKeyFrames.ToString(CultureInfo.InvariantCulture)));
        }

        foreach (var pair in item.StreamOptions)
        {
            if (string.IsNullOrEmpty(pair.Value))
            {
                continue;
            }

            // strip spaces to avoid having to encode h264 profile names
            list.Add(new NameValuePair(pair.Key, pair.Value.Replace(" ", string.Empty, StringComparison.Ordinal)));
        }

        if (!item.IsDirectStream)
        {
            list.Add(new NameValuePair("TranscodeReasons", item.TranscodeReasons.ToString()));
        }

        return list;
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
            if (MediaSource.Protocol == MediaProtocol.File || !string.Equals(stream.Codec, subtitleProfile.Format, StringComparison.OrdinalIgnoreCase) || !stream.IsExternal)
            {
                info.Url = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/Videos/{1}/{2}/Subtitles/{3}/{4}/Stream.{5}",
                    baseUrl,
                    ItemId,
                    MediaSourceId,
                    stream.Index.ToString(CultureInfo.InvariantCulture),
                    startPositionTicks.ToString(CultureInfo.InvariantCulture),
                    subtitleProfile.Format);

                if (!string.IsNullOrEmpty(accessToken))
                {
                    info.Url += "?ApiKey=" + accessToken;
                }

                info.IsExternalUrl = false;
            }
            else
            {
                info.Url = stream.Path;
                info.IsExternalUrl = true;
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
