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

namespace MediaBrowser.LocalMetadata.Images
{
    public class LocalImageProvider : ILocalImageFileProvider
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
            if (item.SupportsLocalMetadata)
            {
                // Episode has it's own provider
                if (item.IsOwnedItem || item is Episode || item is Audio || item is Photo)
                {
                    return false;
                }

                return true;
            }

            if (item.LocationType == LocationType.Virtual)
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

        private IEnumerable<FileSystemInfo> GetFiles(IHasImages item, bool includeDirectories, IDirectoryService directoryService)
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return new List<FileSystemInfo>();
            }

            var path = item.ContainingFolderPath;

            if (includeDirectories)
            {
                return directoryService.GetFileSystemEntries(path)
                .Where(i => BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase) ||
                (i.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
            }

            return directoryService.GetFiles(path)
                .Where(i => BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase));
        }

        public List<LocalImageInfo> GetImages(IHasImages item, IDirectoryService directoryService)
        {
            var files = GetFiles(item, true, directoryService).ToList();

            var list = new List<LocalImageInfo>();

            PopulateImages(item, list, files, true, directoryService);

            return list;
        }

        public List<LocalImageInfo> GetImages(IHasImages item, string path, IDirectoryService directoryService)
        {
            return GetImages(item, new[] { path }, directoryService);
        }

        public List<LocalImageInfo> GetImages(IHasImages item, IEnumerable<string> paths, IDirectoryService directoryService)
        {
            var files = paths.SelectMany(directoryService.GetFiles)
                .Where(i =>
                {
                    var ext = i.Extension;

                    return !string.IsNullOrEmpty(ext) &&
                           BaseItem.SupportedImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                })
               .ToList();

            var list = new List<LocalImageInfo>();

            PopulateImages(item, list, files, false, directoryService);

            return list;
        }

        private void PopulateImages(IHasImages item, List<LocalImageInfo> images, List<FileSystemInfo> files, bool supportParentSeriesFiles, IDirectoryService directoryService)
        {
            var imagePrefix = string.Empty;

            var baseItem = item as BaseItem;
            if (baseItem != null && baseItem.IsInMixedFolder)
            {
                imagePrefix = _fileSystem.GetFileNameWithoutExtension(item.Path) + "-";
            }

            PopulatePrimaryImages(item, images, files, imagePrefix);
            PopulateBackdrops(item, images, files, imagePrefix, directoryService);
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

            if (supportParentSeriesFiles)
            {
                var season = item as Season;

                if (season != null)
                {
                    PopulateSeasonImagesFromSeriesFolder(season, images, directoryService);
                }
            }
        }

        private void PopulatePrimaryImages(IHasImages item, List<LocalImageInfo> images, List<FileSystemInfo> files, string imagePrefix)
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

            if (!string.IsNullOrEmpty(item.Path))
            {
                var name = _fileSystem.GetFileNameWithoutExtension(item.Path);

                if (!string.IsNullOrEmpty(name))
                {
                    AddImage(files, images, name, ImageType.Primary);
                    AddImage(files, images, name + "-poster", ImageType.Primary);
                }
            }
        }

        private void PopulateBackdrops(IHasImages item, List<LocalImageInfo> images, List<FileSystemInfo> files, string imagePrefix, IDirectoryService directoryService)
        {
            PopulateBackdrops(images, files, imagePrefix, "backdrop", "backdrop", ImageType.Backdrop);

            if (!string.IsNullOrEmpty(item.Path))
            {
                var name = _fileSystem.GetFileNameWithoutExtension(item.Path);

                if (!string.IsNullOrEmpty(name))
                {
                    AddImage(files, images, imagePrefix + name + "-fanart", ImageType.Backdrop);
                }
            }

            PopulateBackdrops(images, files, imagePrefix, "fanart", "fanart-", ImageType.Backdrop);
            PopulateBackdrops(images, files, imagePrefix, "background", "background-", ImageType.Backdrop);
            PopulateBackdrops(images, files, imagePrefix, "art", "art-", ImageType.Backdrop);

            var extraFanartFolder = files
                .FirstOrDefault(i => string.Equals(i.Name, "extrafanart", StringComparison.OrdinalIgnoreCase));

            if (extraFanartFolder != null)
            {
                PopulateBackdropsFromExtraFanart(extraFanartFolder.FullName, images, directoryService);
            }
        }

        private void PopulateBackdropsFromExtraFanart(string path, List<LocalImageInfo> images, IDirectoryService directoryService)
        {
            var imageFiles = directoryService.GetFiles(path)
                .Where(i =>
                {
                    var extension = i.Extension;

                    if (string.IsNullOrEmpty(extension))
                    {
                        return false;
                    }

                    return BaseItem.SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
                });

            images.AddRange(imageFiles.Select(i => new LocalImageInfo
            {
                FileInfo = i,
                Type = ImageType.Backdrop
            }));
        }

        private void PopulateScreenshots(List<LocalImageInfo> images, List<FileSystemInfo> files, string imagePrefix)
        {
            PopulateBackdrops(images, files, imagePrefix, "screenshot", "screenshot", ImageType.Screenshot);
        }

        private void PopulateBackdrops(List<LocalImageInfo> images, List<FileSystemInfo> files, string imagePrefix, string firstFileName, string subsequentFileNamePrefix, ImageType type)
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

        private void PopulateSeasonImagesFromSeriesFolder(Season season, List<LocalImageInfo> images, IDirectoryService directoryService)
        {
            var seasonNumber = season.IndexNumber;

            var series = season.Series;
            if (!seasonNumber.HasValue || series.LocationType != LocationType.FileSystem)
            {
                return;
            }

            var seriesFiles = GetFiles(series, false, directoryService).ToList();

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
                AddImage(seriesFiles, images, filename + "-poster", ImageType.Primary);
                AddImage(seriesFiles, images, filename + "-fanart", ImageType.Backdrop);
                AddImage(seriesFiles, images, filename + "-banner", ImageType.Banner);
                AddImage(seriesFiles, images, filename + "-landscape", ImageType.Thumb);
            }
        }

        private bool AddImage(IEnumerable<FileSystemInfo> files, List<LocalImageInfo> images, string name, ImageType type)
        {
            var image = GetImage(files, name) as FileInfo;

            if (image != null)
            {
                images.Add(new LocalImageInfo
                {
                    FileInfo = image,
                    Type = type
                });

                return true;
            }

            return false;
        }

        private FileSystemInfo GetImage(IEnumerable<FileSystemInfo> files, string name)
        {
            var candidates = files
                .Where(i => string.Equals(name, _fileSystem.GetFileNameWithoutExtension(i), StringComparison.OrdinalIgnoreCase))
                .ToList();

            return BaseItem.SupportedImageExtensions
                .Select(i => candidates.FirstOrDefault(c => string.Equals(c.Extension, i, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault(i => i != null);
        }
    }
}
