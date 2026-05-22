using System;
using System.IO;

namespace Jellyfin.Extensions;

/// <summary>
/// Helpers for safely composing filesystem paths from untrusted input.
/// </summary>
/// <remarks>
/// <see cref="Path.Combine(string, string)"/> has two issues that matter in
/// any code that joins a trusted directory with an externally-supplied name:
/// it neither normalises <c>..</c> nor rejects a rooted second argument
/// (a rooted second arg silently discards the first). Use the helpers below
/// any time the name comes from media metadata, request input, archive
/// entries, or any other channel that can be influenced by a third party.
/// </remarks>
public static class PathHelper
{
    /// <summary>
    /// Reduces a possibly-untrusted file name to a safe leaf-only name with no
    /// directory components.
    /// </summary>
    /// <param name="fileName">The candidate file name.</param>
    /// <returns>
    /// The leaf component of <paramref name="fileName"/>, or <c>null</c> if
    /// the input has no usable leaf (empty, <c>.</c>, or <c>..</c>).
    /// </returns>
    public static string? GetSafeLeafFileName(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var leaf = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(leaf) || leaf == "." || leaf == "..")
        {
            return null;
        }

        return leaf;
    }

    /// <summary>
    /// Returns whether <paramref name="candidate"/> resolves to a path that
    /// equals or is contained inside <paramref name="root"/>.
    /// </summary>
    /// <param name="root">The directory the candidate must remain inside.</param>
    /// <param name="candidate">The candidate absolute or relative path.</param>
    /// <returns><c>true</c> if the candidate is inside or equal to root; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Both arguments are resolved via <see cref="Path.GetFullPath(string)"/>
    /// so <c>..</c> segments are collapsed before the comparison. The root is
    /// compared with a trailing directory separator to prevent prefix
    /// collisions (e.g. <c>/var/data</c> must not be accepted as a parent of
    /// <c>/var/dataset</c>).
    /// </remarks>
    public static bool IsContainedIn(string root, string candidate)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentException.ThrowIfNullOrEmpty(candidate);

        var fullRoot = Path.GetFullPath(root);
        var fullCandidate = Path.GetFullPath(candidate);

        if (string.Equals(fullCandidate, fullRoot, StringComparison.Ordinal))
        {
            return true;
        }

        var rootWithSep = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;

        return fullCandidate.StartsWith(rootWithSep, StringComparison.Ordinal);
    }
}
