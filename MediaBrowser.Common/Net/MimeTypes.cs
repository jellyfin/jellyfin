using System;
using System.IO;

namespace MediaBrowser.Common.Net
{
    public static class MimeTypes
    {
        public static string JsonMimeType = "application/json";

        public static string GetMimeType(string path)
        {
            var ext = Path.GetExtension(path);

            // http://en.wikipedia.org/wiki/Internet_media_type
            // Add more as needed

            // Type video
            if (ext.EndsWith("mpg", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("mpeg", StringComparison.OrdinalIgnoreCase))
            {
                return "video/mpeg";
            }
            if (ext.EndsWith("mp4", StringComparison.OrdinalIgnoreCase))
            {
                return "video/mp4";
            }
            if (ext.EndsWith("ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/ogg";
            }
            if (ext.EndsWith("mov", StringComparison.OrdinalIgnoreCase))
            {
                return "video/quicktime";
            }
            if (ext.EndsWith("webm", StringComparison.OrdinalIgnoreCase))
            {
                return "video/webm";
            }
            if (ext.EndsWith("mkv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-matroska";
            }
            if (ext.EndsWith("wmv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-ms-wmv";
            }
            if (ext.EndsWith("flv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-flv";
            }
            if (ext.EndsWith("avi", StringComparison.OrdinalIgnoreCase))
            {
                return "video/avi";
            }
            if (ext.EndsWith("m4v", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-m4v";
            }
            if (ext.EndsWith("asf", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-ms-asf";
            }
            if (ext.EndsWith("3gp", StringComparison.OrdinalIgnoreCase))
            {
                return "video/3gpp";
            }
            if (ext.EndsWith("3g2", StringComparison.OrdinalIgnoreCase))
            {
                return "video/3gpp2";
            }
            if (ext.EndsWith("ts", StringComparison.OrdinalIgnoreCase))
            {
                return "video/mp2t";
            }

            // Type text
            if (ext.EndsWith("css", StringComparison.OrdinalIgnoreCase))
            {
                return "text/css";
            }
            if (ext.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
            {
                return "text/csv";
            }
            if (ext.EndsWith("html", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("html", StringComparison.OrdinalIgnoreCase))
            {
                return "text/html";
            }
            if (ext.EndsWith("txt", StringComparison.OrdinalIgnoreCase))
            {
                return "text/plain";
            }

            // Type image
            if (ext.EndsWith("gif", StringComparison.OrdinalIgnoreCase))
            {
                return "image/gif";
            }
            if (ext.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return "image/jpeg";
            }
            if (ext.EndsWith("png", StringComparison.OrdinalIgnoreCase))
            {
                return "image/png";
            }
            if (ext.EndsWith("ico", StringComparison.OrdinalIgnoreCase))
            {
                return "image/vnd.microsoft.icon";
            }

             // Type audio
            if (ext.EndsWith("mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/mpeg";
            }
            if (ext.EndsWith("m4a", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("aac", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/mp4";
            }
            if (ext.EndsWith("webma", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/webm";
            }
            if (ext.EndsWith("wav", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/wav";
            }
            if (ext.EndsWith("wma", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/x-ms-wma";
            }
            if (ext.EndsWith("flac", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/flac";
            }
            if (ext.EndsWith("aac", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/x-aac";
            }
            if (ext.EndsWith("ogg", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("oga", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/ogg";
            }

            // Playlists
            if (ext.EndsWith("m3u8", StringComparison.OrdinalIgnoreCase))
            {
                return "application/x-mpegURL";
            }

            // Misc
            if (ext.EndsWith("dll", StringComparison.OrdinalIgnoreCase))
            {
                return "application/x-msdownload";
            }

            throw new InvalidOperationException("Argument not supported: " + path);
        }
    }
}
