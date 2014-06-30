using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaBrowser.XbmcMetadata.Images
{
    public class XbmcImageSaver : IImageFileSaver
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public IEnumerable<string> GetSavePaths(IHasImages item, ImageType type, ImageFormat format, int index)
        {
            var season = item as Season;

            if (!SupportsItem(item, type, season))
            {
                return new string[] { };
            }

            var extension = "." + format.ToString().ToLower();

            // Backdrop paths
            if (type == ImageType.Backdrop)
            {
                if (index == 0)
                {
                    if (item.IsInMixedFolder)
                    {
                        return new[] { GetSavePathForItemInMixedFolder(item, type, "fanart", extension) };
                    }

                    if (season != null && season.IndexNumber.HasValue)
                    {
                        var seriesFolder = season.SeriesPath;

                        var seasonMarker = season.IndexNumber.Value == 0
                                               ? "-specials"
                                               : season.IndexNumber.Value.ToString("00", _usCulture);

                        var imageFilename = "season" + seasonMarker + "-fanart" + extension;

                        return new[] { Path.Combine(seriesFolder, imageFilename) };
                    }

                    return new[]
                        {
                            Path.Combine(item.ContainingFolderPath, "fanart" + extension)
                        };
                }

                if (item.IsInMixedFolder)
                {
                    return new[] { GetSavePathForItemInMixedFolder(item, type, "fanart" + index.ToString(_usCulture), extension) };
                }

                var extraFanartFilename = GetBackdropSaveFilename(item.GetImages(ImageType.Backdrop), "fanart", "fanart", index);

                return new[]
                    {
                        Path.Combine(item.ContainingFolderPath, "extrafanart", extraFanartFilename + extension),
                        Path.Combine(item.ContainingFolderPath, "extrathumbs", "thumb" + index.ToString(_usCulture) + extension)
                    };
            }

            if (type == ImageType.Primary)
            {
                if (season != null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", _usCulture);

                    var imageFilename = "season" + seasonMarker + "-poster" + extension;

                    return new[] { Path.Combine(seriesFolder, imageFilename) };
                }

                if (item is Episode)
                {
                    var seasonFolder = Path.GetDirectoryName(item.Path);

                    var imageFilename = Path.GetFileNameWithoutExtension(item.Path) + "-thumb" + extension;

                    return new[] { Path.Combine(seasonFolder, imageFilename) };
                }

                if (item.IsInMixedFolder || item is MusicVideo)
                {
                    return new[] { GetSavePathForItemInMixedFolder(item, type, string.Empty, extension) };
                }

                if (item is MusicAlbum || item is MusicArtist)
                {
                    return new[] { Path.Combine(item.ContainingFolderPath, "folder" + extension) };
                }

                return new[] { Path.Combine(item.ContainingFolderPath, "poster" + extension) };
            }

            if (type == ImageType.Banner)
            {
                if (season != null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", _usCulture);

                    var imageFilename = "season" + seasonMarker + "-banner" + extension;

                    return new[] { Path.Combine(seriesFolder, imageFilename) };
                }
            }

            if (type == ImageType.Thumb)
            {
                if (season != null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", _usCulture);

                    var imageFilename = "season" + seasonMarker + "-landscape" + extension;

                    return new[] { Path.Combine(seriesFolder, imageFilename) };
                }

                if (item.IsInMixedFolder)
                {
                    return new[] { GetSavePathForItemInMixedFolder(item, type, "landscape", extension) };
                }

                return new[] { Path.Combine(item.ContainingFolderPath, "landscape" + extension) };
            }

            return GetStandardSavePaths(item, type, index, extension);
        }

        private IEnumerable<string> GetStandardSavePaths(IHasImages item, ImageType type, int imageIndex, string extension)
        {
            string filename;

            switch (type)
            {
                case ImageType.Art:
                    filename = "clearart";
                    break;
                case ImageType.BoxRear:
                    filename = "back";
                    break;
                case ImageType.Disc:
                    filename = item is MusicAlbum ? "cdart" : "disc";
                    break;
                case ImageType.Screenshot:
                    filename = GetBackdropSaveFilename(item.GetImages(type), "screenshot", "screenshot", imageIndex);
                    break;
                default:
                    filename = type.ToString().ToLower();
                    break;
            }

            string path = null;

            if (item.IsInMixedFolder)
            {
                path = GetSavePathForItemInMixedFolder(item, type, filename, extension);
            }

            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(item.ContainingFolderPath, filename + extension);
            }

            if (string.IsNullOrEmpty(path))
            {
                return new string[] { };
            }

            return new[] { path };
        }


        private string GetSavePathForItemInMixedFolder(IHasImages item, ImageType type, string imageFilename, string extension)
        {
            if (type == ImageType.Primary)
            {
                imageFilename = "poster";
            }
            var folder = Path.GetDirectoryName(item.Path);

            return Path.Combine(folder, Path.GetFileNameWithoutExtension(item.Path) + "-" + imageFilename + extension);
        }

        private bool SupportsItem(IHasImages item, ImageType type, Season season)
        {
            if (item.IsOwnedItem || item is Audio || item is User)
            {
                return false;
            }

            if (type != ImageType.Primary && item is Episode)
            {
                return false;
            }

            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            var locationType = item.LocationType;
            if (locationType == LocationType.Remote || locationType == LocationType.Virtual)
            {
                var allowSaving = false;

                // If season is virtual under a physical series, save locally if using compatible convention
                if (season != null)
                {
                    var series = season.Series;

                    if (series != null && series.SupportsLocalMetadata)
                    {
                        allowSaving = true;
                    }
                }

                if (!allowSaving)
                {
                    return false;
                }
            }

            return true;
        }

        private string GetBackdropSaveFilename(IEnumerable<ItemImageInfo> images, string zeroIndexFilename, string numberedIndexPrefix, int? index)
        {
            if (index.HasValue && index.Value == 0)
            {
                return zeroIndexFilename;
            }

            var filenames = images.Select(i => Path.GetFileNameWithoutExtension(i.Path)).ToList();

            var current = 1;
            while (filenames.Contains(numberedIndexPrefix + current.ToString(_usCulture), StringComparer.OrdinalIgnoreCase))
            {
                current++;
            }

            return numberedIndexPrefix + current.ToString(_usCulture);
        }

        public string Name
        {
            get { return "Media Browser/Plex/Xbmc Images"; }
        }
    }
}
