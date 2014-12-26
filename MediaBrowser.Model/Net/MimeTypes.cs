using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Class MimeTypes
    /// </summary>
    public static class MimeTypes
    {
        /// <summary>
        /// Any extension in this list is considered a video file - can be added to at runtime for extensibility
        /// </summary>
        private static readonly List<string> VideoFileExtensions = new List<string>
            {
                ".mkv",
                ".m2t",
                ".m2ts",
                ".img",
                ".iso",
                ".mk3d",
                ".ts",
                ".rmvb",
                ".mov",
                ".avi",
                ".mpg",
                ".mpeg",
                ".wmv",
                ".mp4",
                ".divx",
                ".dvr-ms",
                ".wtv",
                ".ogm",
                ".ogv",
                ".asf",
                ".m4v",
                ".flv",
                ".f4v",
                ".3gp",
                ".webm",
                ".mts",
                ".m2v",
                ".rec"
        };

        private static readonly Dictionary<string, string> VideoFileExtensionsDictionary = VideoFileExtensions.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        // http://en.wikipedia.org/wiki/Internet_media_type
        // Add more as needed

        private static readonly Dictionary<string, string> MimeTypeLookup =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".tbn", "image/jpeg"},
                {".png", "image/png"},
                {".gif", "image/gif"},
                {".webp", "image/webp"},
                {".ico", "image/vnd.microsoft.icon"},
                {".mpg", "video/mpeg"},
                {".mpeg", "video/mpeg"},
                {".ogv", "video/ogg"},
                {".mov", "video/quicktime"},
                {".webm", "video/webm"},
                {".mkv", "video/x-matroska"},
                {".wmv", "video/x-ms-wmv"},
                {".flv", "video/x-flv"},
                {".avi", "video/x-msvideo"},
                {".asf", "video/x-ms-asf"},
                {".m4v", "video/x-m4v"}
            };
        
        private static readonly Dictionary<string, string> ExtensionLookup =
           MimeTypeLookup
           .GroupBy(i => i.Value)
           .ToDictionary(x => x.Key, x => x.First().Key, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the type of the MIME.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">path</exception>
        /// <exception cref="InvalidOperationException">Argument not supported:  + path</exception>
        public static string GetMimeType(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var ext = Path.GetExtension(path) ?? string.Empty;

            string result;
            if (MimeTypeLookup.TryGetValue(ext, out result))
            {
                return result;
            }

            // Type video
            if (ext.Equals(".3gp", StringComparison.OrdinalIgnoreCase))
            {
                return "video/3gpp";
            }
            if (ext.Equals(".3g2", StringComparison.OrdinalIgnoreCase))
            {
                return "video/3gpp2";
            }
            if (ext.Equals(".ts", StringComparison.OrdinalIgnoreCase))
            {
                return "video/mp2t";
            }
            if (ext.Equals(".mpd", StringComparison.OrdinalIgnoreCase))
            {
                return "video/vnd.mpeg.dash.mpd";
            }

            // Catch-all for all video types that don't require specific mime types
            if (VideoFileExtensionsDictionary.ContainsKey(ext))
            {
                return "video/" + ext.TrimStart('.').ToLower();
            }

            // Type text
            if (ext.Equals(".css", StringComparison.OrdinalIgnoreCase))
            {
                return "text/css";
            }
            if (ext.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return "text/csv";
            }
            if (ext.Equals(".html", StringComparison.OrdinalIgnoreCase) || ext.Equals(".htm", StringComparison.OrdinalIgnoreCase))
            {
                return "text/html; charset=UTF-8";
            }
            if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return "text/plain";
            }
            if (ext.Equals(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return "application/xml";
            }

            // Type document
            if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return "application/pdf";
            }
            if (ext.Equals(".mobi", StringComparison.OrdinalIgnoreCase))
            {
                return "application/x-mobipocket-ebook";
            }
            if (ext.Equals(".epub", StringComparison.OrdinalIgnoreCase))
            {
                return "application/epub+zip";
            }
            if (ext.Equals(".cbz", StringComparison.OrdinalIgnoreCase) || ext.Equals(".cbr", StringComparison.OrdinalIgnoreCase))
            {
                return "application/x-cdisplay";
            }

            // Type audio
            if (ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/mpeg";
            }
            if (ext.Equals(".m4a", StringComparison.OrdinalIgnoreCase) || ext.Equals(".aac", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/mp4";
            }
            if (ext.Equals(".webma", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/webm";
            }
            if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/wav";
            }
            if (ext.Equals(".wma", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/x-ms-wma";
            }
            if (ext.Equals(".flac", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/flac";
            }
            if (ext.Equals(".aac", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/x-aac";
            }
            if (ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".oga", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/ogg";
            }

            // Playlists
            if (ext.Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
            {
                return "application/x-mpegURL";
            }

            // Misc
            if (ext.Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return "application/octet-stream";
            }

            // Web
            if (ext.Equals(".js", StringComparison.OrdinalIgnoreCase))
            {
                return "application/x-javascript";
            }
            if (ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                return "application/json";
            }
            if (ext.Equals(".map", StringComparison.OrdinalIgnoreCase))
            {
                return "application/x-javascript";
            }

            if (ext.Equals(".woff", StringComparison.OrdinalIgnoreCase))
            {
                return "font/woff";
            }

            if (ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase))
            {
                return "font/ttf";
            }
            if (ext.Equals(".eot", StringComparison.OrdinalIgnoreCase))
            {
                return "application/vnd.ms-fontobject";
            }
            if (ext.Equals(".svg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".svgz", StringComparison.OrdinalIgnoreCase))
            {
                return "image/svg+xml";
            }

            if (ext.Equals(".srt", StringComparison.OrdinalIgnoreCase))
            {
                return "text/plain";
            }

            if (ext.Equals(".vtt", StringComparison.OrdinalIgnoreCase))
            {
                return "text/vtt";
            }

            if (ext.Equals(".ttml", StringComparison.OrdinalIgnoreCase))
            {
                return "application/ttml+xml";
            }

            if (ext.Equals(".bif", StringComparison.OrdinalIgnoreCase))
            {
                return "application/octet-stream";
            }

            throw new ArgumentException("Argument not supported: " + path);
        }

        public static string ToExtension(string mimeType)
        {
            return ExtensionLookup[mimeType];
        }
    }
}
