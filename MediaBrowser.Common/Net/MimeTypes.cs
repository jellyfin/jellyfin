using System;
using System.IO;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class MimeTypes
    /// </summary>
    public static class MimeTypes
    {
        /// <summary>
        /// The json MIME type
        /// </summary>
        public static string JsonMimeType = "application/json";

        /// <summary>
        /// Gets the type of the MIME.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        /// <exception cref="System.InvalidOperationException">Argument not supported:  + path</exception>
        public static string GetMimeType(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            
            var ext = Path.GetExtension(path) ?? string.Empty;

            // http://en.wikipedia.org/wiki/Internet_media_type
            // Add more as needed

            // Type video
            if (ext.Equals(".mpg", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("mpeg", StringComparison.OrdinalIgnoreCase))
            {
                return "video/mpeg";
            }
            if (ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                return "video/mp4";
            }
            if (ext.Equals(".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/ogg";
            }
            if (ext.Equals(".mov", StringComparison.OrdinalIgnoreCase))
            {
                return "video/quicktime";
            }
            if (ext.Equals(".webm", StringComparison.OrdinalIgnoreCase))
            {
                return "video/webm";
            }
            if (ext.Equals(".mkv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-matroska";
            }
            if (ext.Equals(".wmv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-ms-wmv";
            }
            if (ext.Equals(".flv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-flv";
            }
            if (ext.Equals(".avi", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-msvideo";
            }
            if (ext.Equals(".m4v", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-m4v";
            }
            if (ext.EndsWith("asf", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-ms-asf";
            }
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

            // Type image
            if (ext.Equals(".gif", StringComparison.OrdinalIgnoreCase))
            {
                return "image/gif";
            }
            if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".tbn", StringComparison.OrdinalIgnoreCase))
            {
                return "image/jpeg";
            }
            if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                return "image/png";
            }
            if (ext.Equals(".ico", StringComparison.OrdinalIgnoreCase))
            {
                return "image/vnd.microsoft.icon";
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

            if (ext.Equals(".bif", StringComparison.OrdinalIgnoreCase))
            {
                return "application/octet-stream";
            }

            throw new ArgumentException("Argument not supported: " + path);
        }
    }
}
