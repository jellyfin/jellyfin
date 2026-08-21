using System;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Interrogates the binary's own capabilities at startup: version, codec and filter lists, and
/// feature tests. These arguments are literal interrogations of FFmpeg rather than media
/// operations, so arguments are passed verbatim.
/// </summary>
public sealed record CapabilitiesRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.Capabilities;

    /// <summary>Gets the interrogation arguments.</summary>
    public required string Arguments { get; init; }

    /// <summary>Gets a value indicating whether this interrogates ffprobe rather than ffmpeg.</summary>
    public bool ProbeOnly { get; init; }

    /// <inheritdoc />
    public override FFActionPolicy ResolvePolicy() => base.ResolvePolicy() with { ProbeOnly = ProbeOnly };

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Append(Arguments);
    }
}
