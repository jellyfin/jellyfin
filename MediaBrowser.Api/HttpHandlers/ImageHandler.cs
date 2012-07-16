using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ImageHandler : Response
    {
        public ImageHandler(RequestContext ctx)
            : base(ctx)
        {
            Headers["Content-Encoding"] = "gzip";

            WriteStream = s =>
            {
                WriteReponse(s);
                s.Close();
            };
        }

        private string _ImagePath = string.Empty;
        private string ImagePath
        {
            get
            {
                if (string.IsNullOrEmpty(_ImagePath))
                {
                    _ImagePath = GetImagePath();
                }

                return _ImagePath;
            }
        }

        public override string ContentType
        {
            get
            {
                string extension = Path.GetExtension(ImagePath);

                if (extension.EndsWith("png", StringComparison.OrdinalIgnoreCase))
                {
                    return "image/png";
                }

                return "image/jpeg";
            }
        }

        public override TimeSpan CacheDuration
        {
            get
            {
                return TimeSpan.FromDays(365);
            }
        }

        public override DateTime? LastDateModified
        {
            get
            {
                try
                {
                    return File.GetLastWriteTime(ImagePath);
                }
                catch
                {
                    return null;
                }
            }
        }

        private int? Height
        {
            get
            {
                string val = QueryString["height"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        private int? Width
        {
            get
            {
                string val = QueryString["width"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        private int? MaxHeight
        {
            get
            {
                string val = QueryString["maxheight"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        private int? MaxWidth
        {
            get
            {
                string val = QueryString["maxwidth"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        private int? Quality
        {
            get
            {
                string val = QueryString["quality"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        private void WriteReponse(Stream stream)
        {
            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Compress, false))
            {
                ImageProcessor.ProcessImage(ImagePath, gzipStream, Width, Height, MaxWidth, MaxHeight, Quality);
            }
        }

        private string GetImagePath()
        {
            string path = QueryString["path"] ?? string.Empty;

            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

            string id = QueryString["id"];
            string personName = QueryString["personname"];
            string imageType = QueryString["type"] ?? string.Empty;
            string imageIndex = QueryString["index"];

            BaseItem item;

            if (!string.IsNullOrEmpty(personName))
            {
                item = Kernel.Instance.ItemController.GetPerson(personName);
            }
            else
            {
                item = ApiService.GetItemById(QueryString["id"]);
            }

            int index = string.IsNullOrEmpty(imageIndex) ? 0 : int.Parse(imageIndex);

            return GetImagePathFromTypes(item, imageType, index);
        }

        private string GetImagePathFromTypes(BaseItem item, string imageType, int imageIndex)
        {
            if (imageType.Equals("logo", StringComparison.OrdinalIgnoreCase))
            {
                return item.LogoImagePath;
            }
            else if (imageType.Equals("backdrop", StringComparison.OrdinalIgnoreCase))
            {
                return item.BackdropImagePaths.ElementAt(imageIndex);
            }
            else if (imageType.Equals("banner", StringComparison.OrdinalIgnoreCase))
            {
                return item.BannerImagePath;
            }
            else if (imageType.Equals("art", StringComparison.OrdinalIgnoreCase))
            {
                return item.ArtImagePath;
            }
            else if (imageType.Equals("thumbnail", StringComparison.OrdinalIgnoreCase))
            {
                return item.ThumbnailImagePath;
            }
            else
            {
                return item.PrimaryImagePath;
            }
        }
    }
}
