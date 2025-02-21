#pragma warning disable CS1591

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Extensions;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Class MimeTypes.
    /// </summary>
    ///
    /// <remarks>
    /// For more information on MIME types:
    /// <list type="bullet">
    ///     <item>http://en.wikipedia.org/wiki/Internet_media_type</item>
    ///     <item>https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Common_types</item>
    ///     <item>http://www.iana.org/assignments/media-types/media-types.xhtml</item>
    /// </list>
    /// </remarks>
    public static class MimeTypes
    {
        /// <summary>
        /// Any extension in this list is considered a video file.
        /// </summary>
        private static readonly FrozenSet<string> _videoFileExtensions = new[]
        {
            ".3gp",
            ".asf",
            ".avi",
            ".divx",
            ".dvr-ms",
            ".f4v",
            ".flv",
            ".img",
            ".iso",
            ".m2t",
            ".m2ts",
            ".m2v",
            ".m4v",
            ".mk3d",
            ".mkv",
            ".mov",
            ".mp4",
            ".mpg",
            ".mpeg",
            ".mts",
            ".ogg",
            ".ogm",
            ".ogv",
            ".rec",
            ".ts",
            ".rmvb",
            ".webm",
            ".wmv",
            ".wtv",
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Used for extensions not in <see cref="Model.MimeTypes"/> or to override them.
        /// </summary>
        private static readonly FrozenDictionary<string, string> _mimeTypeLookup = new KeyValuePair<string, string>[]
        {
            // Type application
            new(".azw3", "application/vnd.amazon.ebook"),
            new(".cb7", "application/x-cb7"),
            new(".cba", "application/x-cba"),
            new(".cbr", "application/vnd.comicbook-rar"),
            new(".cbt", "application/x-cbt"),
            new(".cbz", "application/vnd.comicbook+zip"),

            // Type image
            new(".tbn", "image/jpeg"),

            // Type text
            new(".ass", "text/x-ssa"),
            new(".ssa", "text/x-ssa"),
            new(".edl", "text/plain"),
            new(".html", "text/html; charset=UTF-8"),
            new(".htm", "text/html; charset=UTF-8"),

            // Type video
            new(".mpegts", "video/mp2t"),

            // Type audio
            new(".aac", "audio/aac"),
            new(".ac3", "audio/ac3"),
            new(".ape", "audio/x-ape"),
            new(".dsf", "audio/dsf"),
            new(".dsp", "audio/dsp"),
            new(".flac", "audio/flac"),
            new(".m4b", "audio/mp4"),
            new(".mp3", "audio/mpeg"),
            new(".vorbis", "audio/vorbis"),
            new(".webma", "audio/webm"),
            new(".wv", "audio/x-wavpack"),
            new(".xsp", "audio/xsp"),
        }.ToFrozenDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        private static readonly FrozenDictionary<string, string> _extensionLookup = new KeyValuePair<string, string>[]
        {
            // Type application
            new("application/vnd.comicbook-rar", ".cbr"),
            new("application/vnd.comicbook+zip", ".cbz"),
            new("application/x-cb7", ".cb7"),
            new("application/x-cba", ".cba"),
            new("application/x-cbr", ".cbr"),
            new("application/x-cbt", ".cbt"),
            new("application/x-cbz", ".cbz"),
            new("application/x-javascript", ".js"),
            new("application/xml", ".xml"),
            new("application/x-mpegURL", ".m3u8"),

            // Type audio
            new("audio/aac", ".aac"),
            new("audio/ac3", ".ac3"),
            new("audio/dsf", ".dsf"),
            new("audio/dsp", ".dsp"),
            new("audio/flac", ".flac"),
            new("audio/m4b", ".m4b"),
            new("audio/vorbis", ".vorbis"),
            new("audio/x-ape", ".ape"),
            new("audio/xsp", ".xsp"),
            new("audio/x-aac", ".aac"),
            new("audio/x-wavpack", ".wv"),

            // Type image
            new("image/jpeg", ".jpg"),
            new("image/tiff", ".tiff"),
            new("image/x-png", ".png"),
            new("image/x-icon", ".ico"),

            // Type text
            new("text/plain", ".txt"),
            new("text/rtf", ".rtf"),
            new("text/x-ssa", ".ssa"),

            // Type video
            new("video/vnd.mpeg.dash.mpd", ".mpd"),
            new("video/x-matroska", ".mkv"),
        }.ToFrozenDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        public static string GetMimeType(string path) => GetMimeType(path, MediaTypeNames.Application.Octet);

        /// <summary>
        /// Gets the type of the MIME.
        /// </summary>
        /// <param name="filename">The filename to find the MIME type of.</param>
        /// <param name="defaultValue">The default value to return if no fitting MIME type is found.</param>
        /// <returns>The correct MIME type for the given filename, or <paramref name="defaultValue"/> if it wasn't found.</returns>
        [return: NotNullIfNotNull("defaultValue")]
        public static string? GetMimeType(string filename, string? defaultValue = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(filename);

            var ext = Path.GetExtension(filename);

            if (_mimeTypeLookup.TryGetValue(ext, out string? result))
            {
                return result;
            }

            if (Model.MimeTypes.TryGetMimeType(filename, out var mimeType))
            {
                return mimeType;
            }

            // Catch-all for all video types that don't require specific mime types
            if (_videoFileExtensions.Contains(ext))
            {
                return string.Concat("video/", ext.AsSpan(1));
            }

            return defaultValue;
        }

        public static string? ToExtension(string mimeType)
        {
            ArgumentException.ThrowIfNullOrEmpty(mimeType);

            // handle text/html; charset=UTF-8
            mimeType = mimeType.AsSpan().LeftPart(';').ToString();

            if (_extensionLookup.TryGetValue(mimeType, out string? result))
            {
                return result;
            }

            var extension = Model.MimeTypes.GetMimeTypeExtensions(mimeType).FirstOrDefault();
            return string.IsNullOrEmpty(extension) ? null : "." + extension;
        }

        public static bool IsImage(ReadOnlySpan<char> mimeType)
            => mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
