using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;
using MediaBrowser.Model.MediaInfo;

namespace Emby.Photos
{
    public class PhotoProvider : ICustomMetadataProvider<Photo>, IForcedProvider, IHasItemChangeMonitor
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private IImageProcessor _imageProcessor;

        public PhotoProvider(ILogger logger, IFileSystem fileSystem, IImageProcessor imageProcessor)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _imageProcessor = imageProcessor;
        }

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            if (item.IsFileProtocol)
            {
                var file = directoryService.GetFile(item.Path);
                if (file != null && file.LastWriteTimeUtc != item.DateModified)
                {
                    return true;
                }
            }

            return false;
        }

        // These are causing taglib to hang
        private string[] _includextensions = new string[] { ".jpg", ".jpeg", ".png", ".tiff", ".cr2" };

        public Task<ItemUpdateType> FetchAsync(Photo item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            item.SetImagePath(ImageType.Primary, item.Path);

            // Examples: https://github.com/mono/taglib-sharp/blob/a5f6949a53d09ce63ee7495580d6802921a21f14/tests/fixtures/TagLib.Tests.Images/NullOrientationTest.cs
            if (_includextensions.Contains(Path.GetExtension(item.Path) ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    using (var file = TagLib.File.Create(item.Path))
                    {
                        var image = file as TagLib.Image.File;

                        var tag = file.GetTag(TagTypes.TiffIFD) as IFDTag;

                        if (tag != null)
                        {
                            var structure = tag.Structure;

                            if (structure != null)
                            {
                                var exif = structure.GetEntry(0, (ushort)IFDEntryTag.ExifIFD) as SubIFDEntry;

                                if (exif != null)
                                {
                                    var exifStructure = exif.Structure;

                                    if (exifStructure != null)
                                    {
                                        var entry = exifStructure.GetEntry(0, (ushort)ExifEntryTag.ApertureValue) as RationalIFDEntry;

                                        if (entry != null)
                                        {
                                            double val = entry.Value.Numerator;
                                            val /= entry.Value.Denominator;
                                            item.Aperture = val;
                                        }

                                        entry = exifStructure.GetEntry(0, (ushort)ExifEntryTag.ShutterSpeedValue) as RationalIFDEntry;

                                        if (entry != null)
                                        {
                                            double val = entry.Value.Numerator;
                                            val /= entry.Value.Denominator;
                                            item.ShutterSpeed = val;
                                        }
                                    }
                                }
                            }
                        }

                        if (image != null)
                        {
                            item.CameraMake = image.ImageTag.Make;
                            item.CameraModel = image.ImageTag.Model;

                            item.Width = image.Properties.PhotoWidth;
                            item.Height = image.Properties.PhotoHeight;

                            var rating = image.ImageTag.Rating;
                            if (rating.HasValue)
                            {
                                item.CommunityRating = rating;
                            }
                            else
                            {
                                item.CommunityRating = null;
                            }

                            item.Overview = image.ImageTag.Comment;

                            if (!string.IsNullOrWhiteSpace(image.ImageTag.Title))
                            {
                                if (!item.LockedFields.Contains(MetadataFields.Name))
                                {
                                    item.Name = image.ImageTag.Title;
                                }
                            }

                            var dateTaken = image.ImageTag.DateTime;
                            if (dateTaken.HasValue)
                            {
                                item.DateCreated = dateTaken.Value;
                                item.PremiereDate = dateTaken.Value;
                                item.ProductionYear = dateTaken.Value.Year;
                            }

                            item.Genres = image.ImageTag.Genres;
                            item.Tags = image.ImageTag.Keywords;
                            item.Software = image.ImageTag.Software;

                            if (image.ImageTag.Orientation == TagLib.Image.ImageOrientation.None)
                            {
                                item.Orientation = null;
                            }
                            else
                            {
                                MediaBrowser.Model.Drawing.ImageOrientation orientation;
                                if (Enum.TryParse(image.ImageTag.Orientation.ToString(), true, out orientation))
                                {
                                    item.Orientation = orientation;
                                }
                            }

                            item.ExposureTime = image.ImageTag.ExposureTime;
                            item.FocalLength = image.ImageTag.FocalLength;

                            item.Latitude = image.ImageTag.Latitude;
                            item.Longitude = image.ImageTag.Longitude;
                            item.Altitude = image.ImageTag.Altitude;

                            if (image.ImageTag.ISOSpeedRatings.HasValue)
                            {
                                item.IsoSpeedRating = Convert.ToInt32(image.ImageTag.ISOSpeedRatings.Value);
                            }
                            else
                            {
                                item.IsoSpeedRating = null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Image Provider - Error reading image tag for {0}", item.Path);
                }
            }

            if (item.Width <= 0 || item.Height <= 0)
            {
                var img = item.GetImageInfo(ImageType.Primary, 0);

                try
                {
                    var size = _imageProcessor.GetImageSize(item, img, false, false);

                    if (size.Width > 0 && size.Height > 0)
                    {
                        item.Width = Convert.ToInt32(size.Width);
                        item.Height = Convert.ToInt32(size.Height);
                    }
                }
                catch (ArgumentException)
                {
                    // format not supported
                }
            }

            const ItemUpdateType result = ItemUpdateType.ImageUpdate | ItemUpdateType.MetadataImport;
            return Task.FromResult(result);
        }

        public string Name
        {
            get { return "Embedded Information"; }
        }
    }
}
