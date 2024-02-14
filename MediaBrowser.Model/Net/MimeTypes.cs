#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
        private static readonly HashSet<string> _videoFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
        };

        /// <summary>
        /// Used for extensions not in <see cref="Model.MimeTypes"/> or to override them.
        /// </summary>
        private static readonly Dictionary<string, string> _mimeTypeLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Type application
            { ".azw3", "application/vnd.amazon.ebook" },
            { ".cb7", "application/x-cb7" },
            { ".cba", "application/x-cba" },
            { ".cbr", "application/vnd.comicbook-rar" },
            { ".cbt", "application/x-cbt" },
            { ".cbz", "application/vnd.comicbook+zip" },

            // Type image
            { ".tbn", "image/jpeg" },

            // Type text
            { ".ass", "text/x-ssa" },
            { ".ssa", "text/x-ssa" },
            { ".edl", "text/plain" },
            { ".html", "text/html; charset=UTF-8" },
            { ".htm", "text/html; charset=UTF-8" },

            // Type video
            { ".mpegts", "video/mp2t" },

            // Type audio
            { ".aac", "audio/aac" },
            { ".ac3", "audio/ac3" },
            { ".ape", "audio/x-ape" },
            { ".dsf", "audio/dsf" },
            { ".dsp", "audio/dsp" },
            { ".flac", "audio/flac" },
            { ".m4b", "audio/mp4" },
            { ".mp3", "audio/mpeg" },
            { ".vorbis", "audio/vorbis" },
            { ".webma", "audio/webm" },
            { ".wv", "audio/x-wavpack" },
            { ".xsp", "audio/xsp" },
        };

        private static readonly Dictionary<string, string> _extensionLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Type application
            { "application/vnd.comicbook-rar", ".cbr" },
            { "application/vnd.comicbook+zip", ".cbz" },
            { "application/x-cb7", ".cb7" },
            { "application/x-cba", ".cba" },
            { "application/x-cbr", ".cbr" },
            { "application/x-cbt", ".cbt" },
            { "application/x-cbz", ".cbz" },
            { "application/x-javascript", ".js" },
            { "application/xml", ".xml" },
            { "application/x-mpegURL", ".m3u8" },

            // Type audio
            { "audio/aac", ".aac" },
            { "audio/ac3", ".ac3" },
            { "audio/dsf", ".dsf" },
            { "audio/dsp", ".dsp" },
            { "audio/flac", ".flac" },
            { "audio/m4b", ".m4b" },
            { "audio/vorbis", ".vorbis" },
            { "audio/x-ape", ".ape" },
            { "audio/xsp", ".xsp" },
            { "audio/x-wavpack", ".wv" },

            // Type image
            { "image/jpeg", ".jpg" },
            { "image/tiff", ".tiff" },
            { "image/x-png", ".png" },
            { "image/x-icon", ".ico" },

            // Type text
            { "text/plain", ".txt" },
            { "text/rtf", ".rtf" },
            { "text/x-ssa", ".ssa" },

            // Type video
            { "video/vnd.mpeg.dash.mpd", ".mpd" },
            { "video/x-matroska", ".mkv" },
        };

        public static string GetMimeType(string path) => GetMimeType(path, "application/octet-stream");

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
