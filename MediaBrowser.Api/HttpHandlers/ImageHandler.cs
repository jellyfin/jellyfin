using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.ComponentModel.Composition;
using System.IO;
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
                    _sourceEntity =
                        await Kernel.Instance.ItemController.GetPerson(QueryString["personname"]).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["genre"]))
                {
                    _sourceEntity =
                        await Kernel.Instance.ItemController.GetGenre(QueryString["genre"]).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["year"]))
                {
                    _sourceEntity =
                        await
                        Kernel.Instance.ItemController.GetYear(int.Parse(QueryString["year"])).ConfigureAwait(false);
                }

                else if (!string.IsNullOrEmpty(QueryString["studio"]))
                {
                    _sourceEntity =
                        await Kernel.Instance.ItemController.GetStudio(QueryString["studio"]).ConfigureAwait(false);
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

            return ImageProcessor.GetImagePath(entity, ImageType, ImageIndex);
        }

        protected async override Task<ResponseInfo> GetResponseInfo()
        {
            string path = await GetImagePath().ConfigureAwait(false);

            ResponseInfo info = new ResponseInfo
            {
                CacheDuration = TimeSpan.FromDays(365),
                ContentType = MimeTypes.GetMimeType(path)
            };

            DateTime? date = File.GetLastWriteTimeUtc(path);

            // If the file does not exist it will return jan 1, 1601
            // http://msdn.microsoft.com/en-us/library/system.io.file.getlastwritetimeutc.aspx
            if (date.Value.Year == 1601)
            {
                if (!File.Exists(path))
                {
                    info.StatusCode = 404;
                    date = null;
                }
            }

            info.DateLastModified = date;

            return info;
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
            var entity = await GetSourceEntity().ConfigureAwait(false);

            ImageProcessor.ProcessImage(entity, ImageType, ImageIndex, stream, Width, Height, MaxWidth, MaxHeight, Quality);
        }
    }
}
