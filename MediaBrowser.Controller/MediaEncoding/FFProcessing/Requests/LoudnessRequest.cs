using System;
using System.Globalization;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Measures EBU R128 loudness. The measurement is reported on stderr rather than written to a
/// file, so the caller reads it out of <see cref="FFResult.Stderr"/>.
/// </summary>
public sealed record LoudnessRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.MeasureLoudness;

    /// <summary>Gets the path to measure, or to a concat playlist listing several.</summary>
    public required string Path { get; init; }

    /// <summary>Gets a value indicating whether <see cref="Path"/> is a concat playlist.</summary>
    public bool IsConcatPlaylist { get; init; }

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (IsConcatPlaylist)
        {
            // Read the playlist as one continuous program so an album measures as a whole rather
            // than track by track. -safe 0 permits the absolute paths the playlist is written with.
            builder.Append("-f concat -safe 0 ");
        }

        // ebur128 reports through the log rather than the output stream, so the run is uses
        // FFActionPolicy.StderrIsPayload. That is what ensures -loglevel is at least info and keeps
        // the whole stderr instead of a trailing window. Do not assume the flag is decorative: measured
        // against jellyfin-ffmpeg 7.1.4, the "Summary:" block is emitted at info and verbose and at
        // nothing below them, so at the default server log level of warning this run would exit 0
        // having printed no measurement at all, and the caller's regex would match nothing.
        //
        // framelog=verbose additionally logs each frame's loudness. The caller only reads the
        // summary, but the per-frame lines are what let a truncated or stalled measurement be told
        // apart from one that simply found nothing.
        //
        // -f null discards the decoded audio; the analysis is the entire point of the run.
        builder.AppendFormat(CultureInfo.InvariantCulture, "-i \"{0}\" ", Path)
            .Append("-af ebur128=framelog=verbose -f null -");
    }
}
