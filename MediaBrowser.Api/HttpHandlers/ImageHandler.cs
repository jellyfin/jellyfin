using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    public class ImageHandler : BaseHandler
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("image", request);
        }
        
        private string _imagePath;
        private async Task<string> GetImagePath()
        {
            _imagePath = _imagePath ?? await DiscoverImagePath();

            return _imagePath;
        }

        private async Task<string> DiscoverImagePath()
        {
            string personName = QueryString["personname"];

            if (!string.IsNullOrEmpty(personName))
            {
                return (await Kernel.Instance.ItemController.GetPerson(personName).ConfigureAwait(false)).PrimaryImagePath;
            }

            string genreName = QueryString["genre"];

            if (!string.IsNullOrEmpty(genreName))
            {
                return (await Kernel.Instance.ItemController.GetGenre(genreName).ConfigureAwait(false)).PrimaryImagePath;
            }

            string year = QueryString["year"];

            if (!string.IsNullOrEmpty(year))
            {
                return (await Kernel.Instance.ItemController.GetYear(int.Parse(year)).ConfigureAwait(false)).PrimaryImagePath;
            }

            string studio = QueryString["studio"];

            if (!string.IsNullOrEmpty(studio))
            {
                return (await Kernel.Instance.ItemController.GetStudio(studio).ConfigureAwait(false)).PrimaryImagePath;
            }

            string userId = QueryString["userid"];

            if (!string.IsNullOrEmpty(userId))
            {
                return ApiService.GetUserById(userId, false).PrimaryImagePath;
            }

            BaseItem item = ApiService.GetItemById(QueryString["id"]);

            string imageIndex = QueryString["index"];
            int index = string.IsNullOrEmpty(imageIndex) ? 0 : int.Parse(imageIndex);

            return GetImagePathFromTypes(item, ImageType, index);
        }

        private Stream _sourceStream;
        private async Task<Stream> GetSourceStream()
        {
            await EnsureSourceStream().ConfigureAwait(false);
            return _sourceStream;
        }

        private bool _sourceStreamEnsured;
        private async Task EnsureSourceStream()
        {
            if (!_sourceStreamEnsured)
            {
                try
                {
                    _sourceStream = File.OpenRead(await GetImagePath().ConfigureAwait(false));
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
                    _sourceStreamEnsured = true;
                }
            }
        }

        public async override Task<string> GetContentType()
        {
            await EnsureSourceStream().ConfigureAwait(false);

            if (await GetSourceStream().ConfigureAwait(false) == null)
            {
                return null;
            }

            return MimeTypes.GetMimeType(await GetImagePath().ConfigureAwait(false));
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
            await EnsureSourceStream().ConfigureAwait(false);

            if (await GetSourceStream().ConfigureAwait(false) == null)
            {
                return null;
            }

            return File.GetLastWriteTimeUtc(await GetImagePath().ConfigureAwait(false));
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
            ImageProcessor.ProcessImage(await GetSourceStream().ConfigureAwait(false), stream, Width, Height, MaxWidth, MaxHeight, Quality);
        }

        private string GetImagePathFromTypes(BaseItem item, ImageType imageType, int imageIndex)
        {
            if (imageType == ImageType.Logo)
            {
                return item.LogoImagePath;
            }
            if (imageType == ImageType.Backdrop)
            {
                return item.BackdropImagePaths.ElementAt(imageIndex);
            }
            if (imageType == ImageType.Banner)
            {
                return item.BannerImagePath;
            }
            if (imageType == ImageType.Art)
            {
                return item.ArtImagePath;
            }
            if (imageType == ImageType.Thumbnail)
            {
                return item.ThumbnailImagePath;
            }

            return item.PrimaryImagePath;
        }
    }
}
