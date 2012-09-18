using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
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

        private BaseEntity _sourceEntity;
        private async Task<BaseEntity> GetSourceEntity()
        {
            if (_sourceEntity == null)
            {
                if (!string.IsNullOrEmpty(QueryString["personname"]))
                {
                    _sourceEntity = await Kernel.Instance.ItemController.GetPerson(QueryString["personname"]).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["genre"]))
                {
                    _sourceEntity = await Kernel.Instance.ItemController.GetGenre(QueryString["genre"]).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["year"]))
                {
                    _sourceEntity = await Kernel.Instance.ItemController.GetYear(int.Parse(QueryString["year"])).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["studio"]))
                {
                    _sourceEntity = await Kernel.Instance.ItemController.GetStudio(QueryString["studio"]).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["userid"]))
                {
                    _sourceEntity = ApiService.GetUserById(QueryString["userid"], false);
                }

                else
                {
                    _sourceEntity = ApiService.GetItemById(QueryString["id"]);
                }
            }

            return _sourceEntity;
        }

        private async Task<string> DiscoverImagePath()
        {
            var entity = await GetSourceEntity().ConfigureAwait(false);

            var item = entity as BaseItem;

            if (item != null)
            {
                return GetImagePathFromTypes(item, ImageType, ImageIndex);
            }

            return entity.PrimaryImagePath;
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
            if (await GetSourceStream().ConfigureAwait(false) == null)
            {
                return null;
            }

            return File.GetLastWriteTimeUtc(await GetImagePath().ConfigureAwait(false));
        }

        private int ImageIndex
        {
            get
            {
                string val = QueryString["index"];

                if (string.IsNullOrEmpty(val))
                {
                    return 0;
                }

                return int.Parse(val);
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
                    return ImageType.Primary;
                }

                return (ImageType)Enum.Parse(typeof(ImageType), imageType, true);
            }
        }

        protected override async Task WriteResponseToOutputStream(Stream stream)
        {
            Stream sourceStream = await GetSourceStream().ConfigureAwait(false);

            var entity = await GetSourceEntity().ConfigureAwait(false);

            ImageProcessor.ProcessImage(sourceStream, stream, Width, Height, MaxWidth, MaxHeight, Quality, entity, ImageType, ImageIndex);
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
