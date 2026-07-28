using System;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Reads keyframe packet timings so that HLS segmenting can align to them.
/// </summary>
public sealed record KeyframeScanRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.ScanKeyframes;

    /// <summary>Gets the file to scan.</summary>
    public required string FilePath { get; init; }

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Synthesise timestamps for a container that carries none. Without it such a source reports
        // empty pts_time fields, and a keyframe with no timestamp tells a segmenter nothing.
        builder.Append("-fflags +genpts ")

            // Inert for this query, and kept only so the scan asks for exactly what it always has.
            // -skip_frame is a decoder option, but -show_entries packet= reads the demuxer's packet
            // headers and decodes nothing, so there are no frames to skip.
            .Append("-skip_frame nokey ")

            // Ask for both durations because either can be missing. The parser prefers the stream's
            // and falls back to the container's. These arrive as single-field rows after the packets.
            .Append("-show_entries format=duration ")
            .Append("-show_entries stream=duration ")

            // The payload: one row per video packet, as "packet,<pts_time>,<flags>". The field order
            // is important as the parser splits the row positionally, so pts_time must be named
            // before flags. A keyframe is a row whose flags field begins with K.
            //
            // Note this emits *every* packet, not just keyframes; roughly 30 rows a second, so a
            // feature-length file yields a couple of hundred thousand. Selecting the keyframes out
            // of them is the parser's job.
            .Append("-show_entries packet=pts_time,flags ")

            // Video only. Nothing in a row identifies which stream it came from, so an audio track's
            // packets would be indistinguishable from the video's and read as extra keyframes.
            .Append("-select_streams v ")

            // csv rather than json, because of the row count above.
            //
            // This relies on ffprobe's default of prefixing each row with its section name, which is
            // the only thing that distinguishes a "packet" row from the trailing "stream" and
            // "format" ones so the parser dispatches on it. Adding the usual =p=0 to strip that prefix
            // would leave the output unparseable.
            .Append("-of csv \"")
            .Append(FilePath)
            .Append('"');
    }
}
