using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Dumps embedded attachments such as subtitle fonts to disk, either by stream index or all at
/// once under their embedded filenames.
/// </summary>
public sealed record AttachmentRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.ExtractAttachment;

    /// <summary>Gets the input target, already formed and quoted by the caller.</summary>
    public required string Input { get; init; }

    /// <summary>
    /// Gets the attachments to extract by stream index. When empty, every attachment is dumped
    /// into <see cref="FFRequest.WorkingDirectory"/> using its embedded filename.
    /// </summary>
    public IReadOnlyList<AttachmentTarget> Targets { get; init; } = [];

    /// <summary>Gets a value indicating whether the input is a concat playlist.</summary>
    public bool IsConcatPlaylist { get; init; }

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (Targets.Count == 0)
        {
            // dump every attachment to the working directory using its embedded filename
            builder.Append("-dump_attachment:t \"\" ");
        }
        else
        {
            foreach (var target in Targets)
            {
                builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "-dump_attachment:{0} \"{1}\" ",
                    target.StreamIndex,
                    target.OutputPath);
            }
        }

        if (IsConcatPlaylist)
        {
            // use the concat demuxer to read the playlist emitting the attachments in the order listed
            // and allow absolute paths in the playlist to allow dumping to arbitrary locations.
            builder.Append("-f concat -safe 0 ");
        }

        // Map the attachments themselves so the null muxer always has a stream to write, whatever
        // the source contains. The ? makes the map optional; without it a source carrying no
        // attachments fails with "Stream map '' matches no streams".
        // -t 0 stops after zero media frames, so this only dumps and exits.
        builder.Append("-i ").Append(Input).Append(" -map 0:t? -t 0 -f null null");
    }
}
