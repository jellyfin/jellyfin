using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Publishes only complete, probed compatibility clips. A cancelled generation never becomes a cache entry.
/// </summary>
internal static class TranscodeTestClipCache
{
    /// <summary>
    /// Gets a clip from the cache, or generates, probes and publishes it.
    /// </summary>
    /// <typeparam name="T">The probe result.</typeparam>
    /// <param name="path">The cache entry.</param>
    /// <param name="generate">Generates the clip into temporary files it gets from the given factory; returns the
    /// file it made, or why it could not.</param>
    /// <param name="probe">Probes a clip; null when it is not a usable clip.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The probed clip, or null and why it could not be made.</returns>
    internal static async Task<(T? Value, string? Error)> GetAsync<T>(
        string path,
        Func<Func<string>, CancellationToken, Task<(string? Output, string? Error)>> generate,
        Func<string, CancellationToken, Task<T?>> probe,
        CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(path))
        {
            var cached = new FileInfo(path).Length > 0
                ? await probe(path, cancellationToken).ConfigureAwait(false)
                : null;
            if (cached is not null)
            {
                return (cached, null);
            }

            File.Delete(path);
        }

        // Keep the extension so ffmpeg can infer the container, and keep the files on the cache's filesystem so
        // publishing is one atomic rename. Every attempt gets its own name, so a late process cannot touch another's.
        var temporaries = new List<string>();
        string NewTemporary()
        {
            var temporary = Path.Combine(
                Path.GetDirectoryName(path)!,
                Path.GetFileNameWithoutExtension(path) + "-" + Guid.NewGuid().ToString("N") + ".partial" + Path.GetExtension(path));
            temporaries.Add(temporary);
            return temporary;
        }

        try
        {
            var (output, error) = await generate(NewTemporary, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (output is null)
            {
                return (null, error ?? "The clip was not generated.");
            }

            var value = File.Exists(output) && new FileInfo(output).Length > 0
                ? await probe(output, cancellationToken).ConfigureAwait(false)
                : null;
            if (value is null)
            {
                return (null, "The generated clip could not be probed as video.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(output, path, true);
            return (value, null);
        }
        finally
        {
            foreach (var temporary in temporaries)
            {
                File.Delete(temporary);
            }
        }
    }

    /// <summary>
    /// Generates a clip at full size and, when that ran but failed, once more at 640x360, which needs far less memory.
    /// No retry follows an attempt whose process never started in time: it may still start and write its file.
    /// </summary>
    /// <param name="newTemporary">Gives a fresh temporary file for each attempt.</param>
    /// <param name="attempt">Runs ffmpeg into the given file, small or not, and says whether it made the file and
    /// whether its process started.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file made, or null and why not.</returns>
    internal static async Task<(string? Output, string? Error)> GenerateAsync(
        Func<string> newTemporary,
        Func<string, bool, CancellationToken, Task<(bool Created, bool Started, string Error)>> attempt,
        CancellationToken cancellationToken)
    {
        var first = newTemporary();
        var (created, started, error) = await attempt(first, false, cancellationToken).ConfigureAwait(false);
        if (created)
        {
            return (first, null);
        }

        File.Delete(first);
        if (!started)
        {
            return (null, error);
        }

        var small = newTemporary();
        (created, _, error) = await attempt(small, true, cancellationToken).ConfigureAwait(false);
        return created ? (small, null) : (null, error);
    }
}
