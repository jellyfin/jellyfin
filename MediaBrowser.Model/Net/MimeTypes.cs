#pragma warning disable CS1591

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Extensions;
using Microsoft.AspNetCore.StaticFiles;

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
            ".vob",
            ".webm",
            ".wmv",
            ".wtv",
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Used for extensions not in <see cref="FileExtensionContentTypeProvider"/> or to override them.
        /// </summary>
        private static readonly FrozenDictionary<string, string> _mimeTypeLookup = new KeyValuePair<string, string>[]
        {
            // Type application
            new(".7z", "application/x-7z-compressed"),
            new(".azw", "application/vnd.amazon.ebook"),
            new(".azw3", "application/vnd.amazon.ebook"),
            new(".cb7", "application/x-cb7"),
            new(".cba", "application/x-cba"),
            new(".cbr", "application/vnd.comicbook-rar"),
            new(".cbt", "application/x-cbt"),
            new(".cbz", "application/vnd.comicbook+zip"),
            new(".dll", "application/octet-stream"),
            new(".epub", "application/epub+zip"),
            new(".mobi", "application/x-mobipocket-ebook"),
            new(".opf", "application/oebps-package+xml"),
            new(".rar", "application/vnd.rar"),
            new(".srt", "application/x-subrip"),
            new(".ttml", "application/ttml+xml"),
            new(".xml", "application/xml"),
            new(".zip", "application/zip"),

            // Type image
            new(".apng", "image/apng"),
            new(".ico", "image/vnd.microsoft.icon"),
            new(".tbn", "image/jpeg"),

            // Type text
            new(".ass", "text/x-ssa"),
            new(".ssa", "text/x-ssa"),
            new(".edl", "text/plain"),
            new(".html", "text/html; charset=UTF-8"),
            new(".htm", "text/html; charset=UTF-8"),
            new(".log", "text/plain"),
            new(".vtt", "text/vtt"),

            // Type video
            new(".m4v", "video/x-m4v"),
            new(".mkv", "video/x-matroska"),
            new(".mpegts", "video/mp2t"),
            new(".ts", "video/mp2t"),

            // Type audio
            new(".aac", "audio/aac"),
            new(".ac3", "audio/ac3"),
            new(".ape", "audio/x-ape"),
            new(".dsf", "audio/dsf"),
            new(".dsp", "audio/dsp"),
            new(".flac", "audio/flac"),
            new(".m4b", "audio/mp4"),
            new(".mid", "audio/midi"),
            new(".midi", "audio/midi"),
            new(".mp3", "audio/mpeg"),
            new(".ogg", "audio/ogg"),
            new(".opus", "audio/ogg"),
            new(".vorbis", "audio/vorbis"),
            new(".webma", "audio/webm"),
            new(".wv", "audio/x-wavpack"),
            new(".xsp", "audio/xsp"),

            // Type font
            new(".ttf", "font/ttf"),
            new(".woff", "font/woff"),
        }.ToFrozenDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        private static readonly FrozenDictionary<string, string> _extensionLookup = new KeyValuePair<string, string>[]
        {
            // Type application
            new("application/vnd.comicbook-rar", ".cbr"),
            new("application/vnd.comicbook+zip", ".cbz"),
            new("application/epub+zip", ".epub"),
            new("application/oebps-package+xml", ".opf"),
            new("application/ttml+xml", ".ttml"),
            new("application/vnd.amazon.ebook", ".azw"),
            new("application/vnd.rar", ".rar"),
            new("application/x-7z-compressed", ".7z"),
            new("application/x-cb7", ".cb7"),
            new("application/x-cba", ".cba"),
            new("application/x-cbr", ".cbr"),
            new("application/x-cbt", ".cbt"),
            new("application/x-cbz", ".cbz"),
            new("application/x-javascript", ".js"),
            new("application/x-mobipocket-ebook", ".mobi"),
            new("application/xml", ".xml"),
            new("application/x-mpegURL", ".m3u8"),
            new("application/x-subrip", ".srt"),
            new("application/zip", ".zip"),

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

            // Type font
            new("font/ttf", ".ttf"),
            new("font/woff", ".woff"),

            // Type image
            new("image/apng", ".apng"),
            new("image/jpeg", ".jpg"),
            new("image/jpg", ".jpg"),
            new("image/tiff", ".tiff"),
            new("image/vnd.microsoft.icon", ".ico"),
            new("image/x-png", ".png"),
            new("image/x-icon", ".ico"),

            // Type text
            new("text/plain", ".txt"),
            new("text/rtf", ".rtf"),
            new("text/vtt", ".vtt"),
            new("text/x-ssa", ".ssa"),

            // Type video
            new("video/vnd.mpeg.dash.mpd", ".mpd"),
            new("video/mp2t", ".ts"),
            new("video/mp4", ".mp4"),
            new("video/ogg", ".ogv"),
            new("video/x-m4v", ".m4v"),
            new("video/x-matroska", ".mkv"),
        }.ToFrozenDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

        private static readonly FrozenDictionary<string, string> _contentTypeExtensionLookup = _contentTypeProvider.Mappings
            .GroupBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(group => group.Key, group => group.First().Key, StringComparer.OrdinalIgnoreCase);

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

            if (_contentTypeProvider.TryGetContentType(filename, out var mimeType))
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

            return _contentTypeExtensionLookup.GetValueOrDefault(mimeType);
        }

        public static bool IsImage(ReadOnlySpan<char> mimeType)
            => mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
