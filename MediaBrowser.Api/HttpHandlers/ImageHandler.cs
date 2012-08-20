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
        private async Task<string> GetImagePath()
        {
            if (_ImagePath == null)
            {
                _ImagePath = await DiscoverImagePath();
            }

            return _ImagePath;
        }

        private async Task<string> DiscoverImagePath()
        {
            string personName = QueryString["personname"];

            if (!string.IsNullOrEmpty(personName))
            {
                return (await Kernel.Instance.ItemController.GetPerson(personName)).PrimaryImagePath;
            }

            string genreName = QueryString["genre"];

            if (!string.IsNullOrEmpty(genreName))
            {
                return (await Kernel.Instance.ItemController.GetGenre(genreName)).PrimaryImagePath;
            }

            string year = QueryString["year"];

            if (!string.IsNullOrEmpty(year))
            {
                return (await Kernel.Instance.ItemController.GetYear(int.Parse(year))).PrimaryImagePath;
            }

            string studio = QueryString["studio"];

            if (!string.IsNullOrEmpty(studio))
            {
                return (await Kernel.Instance.ItemController.GetStudio(studio)).PrimaryImagePath;
            }
            
            BaseItem item = ApiService.GetItemById(QueryString["id"]);

            string imageIndex = QueryString["index"];
            int index = string.IsNullOrEmpty(imageIndex) ? 0 : int.Parse(imageIndex);

            return GetImagePathFromTypes(item, ImageType, index);
        }

        private Stream _SourceStream = null;
        private async Task<Stream> GetSourceStream()
        {
            await EnsureSourceStream();
            return _SourceStream;
        }

        private bool _SourceStreamEnsured = false;
        private async Task EnsureSourceStream()
        {
            if (!_SourceStreamEnsured)
            {
                try
                {
                    _SourceStream = File.OpenRead(await GetImagePath());
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

        public async override Task<string> GetContentType()
        {
            await EnsureSourceStream();

            if (await GetSourceStream() == null)
            {
                return null;
            }

            return MimeTypes.GetMimeType(await GetImagePath());
        }

        public override TimeSpan CacheDuration
        {
            get
            {
                return TimeSpan.FromDays(365);
            }
        }

        protected async override Task<DateTime?> GetLastDateModified()
        {
            await EnsureSourceStream();

            if (await GetSourceStream() == null)
            {
                return null;
            }

            return File.GetLastWriteTime(await GetImagePath());
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

        protected override async Task WriteResponseToOutputStream(Stream stream)
        {
            ImageProcessor.ProcessImage(await GetSourceStream(), stream, Width, Height, MaxWidth, MaxHeight, Quality);
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
