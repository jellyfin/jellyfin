using System;
using System.Globalization;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Decodes a single frame and writes it as a still image.
/// </summary>
public sealed record ImageRequest : FFRequest
{
    /// <summary>Value for <see cref="StreamIndex"/> meaning "let FFmpeg pick the stream".</summary>
    public const int AutoStreamIndex = -1;

    /// <inheritdoc />
    public override FFAction Action => FFAction.ExtractImage;

    /// <summary>Gets the input target, already formed and quoted by the caller.</summary>
    public required string Input { get; init; }

    /// <summary>Gets the file to write, already normalised by the caller.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Gets the filter chain, without the <c>-vf</c> prefix.</summary>
    public required string Filters { get; init; }

    /// <summary>Gets the FFmpeg version, which decides how frame sync is spelled.</summary>
    public required Version EncoderVersion { get; init; }

    /// <summary>Gets the demuxer to force, when the container needs one. Empty to let FFmpeg decide.</summary>
    public string InputFormat { get; init; } = string.Empty;

    /// <summary>Gets the position to seek to. Zero takes the frame from the start.</summary>
    public TimeSpan SeekTo { get; init; }

    /// <summary>
    /// Gets the stream to take the image from. <see cref="AutoStreamIndex"/> lets FFmpeg pick.
    /// </summary>
    public int StreamIndex { get; init; } = AutoStreamIndex;

    /// <summary>
    /// Gets the requested output size. <see cref="ImageResolution.MatchSource"/> keeps the source
    /// size and emits no scaling flag.
    /// </summary>
    public ImageResolution Resolution { get; init; } = ImageResolution.MatchSource;

    /// <summary>
    /// Gets a value indicating whether decoding should discard non-keyframes. Faster, but on
    /// containers that cannot seek to a keyframe the frame may be corrupt.
    /// </summary>
    public bool KeyFrameOnly { get; init; }

    /// <summary>Gets the decoder thread count. Zero means auto and omits the flag.</summary>
    public int Threads { get; init; }

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (InputFormat.Length > 0)
        {
            // force the demuxer; FFmpeg's probe cannot always name the container from its bytes
            builder.Append("-f ").Append(InputFormat).Append(' ');
        }

        if (KeyFrameOnly)
        {
            // discard non-keyframes in the decoder, so the seek lands cheaply
            builder.Append("-skip_frame nokey ");
        }

        if (SeekTo > TimeSpan.Zero)
        {
            // before -i, so the demuxer seeks rather than the decoder discarding frames up to here
            builder.Append("-ss ").Append(FormatSeek(SeekTo)).Append(' ');
        }

        builder.Append("-i ").Append(Input);

        if (StreamIndex != AutoStreamIndex)
        {
            // pick the embedded image out of the container by index rather than by heuristic
            builder.Append(" -map 0:").Append(StreamIndex.ToString(CultureInfo.InvariantCulture));
        }

        if (Threads > 0)
        {
            builder.Append(" -threads ").Append(Threads.ToString(CultureInfo.InvariantCulture));
        }

        // -vframes 1: stop after a single decoded frame
        builder.Append(" -vframes 1 -vf ").Append(Filters);

        var size = ResolveSize(Resolution);
        if (size.Length > 0)
        {
            builder.Append(" -s ").Append(size);
        }

        // -1 lets FFmpeg decide the fps mode, which is all a single frame needs
        builder.Append(EncodingHelper.GetVideoSyncOption("-1", EncoderVersion));

        // image2 writes the single frame as a standalone file
        builder.Append(" -f image2 \"").Append(OutputPath).Append('"');
    }

    /// <summary>
    /// FFmpeg wants a duration, not a clock time, but accepts this spelling and it stays readable
    /// in the logs.
    /// </summary>
    private static string FormatSeek(TimeSpan offset)
        => offset.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);

    /// <summary>
    /// FFmpeg's <c>-s</c> takes explicit dimensions, so the configured rung has to be expanded to
    /// a concrete 16:9 frame size.
    /// </summary>
    private static string ResolveSize(ImageResolution resolution) => resolution switch
    {
        ImageResolution.P144 => "256x144",
        ImageResolution.P240 => "426x240",
        ImageResolution.P360 => "640x360",
        ImageResolution.P480 => "854x480",
        ImageResolution.P720 => "1280x720",
        ImageResolution.P1080 => "1920x1080",
        ImageResolution.P1440 => "2560x1440",
        ImageResolution.P2160 => "3840x2160",
        _ => string.Empty
    };
}
