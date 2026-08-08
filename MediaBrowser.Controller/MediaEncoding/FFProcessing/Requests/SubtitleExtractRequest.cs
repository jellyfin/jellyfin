using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Writes one or more embedded subtitle streams out of a container as sidecar files. A single run
/// serves every target, so a container with many subtitle tracks is demuxed once rather than once
/// per track.
/// </summary>
public sealed record SubtitleExtractRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.ExtractSubtitle;

    /// <summary>Gets the input target, already formed and quoted by the caller.</summary>
    public required string Input { get; init; }

    /// <summary>Gets the streams to write. Each becomes its own output file.</summary>
    public required IReadOnlyList<SubtitleTarget> Targets { get; init; }

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Append("-i ").Append(Input);

        foreach (var target in Targets)
        {
            // -an -vn: this output carries the one subtitle stream and nothing else
            builder.Append(" -map 0:")
                .Append(target.StreamIndex.ToString(CultureInfo.InvariantCulture))
                .Append(" -an -vn -c:s ")
                .Append(target.CopyStream ? "copy" : "srt");

            if (target.IsVobSub)
            {
                // FFmpeg has no .idx/.sub muxer, so VobSub has to be wrapped in Matroska
                builder.Append(" -f matroska");
            }

            // write through rather than buffering, so a reader can open the file before the run ends
            builder.Append(" -flush_packets 1");

            builder.Append(" \"").Append(target.OutputPath).Append('"');
        }
    }
}
