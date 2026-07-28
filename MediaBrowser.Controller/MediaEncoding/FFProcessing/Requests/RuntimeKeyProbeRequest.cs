using System;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Tests whether the encoder responds to runtime keys. The runner writes the query key to stdin;
/// the answer is whatever the encoder prints to stderr in response.
/// </summary>
public sealed record RuntimeKeyProbeRequest : FFRequest
{
    /// <summary>The key written to stdin. FFmpeg's "list runtime keys" query.</summary>
    public const string QueryKey = "?";

    /// <inheritdoc />
    public override FFAction Action => FFAction.ProbeRuntimeKeys;

    /// <summary>Gets the interrogation arguments.</summary>
    public required string Arguments { get; init; }

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Append(Arguments);
    }
}
