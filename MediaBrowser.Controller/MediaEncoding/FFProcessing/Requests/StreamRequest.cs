using System;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Delivers media to a client, either as one continuous response or as HLS segments.
/// <para>
/// Unlike the other requests this carries its arguments already assembled. Building them needs the
/// whole of <c>EncodingHelper</c> — hardware pipelines, filter graphs, bitrate and profile
/// negotiation — which is a far larger body of logic than any single request should absorb.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// Three builders are the only ones that produce the string:
/// <c>DynamicHlsController.GetCommandLineArguments</c> for segmented delivery, and
/// <c>EncodingHelper</c>'s progressive video and audio builders, reached through
/// <c>FileStreamResponseHelpers</c>. All three emit the same shape — input options, <c>-i</c>,
/// stream selection and mapping, codec and filter arguments, muxer arguments, and finally
/// <c>"&lt;output&gt;"</c>. The output target lives inside the string; the runner never supplies one.
/// </para>
/// <para>
/// What they must <em>not</em> emit is any flag the runner already owns: <c>-hide_banner</c>, the
/// <c>-loglevel</c> derived from the server's logger, <c>-stats</c>, and the <c>-y</c> / <c>-n</c>
/// overwrite flag. Those are written first and this string is appended after them, so a repeat
/// here is the last word on the command line.
/// </para>
/// </remarks>
public sealed record StreamRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.Stream;

    /// <summary>Gets how the output reaches the client.</summary>
    public required FFDelivery Delivery { get; init; }

    /// <summary>
    /// Gets how much re-encoding the delivery actually involves. Not used to build the arguments —
    /// they already reflect it — but it decides how the run is supervised and reported, since a
    /// remux and a full transcode cost wildly different amounts.
    /// </summary>
    public required StreamMode Mode { get; init; }

    /// <summary>
    /// Gets the assembled arguments, including the input and output terms.
    /// </summary>
    public required string Arguments { get; init; }

    /// <summary>
    /// Resolves the action's policy and then pins <c>Overwrite</c> on. Delivery routinely writes over
    /// output a previous run left behind — a stale segment, or a progressive file from an abandoned
    /// session.
    /// </summary>
    /// <returns>The effective policy, always with <c>Overwrite</c> set.</returns>
    public override FFActionPolicy ResolvePolicy() => base.ResolvePolicy() with { Overwrite = true };

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Verbatim, and appended after the runner's own global flags. Nothing is validated or
        // rewritten here: the caller owns the whole command line from the input options onwards,
        // which is the point of this request type.
        builder.Append(Arguments);
    }
}
