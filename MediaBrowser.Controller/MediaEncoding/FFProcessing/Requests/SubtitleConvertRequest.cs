using System;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Rewrites a standalone subtitle file as SubRip, optionally reinterpreting its character
/// encoding. Unlike <see cref="SubtitleExtractRequest"/> there is no container to select from, so
/// the whole file is converted.
/// </summary>
public sealed record SubtitleConvertRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.ExtractSubtitle;

    /// <summary>Gets the file to read.</summary>
    public required string Input { get; init; }

    /// <summary>Gets the file to write.</summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Gets the character encoding to read the source as. Empty lets FFmpeg decide, which is
    /// correct when the source is already UTF-8 or the encoding could not be detected. It is also
    /// ignored where FFmpeg refuses it — see <see cref="SpecifiesCharacterEncoding"/>.
    /// </summary>
    public string SourceCharacterEncoding { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether <c>-sub_charenc</c> is actually emitted.
    /// <para>
    /// FFmpeg recodes UTF-16 SAMI on its own and then rejects being told the encoding as well,
    /// failing with "do not specify a character encoding" and "Unable to recode subtitle event".
    /// </para>
    /// </summary>
    public bool SpecifiesCharacterEncoding
        => SourceCharacterEncoding.Length > 0 && !(IsSami && IsUtf16);

    private bool IsSami
        => Input.EndsWith(".smi", StringComparison.OrdinalIgnoreCase)
           || Input.EndsWith(".sami", StringComparison.OrdinalIgnoreCase);

    private bool IsUtf16
        => SourceCharacterEncoding.Equals("UTF-16BE", StringComparison.OrdinalIgnoreCase)
           || SourceCharacterEncoding.Equals("UTF-16LE", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (SpecifiesCharacterEncoding)
        {
            // before -i, so the demuxer decodes the bytes as this charset rather than guessing
            builder.Append("-sub_charenc ").Append(SourceCharacterEncoding).Append(' ');
        }

        builder.Append("-i \"").Append(Input).Append('"');

        // -c:s srt re-encodes the cues as SubRip rather than copying them, which is what actually
        // performs the conversion. There is no -map: the file holds one subtitle stream and
        // nothing else, so FFmpeg's default selection already picks it.
        builder.Append(" -c:s srt \"").Append(OutputPath).Append('"');
    }
}
