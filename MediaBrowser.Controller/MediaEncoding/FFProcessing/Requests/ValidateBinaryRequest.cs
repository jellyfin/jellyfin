using System;
using System.Text;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;

/// <summary>
/// Asks a candidate executable to report its version, so the answer can decide whether that path
/// is accepted. The version banner is written to standard output.
/// </summary>
public sealed record ValidateBinaryRequest : FFRequest
{
    /// <inheritdoc />
    public override FFAction Action => FFAction.ValidateBinary;

    /// <summary>Gets the executable to run. Not yet accepted, hence not resolvable from IFFPaths.</summary>
    public required string BinaryPath { get; init; }

    /// <inheritdoc />
    public override string BinaryPathOverride => BinaryPath;

    /// <inheritdoc />
    public override void BuildArguments(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Append("-version");
    }
}
