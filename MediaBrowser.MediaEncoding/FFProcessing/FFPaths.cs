using System.Text.RegularExpressions;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;

namespace MediaBrowser.MediaEncoding.FFProcessing;

/// <inheritdoc cref="IFFPaths"/>
public sealed partial class FFPaths : IFFPaths
{
    // Held as one object and swapped atomically. Written during startup and read by every action
    // thereafter, so publishing the two separately would let a reader pick up a new encoder path
    // beside a stale prober path.
    private volatile Resolved _resolved = new(string.Empty, string.Empty);

    /// <inheritdoc />
    public string EncoderPath => _resolved.Encoder;

    /// <inheritdoc />
    public string ProbePath => _resolved.Prober;

    /// <inheritdoc />
    public void SetEncoderPath(string encoderPath)
    {
        var encoder = encoderPath ?? string.Empty;

        // Replace only the file name, so a versioned or prefixed binary such as
        // jellyfin-ffmpeg.exe still resolves to ffprobe.exe beside it.
        var prober = encoder.Length == 0
            ? string.Empty
            : ProberNameRegex().Replace(encoder, "ffprobe$1");

        _resolved = new Resolved(encoder, prober);
    }

    [GeneratedRegex(@"[^\/\\]+?(\.[^\/\\\n.]+)?$")]
    private static partial Regex ProberNameRegex();

    private sealed record Resolved(string Encoder, string Prober);
}
