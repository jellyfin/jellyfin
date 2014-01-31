using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaBrowser.Providers.All
{
    public class LocalImageProvider : IImageFileProvider
    {
        private readonly IFileSystem _fileSystem;

        public LocalImageProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get { return "Local Images"; }
        }

        public int Order
        {
            get { return 0; }
        }

        public bool Supports(IHasImages item)
        {
            var locationType = item.LocationType;

            if (locationType == LocationType.FileSystem)
            {
                // Episode has it's own provider
                if (item is Episode || item is Audio)
                {
                    return false;
                }

                return true;
            }

            if (locationType == LocationType.Virtual)
            {
                var season = item as Season;

                if (season != null)
                {
                    var series = season.Series;

                    if (series != null && series.LocationType == LocationType.FileSystem)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerable<string> GetFiles(IHasImages item, bool includeDirectories)
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return new List<string>();
            }

            var path = item.Path;
            var fileInfo = _fileSystem.GetFileSystemInfo(path) as DirectoryInfo;

            if (fileInfo == null)
            {
                path = Path.GetDirectoryName(path);
            }

            if (includeDirectories)
            {
                return Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
            }
            return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        public List<LocalImageInfo> GetImages(IHasImages item)
        {
            var files = GetFileDictionary(GetFiles(item, true));

            var list = new List<LocalImageInfo>();

            PopulateImages(item, list, files);

            return list;
        }

        private void PopulateImages(IHasImages item, List<LocalImageInfo> images, Dictionary<string, string> files)
        {
            var imagePrefix = string.Empty;

            var baseItem = item as BaseItem;
            if (baseItem != null && baseItem.IsInMixedFolder)
            {
                imagePrefix = Path.GetFileNameWithoutExtension(item.Path) + "-";
            }

            PopulatePrimaryImages(item, images, files, imagePrefix);
            PopulateBackdrops(item, images, files, imagePrefix);
            PopulateScreenshots(images, files, imagePrefix);

            AddImage(files, images, imagePrefix + "logo", ImageType.Logo);
            AddImage(files, images, imagePrefix + "clearart", ImageType.Art);
            AddImage(files, images, imagePrefix + "disc", ImageType.Disc);
            AddImage(files, images, imagePrefix + "cdart", ImageType.Disc);
            AddImage(files, images, imagePrefix + "box", ImageType.Box);
            AddImage(files, images, imagePrefix + "back", ImageType.BoxRear);
            AddImage(files, images, imagePrefix + "boxrear", ImageType.BoxRear);
            AddImage(files, images, imagePrefix + "menu", ImageType.Menu);

            // Banner
            AddImage(files, images, imagePrefix + "banner", ImageType.Banner);

            // Thumb
            AddImage(files, images, imagePrefix + "thumb", ImageType.Thumb);
            AddImage(files, images, imagePrefix + "landscape", ImageType.Thumb);

            var season = item as Season;

            if (season != null)
            {
                PopulateSeasonImagesFromSeriesFolder(season, images);
            }
        }

        private void PopulatePrimaryImages(IHasImages item, List<LocalImageInfo> images, Dictionary<string, string> files, string imagePrefix)
        {
            AddImage(files, images, imagePrefix + "folder", ImageType.Primary);
            AddImage(files, images, imagePrefix + "cover", ImageType.Primary);
            AddImage(files, images, imagePrefix + "poster", ImageType.Primary);
            AddImage(files, images, imagePrefix + "default", ImageType.Primary);

            // Support plex/xbmc convention
            if (item is Series)
            {
                AddImage(files, images, imagePrefix + "show", ImageType.Primary);
            }

            // Support plex/xbmc convention
            if (item is Movie || item is MusicVideo || item is AdultVideo)
            {
                AddImage(files, images, imagePrefix + "movie", ImageType.Primary);
            }

            if (string.IsNullOrEmpty(item.Path))
            {
                var name = Path.GetFileNameWithoutExtension(item.Path);

                if (!string.IsNullOrEmpty(name))
                {
                    AddImage(files, images, name, ImageType.Primary);
                    AddImage(files, images, name + "-poster", ImageType.Primary);
                }
            }
        }

        private void PopulateBackdrops(IHasImages item, List<LocalImageInfo> images, Dictionary<string, string> files, string imagePrefix)
        {
            PopulateBackdrops(images, files, imagePrefix, "backdrop", "backdrop", ImageType.Backdrop);

            if (string.IsNullOrEmpty(item.Path))
            {
                var name = Path.GetFileNameWithoutExtension(item.Path);

                if (!string.IsNullOrEmpty(name))
                {
                    AddImage(files, images, imagePrefix + name + "-fanart", ImageType.Backdrop);
                }
            }

            PopulateBackdrops(images, files, imagePrefix, "fanart", "fanart-", ImageType.Backdrop);
            PopulateBackdrops(images, files, imagePrefix, "background", "background-", ImageType.Backdrop);
            PopulateBackdrops(images, files, imagePrefix, "art", "art-", ImageType.Backdrop);

            string extraFanartFolder;
            if (files.TryGetValue("extrafanart", out extraFanartFolder))
            {
                PopulateBackdropsFromExtraFanart(extraFanartFolder, images);
            }
        }

        private void PopulateBackdropsFromExtraFanart(string path, List<LocalImageInfo> images)
        {
            var imageFiles = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
                .Where(i =>
                {
                    var extension = Path.GetExtension(i);

                    if (string.IsNullOrEmpty(extension))
                    {
                        return false;
                    }

                    return BaseItem.SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
                });

            images.AddRange(imageFiles.Select(i => new LocalImageInfo
            {
                Path = i,
                Type = ImageType.Backdrop
            }));
        }

        private void PopulateScreenshots(List<LocalImageInfo> images, Dictionary<string, string> files, string imagePrefix)
        {
            PopulateBackdrops(images, files, imagePrefix, "screenshot", "screenshot", ImageType.Screenshot);
        }

        private void PopulateBackdrops(List<LocalImageInfo> images, Dictionary<string, string> files, string imagePrefix, string firstFileName, string subsequentFileNamePrefix, ImageType type)
        {
            AddImage(files, images, imagePrefix + firstFileName, type);

            var unfound = 0;
            for (var i = 1; i <= 20; i++)
            {
                // Screenshot Image
                var found = AddImage(files, images, imagePrefix + subsequentFileNamePrefix + i, type);

                if (!found)
                {
                    unfound++;

                    if (unfound >= 3)
                    {
                        break;
                    }
                }
            }
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private void PopulateSeasonImagesFromSeriesFolder(Season season, List<LocalImageInfo> images)
        {
            var seasonNumber = season.IndexNumber;

            var series = season.Series;
            if (!seasonNumber.HasValue || series.LocationType != LocationType.FileSystem)
            {
                return;
            }

            var files = GetFileDictionary(GetFiles(series, false));

            // Try using the season name
            var prefix = season.Name.ToLower().Replace(" ", string.Empty);

            var filenamePrefixes = new List<string> { prefix };

            var seasonMarker = seasonNumber.Value == 0
                                   ? "-specials"
                                   : seasonNumber.Value.ToString("00", _usCulture);

            // Get this one directly from the file system since we have to go up a level
            if (!string.Equals(prefix, seasonMarker, StringComparison.OrdinalIgnoreCase))
            {
                filenamePrefixes.Add("season" + seasonMarker);
            }

            foreach (var filename in filenamePrefixes)
            {
                AddImage(files, images, filename + "-poster", ImageType.Primary);
                AddImage(files, images, filename + "-fanart", ImageType.Backdrop);
                AddImage(files, images, filename + "-banner", ImageType.Banner);
                AddImage(files, images, filename + "-landscape", ImageType.Thumb);
            }
        }

        private Dictionary<string, string> GetFileDictionary(IEnumerable<string> paths)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in paths)
            {
                var filename = Path.GetFileName(path);

                if (!string.IsNullOrEmpty(filename))
                {
                    dict[filename] = path;
                }
            }

            return dict;
        }

        private bool AddImage(Dictionary<string, string> dict, List<LocalImageInfo> images, string name, ImageType type)
        {
            var image = GetImage(dict, name);

            if (image != null)
            {
                images.Add(new LocalImageInfo
                {
                    Path = image,
                    Type = type
                });

                return true;
            }

            return false;
        }

        private string GetImage(Dictionary<string, string> dict, string name)
        {
            return BaseItem.SupportedImageExtensions
                .Select(i =>
                {
                    var filename = name + i;
                    string path;

                    return dict.TryGetValue(filename, out path) ? path : null;
                })
                .FirstOrDefault(i => i != null);
        }
    }
}
