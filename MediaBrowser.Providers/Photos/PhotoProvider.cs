using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;

namespace MediaBrowser.Providers.Photos
{
    public class PhotoProvider : ICustomMetadataProvider<Photo>, IHasItemChangeMonitor, IForcedProvider
    {
        private readonly ILogger _logger;

        public PhotoProvider(ILogger logger)
        {
            _logger = logger;
        }

        public Task<ItemUpdateType> FetchAsync(Photo item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            item.SetImagePath(ImageType.Primary, item.Path);

            // Examples: https://github.com/mono/taglib-sharp/blob/a5f6949a53d09ce63ee7495580d6802921a21f14/tests/fixtures/TagLib.Tests.Images/NullOrientationTest.cs

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
                        item.Name = image.ImageTag.Title;
                    }

                    var dateTaken = image.ImageTag.DateTime;
                    if (dateTaken.HasValue)
                    {
                        item.DateCreated = dateTaken.Value;
                        item.PremiereDate = dateTaken.Value;
                        item.ProductionYear = dateTaken.Value.Year;
                    }

                    item.Genres = image.ImageTag.Genres.ToList();
                    item.Tags = image.ImageTag.Keywords.ToList();
                    item.Software = image.ImageTag.Software;

                    if (image.ImageTag.Orientation == TagLib.Image.ImageOrientation.None)
                    {
                        item.Orientation = null;
                    }
                    else
                    {
                        Model.Drawing.ImageOrientation orientation;
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
            catch (Exception e)
            {
                _logger.ErrorException("Image Provider - Error reading image tag for {0}", e, item.Path);
            }

            const ItemUpdateType result = ItemUpdateType.ImageUpdate | ItemUpdateType.MetadataImport;
            return Task.FromResult(result);
        }

        public string Name
        {
            get { return "Embedded Information"; }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService)
        {
            var file = directoryService.GetFile(item.Path);
            if (file != null && file.LastWriteTimeUtc != item.DateModified)
            {
                return true;
            }

            return false;
        }
    }
}
