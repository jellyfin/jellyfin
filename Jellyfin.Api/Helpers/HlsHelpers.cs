using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// The hls helpers.
/// </summary>
public static class HlsHelpers
{
    /// <summary>
    /// Waits for a minimum number of segments to be available.
    /// </summary>
    /// <param name="playlist">The playlist string.</param>
    /// <param name="segmentCount">The segment count.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> indicating the waiting process.</returns>
    public static async Task WaitForMinimumSegmentCount(string playlist, int? segmentCount, ILogger logger, CancellationToken cancellationToken)
    {
        logger.LogDebug("Waiting for {0} segments in {1}", segmentCount, playlist);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                var fileStream = new FileStream(
                    playlist,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    IODefaults.FileStreamBufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using (fileStream.ConfigureAwait(false))
                {
                    using var reader = new StreamReader(fileStream);
                    var count = 0;

                    string? line;
                    while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
                    {
                        if (line.Contains("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                        {
                            count++;
                            if (count >= segmentCount)
                            {
                                logger.LogDebug("Finished waiting for {0} segments in {1}", segmentCount, playlist);
                                return;
                            }
                        }
                    }
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // May get an error if the file is locked
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the #EXT-X-MAP string.
    /// </summary>
    /// <param name="outputPath">The output path of the file.</param>
    /// <param name="state">The <see cref="StreamState"/>.</param>
    /// <param name="isOsDepends">Get a normal string or depends on OS.</param>
    /// <returns>The string text of #EXT-X-MAP.</returns>
    public static string GetFmp4InitFileName(string outputPath, StreamState state, bool isOsDepends)
    {
        var directory = Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath));
        var outputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputPath);
        var outputPrefix = Path.Combine(directory, outputFileNameWithoutExtension);
        var outputExtension = EncodingHelper.GetSegmentFileExtension(state.Request.SegmentContainer);

        // on Linux/Unix
        // #EXT-X-MAP:URI="prefix-1.mp4"
        var fmp4InitFileName = outputFileNameWithoutExtension + "-1" + outputExtension;
        if (!isOsDepends)
        {
            return fmp4InitFileName;
        }

        if (OperatingSystem.IsWindows())
        {
            // on Windows
            // #EXT-X-MAP:URI="X:\transcodes\prefix-1.mp4"
            fmp4InitFileName = outputPrefix + "-1" + outputExtension;
        }

        return fmp4InitFileName;
    }

    /// <summary>
    /// Gets the hls playlist text.
    /// </summary>
    /// <param name="path">The path to the playlist file.</param>
    /// <param name="state">The <see cref="StreamState"/>.</param>
    /// <returns>The playlist text as a string.</returns>
    public static string GetLivePlaylistText(string path, StreamState state)
    {
        var text = File.ReadAllText(path);

        var segmentFormat = EncodingHelper.GetSegmentFileExtension(state.Request.SegmentContainer).TrimStart('.');
        if (string.Equals(segmentFormat, "mp4", StringComparison.OrdinalIgnoreCase))
        {
            var fmp4InitFileName = GetFmp4InitFileName(path, state, true);
            var baseUrlParam = string.Format(
                CultureInfo.InvariantCulture,
                "hls/{0}/",
                Path.GetFileNameWithoutExtension(path));
            var newFmp4InitFileName = baseUrlParam + GetFmp4InitFileName(path, state, false);

            // Replace fMP4 init file URI.
            text = text.Replace(fmp4InitFileName, newFmp4InitFileName, StringComparison.InvariantCulture);
        }

        return text;
    }

    /// <summary>
    /// Propagates normal request authentication only to segments belonging to this playlist.
    /// </summary>
    /// <param name="text">The generated playlist.</param>
    /// <param name="playlistPath">The verified output playlist path.</param>
    /// <param name="token">The authenticated request token.</param>
    /// <returns>The playlist with authenticated local segment and initialization URLs.</returns>
    public static string AuthenticatePlaylistSegments(string text, string playlistPath, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return text;
        }

        var lines = text.ReplaceLineEndings("\n").Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (line.StartsWith("#EXT-X-MAP:", StringComparison.Ordinal))
            {
                const string UriAttribute = "URI=\"";
                var start = line.IndexOf(UriAttribute, StringComparison.Ordinal);
                if (start >= 0)
                {
                    start += UriAttribute.Length;
                    var end = line.IndexOf('"', start);
                    if (end > start)
                    {
                        lines[index] = string.Concat(line.AsSpan(0, start), AuthenticateSegmentUri(line[start..end], playlistPath, token), line.AsSpan(end));
                    }
                }
            }
            else if (line.Length > 0 && line[0] != '#')
            {
                lines[index] = AuthenticateSegmentUri(line, playlistPath, token);
            }
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Preserves request options while propagating the verified token and resolved encoding alias.
    /// </summary>
    /// <param name="queryString">The original playlist query.</param>
    /// <param name="playSessionId">The resolved encoding alias.</param>
    /// <param name="token">The authenticated request token.</param>
    /// <returns>The query to append to local playlist and segment URLs.</returns>
    public static string GetAuthenticatedPlaylistQuery(string queryString, string? playSessionId, string? token)
    {
        var query = QueryHelpers.ParseQuery(queryString);
        if (!string.IsNullOrWhiteSpace(playSessionId))
        {
            query["PlaySessionId"] = playSessionId;
        }

        if (!string.IsNullOrEmpty(token))
        {
            query["ApiKey"] = token;
        }

        return QueryHelpers.AddQueryString(string.Empty, query);
    }

    private static string AuthenticateSegmentUri(string uri, string playlistPath, string token)
    {
        var queryStart = uri.IndexOf('?', StringComparison.Ordinal);
        var path = queryStart < 0 ? uri : uri[..queryStart];
        if (path.Contains("..", StringComparison.Ordinal) || uri.Contains('#', StringComparison.Ordinal))
        {
            return uri;
        }

        var filename = Path.GetFileName(path);
        var prefix = Path.GetFileNameWithoutExtension(playlistPath);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (Path.IsPathFullyQualified(path))
        {
            if (!string.Equals(Path.GetDirectoryName(path), Path.GetDirectoryName(playlistPath), comparison))
            {
                return uri;
            }

            path = filename;
        }
        else if (!string.Equals(path, filename, comparison)
                 && !string.Equals(path, $"hls/{prefix}/{filename}", comparison))
        {
            return uri;
        }

        var stem = Path.GetFileNameWithoutExtension(filename);
        if (!stem.StartsWith(prefix, comparison)
            || !int.TryParse(stem.AsSpan(prefix.Length), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var index)
            || index < -1)
        {
            return uri;
        }

        var query = QueryHelpers.ParseQuery(queryStart < 0 ? string.Empty : uri[queryStart..]);
        query["ApiKey"] = token;
        return QueryHelpers.AddQueryString(path, query);
    }
}
