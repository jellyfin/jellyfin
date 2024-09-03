using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;

namespace Emby.Photos;

/// <summary>
/// Metadata provider for photos.
/// </summary>
public class PhotoProvider : ICustomMetadataProvider<Photo>, IForcedProvider, IHasItemChangeMonitor
{
    private readonly ILogger<PhotoProvider> _logger;
    private readonly IImageProcessor _imageProcessor;

    // Other extensions might cause taglib to hang
    private readonly string[] _includeExtensions = [".jpg", ".jpeg", ".png", ".tiff", ".cr2", ".webp", ".avif"];

    /// <summary>
    /// Initializes a new instance of the <see cref="PhotoProvider" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="imageProcessor">The image processor.</param>
    public PhotoProvider(ILogger<PhotoProvider> logger, IImageProcessor imageProcessor)
    {
        _logger = logger;
        _imageProcessor = imageProcessor;
    }

    /// <inheritdoc />
    public string Name => "Embedded Information";

    /// <inheritdoc />
    public bool HasChanged(BaseItem item, IDirectoryService directoryService)
    {
        if (item.IsFileProtocol)
        {
            var file = directoryService.GetFile(item.Path);
            return file is not null && file.LastWriteTimeUtc != item.DateModified;
        }

        return false;
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Photo item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        item.SetImagePath(ImageType.Primary, item.Path);

        // Examples: https://github.com/mono/taglib-sharp/blob/a5f6949a53d09ce63ee7495580d6802921a21f14/tests/fixtures/TagLib.Tests.Images/NullOrientationTest.cs
        if (_includeExtensions.Contains(Path.GetExtension(item.Path.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var file = TagLib.File.Create(item.Path);
                if (file.GetTag(TagTypes.TiffIFD) is IFDTag tag)
                {
                    var structure = tag.Structure;
                    if (structure?.GetEntry(0, (ushort)IFDEntryTag.ExifIFD) is SubIFDEntry exif)
                    {
                        var exifStructure = exif.Structure;
                        if (exifStructure is not null)
                        {
                            if (exifStructure.GetEntry(0, (ushort)ExifEntryTag.ApertureValue) is RationalIFDEntry apertureEntry)
                            {
                                item.Aperture = (double)apertureEntry.Value.Numerator / apertureEntry.Value.Denominator;
                            }

                            if (exifStructure.GetEntry(0, (ushort)ExifEntryTag.ShutterSpeedValue) is RationalIFDEntry shutterSpeedEntry)
                            {
                                item.ShutterSpeed = (double)shutterSpeedEntry.Value.Numerator / shutterSpeedEntry.Value.Denominator;
                            }
                        }
                    }
                }

                if (file is TagLib.Image.File image)
                {
                    item.CameraMake = image.ImageTag.Make;
                    item.CameraModel = image.ImageTag.Model;

                    item.Width = image.Properties.PhotoWidth;
                    item.Height = image.Properties.PhotoHeight;

                    item.CommunityRating = image.ImageTag.Rating;

                    item.Overview = image.ImageTag.Comment;

                    if (!string.IsNullOrWhiteSpace(image.ImageTag.Title)
                        && !item.LockedFields.Contains(MetadataField.Name))
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

                    item.Genres = image.ImageTag.Genres;
                    item.Tags = image.ImageTag.Keywords;
                    item.Software = image.ImageTag.Software;

                    if (image.ImageTag.Orientation == TagLib.Image.ImageOrientation.None)
                    {
                        item.Orientation = null;
                    }
                    else if (Enum.TryParse(image.ImageTag.Orientation.ToString(), true, out ImageOrientation orientation))
                    {
                        item.Orientation = orientation;
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
                var size = _imageProcessor.GetImageDimensions(item, img);

                if (size.Width > 0 && size.Height > 0)
                {
                    item.Width = size.Width;
                    item.Height = size.Height;
                }
            }
            catch (ArgumentException)
            {
                // format not supported
            }
        }

        const ItemUpdateType Result = ItemUpdateType.ImageUpdate | ItemUpdateType.MetadataImport;
        return Task.FromResult(Result);
    }
}
