using System;
using System.IO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Net.Handlers;

namespace MediaBrowser.Api.HttpHandlers
{
    class MediaHandler : BaseHandler
    {
        private string _MediaPath = string.Empty;
        private string MediaPath
        {
            get
            {
                if (string.IsNullOrEmpty(_MediaPath))
                {
                    _MediaPath = GetMediaPath();
                }

                return _MediaPath;
            }
        }

        private string GetMediaPath()
        {
            string path = QueryString["path"] ?? string.Empty;

            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

            BaseItem item = ApiService.GetItemById(QueryString["id"]);

            return item.Path;
        }

        public override bool GzipResponse
        {
            get
            {
                return false;
            }
        }

        public override string ContentType
        {
            get
            {
                // http://www.codingcereal.com/2011/10/an-array-of-45-video-mime-types/

                string extension = Path.GetExtension(MediaPath);

                if (extension.EndsWith("mkv", StringComparison.OrdinalIgnoreCase))
                {
                    return "video/x-matroska";
                }
                else if (extension.EndsWith("avi", StringComparison.OrdinalIgnoreCase))
                {
                    return "video/avi";
                }
                else if (extension.EndsWith("wmv", StringComparison.OrdinalIgnoreCase))
                {
                    return "video/wmv";
                }
                else if (extension.EndsWith("m4v", StringComparison.OrdinalIgnoreCase))
                {
                    return "video/m4v";
                }
                else if (extension.EndsWith("flv", StringComparison.OrdinalIgnoreCase))
                {
                    return "video/flv";
                }
                else if (extension.EndsWith("mov", StringComparison.OrdinalIgnoreCase))
                {
                    return "video/quicktime";
                }
                else if (extension.EndsWith("mp4", StringComparison.OrdinalIgnoreCase))
                {
                    return "video/mp4";
                }

                return "video/x-matroska";
            }
        }

        protected override void WriteResponseToOutputStream(Stream stream)
        {
            try
            {
                using (Stream input = File.OpenRead(MediaPath))
                {
                    input.CopyTo(stream);
                }
            }
            catch
            {
            }
        }
    }
}
