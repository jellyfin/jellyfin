using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ImageHandler : BaseHandler
    {
        private string _ImagePath = null;
        private string ImagePath
        {
            get
            {
                if (_ImagePath == null)
                {
                    _ImagePath = GetImagePath();
                }

                return _ImagePath;
            }
        }

        private Stream _SourceStream = null;
        private Stream SourceStream
        {
            get
            {
                EnsureSourceStream();

                return _SourceStream;
            }
        }


        private bool _SourceStreamEnsured = false;
        private void EnsureSourceStream()
        {
            if (!_SourceStreamEnsured)
            {
                try
                {
                    _SourceStream = File.OpenRead(ImagePath);
                }
                catch (FileNotFoundException ex)
                {
                    StatusCode = 404;
                    Logger.LogException(ex);
                }
                catch (DirectoryNotFoundException ex)
                {
                    StatusCode = 404;
                    Logger.LogException(ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    StatusCode = 403;
                    Logger.LogException(ex);
                }
                finally
                {
                    _SourceStreamEnsured = true;
                }
            }
        }
        
        public override string ContentType
        {
            get
            {
                EnsureSourceStream();

                if (SourceStream == null)
                {
                    return null;
                }
                
                return MimeTypes.GetMimeType(ImagePath);
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
            EnsureSourceStream();

            if (SourceStream == null)
            {
                return null;
            }

            return File.GetLastWriteTime(ImagePath);
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
                    return ImageType.Primary;
                }

                return (ImageType)Enum.Parse(typeof(ImageType), imageType, true);
            }
        }

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            return Task.Run(() =>
            {
                ImageProcessor.ProcessImage(SourceStream, stream, Width, Height, MaxWidth, MaxHeight, Quality);
            });
        }

        private string GetImagePath()
        {
            string path = QueryString["path"] ?? string.Empty;

            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

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
