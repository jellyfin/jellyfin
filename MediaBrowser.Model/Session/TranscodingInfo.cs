#nullable disable

using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Session;

/// <summary>
/// Class holding information on a running transcode.
/// </summary>
public class TranscodingInfo
{
    /// <summary>
    /// Gets or sets the thread count used for encoding.
    /// </summary>
    public string AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the thread count used for encoding.
    /// </summary>
    public string VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the thread count used for encoding.
    /// </summary>
    public string Container { get; set; }

    /// <summary>
    /// Gets or sets the codec of the selected subtitle stream, if any.
    /// </summary>
    public string SubtitleCodec { get; set; }

    /// <summary>
    /// Gets or sets how the selected subtitle stream is delivered (for example <c>Encode</c> for
    /// burn-in, <c>Embed</c>, <c>External</c> or <c>Hls</c>). <see langword="null"/> when no
    /// subtitle stream is selected.
    /// </summary>
    public string SubtitleDeliveryMethod { get; set; }

    /// <summary>
    /// Gets or sets the delivery protocol of the transcode (for example <c>hls</c>, <c>dash</c>
    /// or <c>http</c> for progressive streaming).
    /// </summary>
    public string TranscodeProtocol { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the video is passed through.
    /// </summary>
    public bool IsVideoDirect { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the audio is passed through.
    /// </summary>
    public bool IsAudioDirect { get; set; }

    /// <summary>
    /// Gets or sets the bitrate.
    /// </summary>
    public int? Bitrate { get; set; }

    /// <summary>
    /// Gets or sets the target audio bitrate.
    /// </summary>
    public int? AudioBitrate { get; set; }

    /// <summary>
    /// Gets or sets the target video bitrate.
    /// </summary>
    public int? VideoBitrate { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes the transcoder has produced so far.
    /// </summary>
    public long? BytesTranscoded { get; set; }

    /// <summary>
    /// Gets or sets the framerate.
    /// </summary>
    public float? Framerate { get; set; }

    /// <summary>
    /// Gets or sets the encoding speed as a realtime multiplier (e.g. 2.5 means 2.5x realtime).
    /// </summary>
    public float? Speed { get; set; }

    /// <summary>
    /// Gets or sets the completion percentage.
    /// </summary>
    public double? CompletionPercentage { get; set; }

    /// <summary>
    /// Gets or sets the video width.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the video height.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the audio channels.
    /// </summary>
    public int? AudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the hardware acceleration type.
    /// </summary>
    public HardwareAccelerationType? HardwareAccelerationType { get; set; }

    /// <summary>
    /// Gets or sets the transcode reasons.
    /// </summary>
    public TranscodeReason TranscodeReasons { get; set; }

    /// <summary>
    /// Gets or sets the position the transcoder has reached, in ticks. May run ahead of the
    /// playback position because the transcoder buffers content.
    /// </summary>
    public long? TranscodePositionTicks { get; set; }

    /// <summary>
    /// Gets or sets the amount of content, in ticks, the transcoder has produced ahead of the
    /// current playback position.
    /// </summary>
    public long? TranscodeBufferTicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the transcode is currently throttled (paused to
    /// avoid running too far ahead of playback).
    /// </summary>
    public bool IsThrottled { get; set; }

    /// <summary>
    /// Gets or sets the transcoding pipeline, describing the ordered chain of decode, filter and
    /// encode stages.
    /// </summary>
    public TranscodingPipelineInfo Pipeline { get; set; }

    /// <summary>
    /// Creates a shallow copy of this instance, for callers that need to redact fields without
    /// touching the live instance owned by the session.
    /// </summary>
    /// <returns>A shallow copy of this instance.</returns>
    public TranscodingInfo Clone() => (TranscodingInfo)MemberwiseClone();
}
