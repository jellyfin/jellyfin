using System;
using System.IO;

namespace MediaBrowser.Common.Net
{
    public static class MimeTypes
    {
        public static string JsonMimeType = "application/json";

        public static string GetMimeType(string path)
        {
            string ext = Path.GetExtension(path);

            // http://en.wikipedia.org/wiki/Internet_media_type
            // Add more as needed

            // Type video
            if (ext.EndsWith("mpg", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("mpeg", StringComparison.OrdinalIgnoreCase))
            {
                return "video/mpeg";
            }
            else if (ext.EndsWith("mp4", StringComparison.OrdinalIgnoreCase))
            {
                return "video/mp4";
            }
            else if (ext.EndsWith("ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/ogg";
            }
            else if (ext.EndsWith("mov", StringComparison.OrdinalIgnoreCase))
            {
                return "video/quicktime";
            }
            else if (ext.EndsWith("webm", StringComparison.OrdinalIgnoreCase))
            {
                return "video/webm";
            }
            else if (ext.EndsWith("mkv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-matroska";
            }
            else if (ext.EndsWith("wmv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-ms-wmv";
            }
            else if (ext.EndsWith("flv", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-flv";
            }
            else if (ext.EndsWith("avi", StringComparison.OrdinalIgnoreCase))
            {
                return "video/avi";
            }
            else if (ext.EndsWith("m4v", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-m4v";
            }
            else if (ext.EndsWith("asf", StringComparison.OrdinalIgnoreCase))
            {
                return "video/x-ms-asf";
            }
            else if (ext.EndsWith("3gp", StringComparison.OrdinalIgnoreCase))
            {
                return "video/3gpp";
            }
            else if (ext.EndsWith("3g2", StringComparison.OrdinalIgnoreCase))
            {
                return "video/3gpp2";
            }
            else if (ext.EndsWith("ts", StringComparison.OrdinalIgnoreCase))
            {
                return "video/mp2t";
            }

            // Type text
            else if (ext.EndsWith("css", StringComparison.OrdinalIgnoreCase))
            {
                return "text/css";
            }
            else if (ext.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
            {
                return "text/csv";
            }
            else if (ext.EndsWith("html", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("html", StringComparison.OrdinalIgnoreCase))
            {
                return "text/html";
            }
            else if (ext.EndsWith("txt", StringComparison.OrdinalIgnoreCase))
            {
                return "text/plain";
            }

            // Type image
            else if (ext.EndsWith("gif", StringComparison.OrdinalIgnoreCase))
            {
                return "image/gif";
            }
            else if (ext.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return "image/jpeg";
            }
            else if (ext.EndsWith("png", StringComparison.OrdinalIgnoreCase))
            {
                return "image/png";
            }
            else if (ext.EndsWith("ico", StringComparison.OrdinalIgnoreCase))
            {
                return "image/vnd.microsoft.icon";
            }

             // Type audio
            else if (ext.EndsWith("mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/mpeg";
            }
            else if (ext.EndsWith("m4a", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("aac", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/mp4";
            }
            else if (ext.EndsWith("webma", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/webm";
            }
            else if (ext.EndsWith("wav", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/wav";
            }
            else if (ext.EndsWith("wma", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/x-ms-wma";
            }
            else if (ext.EndsWith("flac", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/flac";
            }
            else if (ext.EndsWith("aac", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/x-aac";
            }
            else if (ext.EndsWith("ogg", StringComparison.OrdinalIgnoreCase) || ext.EndsWith("oga", StringComparison.OrdinalIgnoreCase))
            {
                return "audio/ogg";
            }

            // Playlists
            else if (ext.EndsWith("m3u8", StringComparison.OrdinalIgnoreCase))
            {
                return "application/x-mpegURL";
            }

            throw new InvalidOperationException("Argument not supported: " + path);
        }
    }
}
