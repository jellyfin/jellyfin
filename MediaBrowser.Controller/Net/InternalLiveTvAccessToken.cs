using System.Text.RegularExpressions;

namespace MediaBrowser.Controller.Net;

/// <summary>
/// Prevents internal Live TV capabilities from appearing in encoder logs.
/// </summary>
public static partial class InternalLiveTvAccessToken
{
    /// <summary>
    /// Redacts internal capability query values while preserving other log content.
    /// </summary>
    /// <param name="value">An encoder command line or process output.</param>
    /// <returns>The value with internal capabilities redacted.</returns>
    public static string Redact(string value)
        => TokenPattern().Replace(value, "$1[redacted]");

    [GeneratedRegex("([?&]InternalLiveTvToken=)[^&\\s\"']*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
