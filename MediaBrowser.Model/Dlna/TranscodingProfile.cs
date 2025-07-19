using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Dlna;

/// <summary>
/// A class for transcoding profile information.
/// Note for client developers: Conditions defined in <see cref="CodecProfile"/> has higher priority and can override values defined here.
/// </summary>
public class TranscodingProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodingProfile" /> class.
    /// </summary>
    public TranscodingProfile()
    {
        Conditions = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodingProfile" /> class copying the values from another instance.
    /// </summary>
    /// <param name="other">Another instance of <see cref="TranscodingProfile" /> to be copied.</param>
    public TranscodingProfile(TranscodingProfile other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Container = other.Container;
        Type = other.Type;
        VideoCodec = other.VideoCodec;
        AudioCodec = other.AudioCodec;
        Protocol = other.Protocol;
        EstimateContentLength = other.EstimateContentLength;
        EnableMpegtsM2TsMode = other.EnableMpegtsM2TsMode;
        TranscodeSeekInfo = other.TranscodeSeekInfo;
        CopyTimestamps = other.CopyTimestamps;
        Context = other.Context;
        EnableSubtitlesInManifest = other.EnableSubtitlesInManifest;
        MaxAudioChannels = other.MaxAudioChannels;
        MinSegments = other.MinSegments;
        SegmentLength = other.SegmentLength;
        BreakOnNonKeyFrames = other.BreakOnNonKeyFrames;
        Conditions = other.Conditions;
        EnableAudioVbrEncoding = other.EnableAudioVbrEncoding;
    }

    /// <summary>
    /// Gets or sets the container.
    /// </summary>
    [XmlAttribute("container")]
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DLNA profile type.
    /// </summary>
    [XmlAttribute("type")]
    public DlnaProfileType Type { get; set; }

    /// <summary>
    /// Gets or sets the video codec.
    /// </summary>
    [XmlAttribute("videoCodec")]
    public string VideoCodec { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audio codec.
    /// </summary>
    [XmlAttribute("audioCodec")]
    public string AudioCodec { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the protocol.
    /// </summary>
    [XmlAttribute("protocol")]
    public MediaStreamProtocol Protocol { get; set; } = MediaStreamProtocol.http;

    /// <summary>
    /// Gets or sets a value indicating whether the content length should be estimated.
    /// </summary>
    [DefaultValue(false)]
    [XmlAttribute("estimateContentLength")]
    public bool EstimateContentLength { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether M2TS mode is enabled.
    /// </summary>
    [DefaultValue(false)]
    [XmlAttribute("enableMpegtsM2TsMode")]
    public bool EnableMpegtsM2TsMode { get; set; }

    /// <summary>
    /// Gets or sets the transcoding seek info mode.
    /// </summary>
    [DefaultValue(TranscodeSeekInfo.Auto)]
    [XmlAttribute("transcodeSeekInfo")]
    public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether timestamps should be copied.
    /// </summary>
    [DefaultValue(false)]
    [XmlAttribute("copyTimestamps")]
    public bool CopyTimestamps { get; set; }

    /// <summary>
    /// Gets or sets the encoding context.
    /// </summary>
    [DefaultValue(EncodingContext.Streaming)]
    [XmlAttribute("context")]
    public EncodingContext Context { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether subtitles are allowed in the manifest.
    /// </summary>
    [DefaultValue(false)]
    [XmlAttribute("enableSubtitlesInManifest")]
    public bool EnableSubtitlesInManifest { get; set; }

    /// <summary>
    /// Gets or sets the maximum audio channels.
    /// </summary>
    [XmlAttribute("maxAudioChannels")]
    public string? MaxAudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the minimum amount of segments.
    /// </summary>
    [DefaultValue(0)]
    [XmlAttribute("minSegments")]
    public int MinSegments { get; set; }

    /// <summary>
    /// Gets or sets the segment length.
    /// </summary>
    [DefaultValue(0)]
    [XmlAttribute("segmentLength")]
    public int SegmentLength { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether breaking the video stream on non-keyframes is supported.
    /// </summary>
    [DefaultValue(false)]
    [XmlAttribute("breakOnNonKeyFrames")]
    public bool BreakOnNonKeyFrames { get; set; }

    /// <summary>
    /// Gets or sets the profile conditions.
    /// </summary>
    public ProfileCondition[] Conditions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether variable bitrate encoding is supported.
    /// </summary>
    [DefaultValue(true)]
    [XmlAttribute("enableAudioVbrEncoding")]
    public bool EnableAudioVbrEncoding { get; set; } = true;
}
