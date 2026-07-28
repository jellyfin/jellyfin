using System;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Reads container and stream metadata as JSON.
/// </summary>
public sealed record ProbeRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.Probe;

    /// <summary>Gets the input target, already formed and quoted by the caller.</summary>
    public required string Input { get; init; }

    /// <summary>
    /// Gets the per-source tuning arguments: <c>-analyzeduration</c>, <c>-probesize</c>,
    /// <c>-user_agent</c> and the RTSP transport flags. These are properties of the media source
    /// rather than of the process, so they are the caller's responsibility.
    /// </summary>
    public string SourceTuning { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether chapter markers should be reported.</summary>
    public bool IncludeChapters { get; init; }

    /// <summary>
    /// Gets a value indicating whether only the first video frame should be reported, which the
    /// prober must have been verified to support.
    /// </summary>
    public bool FirstVideoFrameOnly { get; init; }

    /// <summary>Gets the decoder thread count. Zero means auto and omits the command-line flag.</summary>
    public int Threads { get; init; }

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Must precede -i. These are input options (how long to sniff for, how many bytes to read,
        // which RTSP transport to negotiate) and FFmpeg binds an input option to the next input
        // named after it.
        if (SourceTuning.Length > 0)
        {
            builder.Append(SourceTuning.Trim()).Append(' ');
        }

        builder.Append("-i ").Append(Input);

        // Position preserved from the original command: -show_streams reports what the headers
        // already say and decodes almost nothing, so there is very little here for extra
        // threads to do.
        if (Threads > 0)
        {
            builder.Append(" -threads ").Append(Threads);
        }

        builder.Append(" -print_format json -show_streams");

        if (IncludeChapters)
        {
            builder.Append(" -show_chapters");
        }

        // -only_first_vframe stops after the first video frame, which is all the caller wants and
        // avoids reporting every frame in the file. Unlike everything else here it is not present in
        // every ffprobe build, so it is only ever set once startup has confirmed the running prober
        // accepts it (CheckSupportedProberOptionAsync); on a build without it the whole run fails.
        if (FirstVideoFrameOnly)
        {
            builder.Append(" -show_frames -only_first_vframe");
        }

        // Container-level metadata: duration, bitrate, format name and tags.
        builder.Append(" -show_format");
    }
}
