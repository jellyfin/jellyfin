using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Photos
{
    public class PhotoProvider : ICustomMetadataProvider<Photo>, IHasChangeMonitor
    {
        private readonly ILogger _logger;
        private readonly IImageProcessor _imageProcessor;

        public PhotoProvider(ILogger logger, IImageProcessor imageProcessor)
        {
            _logger = logger;
            _imageProcessor = imageProcessor;
        }

        public Task<ItemUpdateType> FetchAsync(Photo item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            item.SetImagePath(ImageType.Primary, item.Path);
            item.SetImagePath(ImageType.Backdrop, item.Path);

            if (item.Path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || item.Path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using (var reader = new ExifReader(item.Path))
                    {
                        double aperture = 0;
                        double shutterSpeed = 0;

                        DateTime dateTaken;

                        string manufacturer;
                        string model;

                        reader.GetTagValue(ExifTags.FNumber, out aperture);
                        reader.GetTagValue(ExifTags.ExposureTime, out shutterSpeed);
                        reader.GetTagValue(ExifTags.DateTimeOriginal, out dateTaken);

                        reader.GetTagValue(ExifTags.Make, out manufacturer);
                        reader.GetTagValue(ExifTags.Model, out model);

                        if (dateTaken > DateTime.MinValue)
                        {
                            item.DateCreated = dateTaken;
                            item.PremiereDate = dateTaken;
                            item.ProductionYear = dateTaken.Year;
                        }

                        var cameraModel = manufacturer ?? string.Empty;
                        cameraModel += " ";
                        cameraModel += model ?? string.Empty;

                        var size = _imageProcessor.GetImageSize(item.Path);
                        var xResolution = size.Width;
                        var yResolution = size.Height;

                        item.Overview = "Taken " + dateTaken.ToString("F") + "\n" +
                                        (!string.IsNullOrWhiteSpace(cameraModel) ? "With a " + cameraModel : "") +
                                        (aperture > 0 && shutterSpeed > 0 ? " at f" + aperture.ToString(CultureInfo.InvariantCulture) + " and " + PhotoHelper.Dec2Frac(shutterSpeed) + "s" : "") + "\n"
                                        + (xResolution > 0 ? "\n<br/>Resolution: " + xResolution + "x" + yResolution : "");
                    }

                }
                catch (Exception e)
                {
                    _logger.ErrorException("Image Provider - Error reading image tag for {0}", e, item.Path);
                }
            }

            //// Get additional tags from xmp
            //try
            //{
            //    using (var fs = new FileStream(item.Path, FileMode.Open, FileAccess.Read))
            //    {
            //        var bf = BitmapFrame.Create(fs);

            //        if (bf != null)
            //        {
            //            var data = (BitmapMetadata)bf.Metadata;
            //            if (data != null)
            //            {

            //                DateTime dateTaken;
            //                var cameraModel = "";

            //                DateTime.TryParse(data.DateTaken, out dateTaken);
            //                if (dateTaken > DateTime.MinValue) item.DateCreated = dateTaken;
            //                cameraModel = data.CameraModel;

            //                item.PremiereDate = dateTaken;
            //                item.ProductionYear = dateTaken.Year;
            //                item.Overview = "Taken " + dateTaken.ToString("F") + "\n" +
            //                                (cameraModel != "" ? "With a " + cameraModel : "") +
            //                                (aperture > 0 && shutterSpeed > 0 ? " at f" + aperture.ToString(CultureInfo.InvariantCulture) + " and " + PhotoHelper.Dec2Frac(shutterSpeed) + "s" : "") + "\n"
            //                                + (bf.Width > 0 ? "\n<br/>Resolution: " + (int)bf.Width + "x" + (int)bf.Height : "");

            //                var photo = item as Photo;
            //                if (data.Keywords != null) item.Genres = photo.Tags = new List<string>(data.Keywords);
            //                item.Name = !string.IsNullOrWhiteSpace(data.Title) ? data.Title : item.Name;
            //                item.CommunityRating = data.Rating;
            //                if (!string.IsNullOrWhiteSpace(data.Subject)) photo.AddTagline(data.Subject);
            //            }
            //        }

            //    }
            //}
            //catch (NotSupportedException)
            //{
            //    // No problem - move on
            //}
            //catch (Exception e)
            //{
            //    _logger.ErrorException("Error trying to read extended data from {0}", e, item.Path);
            //}

            const ItemUpdateType result = ItemUpdateType.ImageUpdate | ItemUpdateType.MetadataImport;
            return Task.FromResult(result);
        }

        public string Name
        {
            get { return "Embedded Information"; }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            return item.DateModified > date;
        }
    }
}
