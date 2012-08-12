using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ImageHandler : BaseHandler
    {
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

        protected override DateTime? GetLastDateModified()
        {
            try
            {
                return File.GetLastWriteTime(ImagePath);
            }
            catch
            {
                return base.GetLastDateModified();
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

        private ImageType ImageType
        {
            get
            {
                string imageType = QueryString["type"];

                if (string.IsNullOrEmpty(imageType))
                {
                    return Model.Entities.ImageType.Primary;
                }

                return (ImageType)Enum.Parse(typeof(ImageType), imageType, true);
            }
        }

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            return Task.Run(() =>
            {
                ImageProcessor.ProcessImage(ImagePath, stream, Width, Height, MaxWidth, MaxHeight, Quality);
            });
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

            if (!string.IsNullOrEmpty(personName))
            {
                return Kernel.Instance.ItemController.GetPerson(personName).PrimaryImagePath;
            }

            BaseItem item = ApiService.GetItemById(QueryString["id"]);

            string imageIndex = QueryString["index"];
            int index = string.IsNullOrEmpty(imageIndex) ? 0 : int.Parse(imageIndex);

            return GetImagePathFromTypes(item, ImageType, index);
        }

        private string GetImagePathFromTypes(BaseItem item, ImageType imageType, int imageIndex)
        {
            if (imageType == ImageType.Logo)
            {
                return item.LogoImagePath;
            }
            else if (imageType == ImageType.Backdrop)
            {
                return item.BackdropImagePaths.ElementAt(imageIndex);
            }
            else if (imageType == ImageType.Banner)
            {
                return item.BannerImagePath;
            }
            else if (imageType == ImageType.Art)
            {
                return item.ArtImagePath;
            }
            else if (imageType == ImageType.Thumbnail)
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
