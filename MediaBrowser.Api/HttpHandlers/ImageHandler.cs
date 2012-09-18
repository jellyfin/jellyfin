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

        public override async Task<string> GetContentType()
        {
            if (Kernel.Instance.ImageProcessors.Any(i => i.RequiresTransparency))
            {
                return MimeTypes.GetMimeType(".png");
            }

            return MimeTypes.GetMimeType(await GetImagePath().ConfigureAwait(false));
        }

        public override TimeSpan CacheDuration
        {
            get { return TimeSpan.FromDays(365); }
        }

        protected override async Task<DateTime?> GetLastDateModified()
        {
            string path = await GetImagePath().ConfigureAwait(false);

            DateTime date = File.GetLastWriteTimeUtc(path);

            // If the file does not exist it will return jan 1, 1601
            // http://msdn.microsoft.com/en-us/library/system.io.file.getlastwritetimeutc.aspx
            if (date.Year == 1601)
            {
                if (!File.Exists(path))
                {
                    StatusCode = 404;
                    return null;
                }
            }

            return await GetMostRecentDateModified(date);
        }

        private async Task<DateTime> GetMostRecentDateModified(DateTime imageFileLastDateModified)
        {
            var date = imageFileLastDateModified;

            var entity = await GetSourceEntity().ConfigureAwait(false);
            
            foreach (var processor in Kernel.Instance.ImageProcessors)
            {
                if (processor.IsConfiguredToProcess(entity, ImageType, ImageIndex))
                {
                    if (processor.ProcessingConfigurationDateLastModifiedUtc > date)
                    {
                        date = processor.ProcessingConfigurationDateLastModifiedUtc;
                    }
                }
            }

            return date;
        }

        protected override async Task<string> GetETag()
        {
            string tag = string.Empty;

            var entity = await GetSourceEntity().ConfigureAwait(false);

            foreach (var processor in Kernel.Instance.ImageProcessors)
            {
                if (processor.IsConfiguredToProcess(entity, ImageType, ImageIndex))
                {
                    tag += processor.ProcessingConfigurationDateLastModifiedUtc.Ticks.ToString();
                }
            }

            return tag;
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
