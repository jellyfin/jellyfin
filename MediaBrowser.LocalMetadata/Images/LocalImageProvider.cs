using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommonIO;

namespace MediaBrowser.LocalMetadata.Images
{
    public class LocalImageProvider : ILocalImageFileProvider, IHasOrder
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

        private IEnumerable<FileSystemMetadata> GetFiles(IHasImages item, bool includeDirectories, IDirectoryService directoryService)
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return new List<FileSystemMetadata>();
            }

            var path = item.ContainingFolderPath;

            if (includeDirectories)
            {
                return directoryService.GetFileSystemEntries(path)
                .Where(i => BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase) || i.IsDirectory)

                .OrderBy(i => BaseItem.SupportedImageExtensionsList.IndexOf(i.Extension ?? string.Empty));
            }

            return directoryService.GetFiles(path)
                .Where(i => BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                .OrderBy(i => BaseItem.SupportedImageExtensionsList.IndexOf(i.Extension ?? string.Empty));
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
                .OrderBy(i => BaseItem.SupportedImageExtensionsList.IndexOf(i.Extension ?? string.Empty))
               .ToList();

            var list = new List<LocalImageInfo>();

            PopulateImages(item, list, files, false, directoryService);

            return list;
        }

        private void PopulateImages(IHasImages item, List<LocalImageInfo> images, List<FileSystemMetadata> files, bool supportParentSeriesFiles, IDirectoryService directoryService)
        {
            if (supportParentSeriesFiles)
            {
                var season = item as Season;

                if (season != null)
                {
                    PopulateSeasonImagesFromSeriesFolder(season, images, directoryService);
                }
            }
            
            var imagePrefix = item.FileNameWithoutExtension + "-";
            var isInMixedFolder = item.DetectIsInMixedFolder();

            PopulatePrimaryImages(item, images, files, imagePrefix, isInMixedFolder);

            AddImage(files, images, "logo", imagePrefix, isInMixedFolder, ImageType.Logo);
            AddImage(files, images, "clearart", imagePrefix, isInMixedFolder, ImageType.Art);

            // For music albums, prefer cdart before disc
            if (item is MusicAlbum)
            {
                AddImage(files, images, "cdart", imagePrefix, isInMixedFolder, ImageType.Disc);
                AddImage(files, images, "disc", imagePrefix, isInMixedFolder, ImageType.Disc);
            }
            else
            {
                AddImage(files, images, "disc", imagePrefix, isInMixedFolder, ImageType.Disc);
                AddImage(files, images, "cdart", imagePrefix, isInMixedFolder, ImageType.Disc);
            }

            AddImage(files, images, "box", imagePrefix, isInMixedFolder, ImageType.Box);
            AddImage(files, images, "back", imagePrefix, isInMixedFolder, ImageType.BoxRear);
            AddImage(files, images, "boxrear", imagePrefix, isInMixedFolder, ImageType.BoxRear);
            AddImage(files, images, "menu", imagePrefix, isInMixedFolder, ImageType.Menu);

            // Banner
            AddImage(files, images, "banner", imagePrefix, isInMixedFolder, ImageType.Banner);

            // Thumb
            AddImage(files, images, "landscape", imagePrefix, isInMixedFolder, ImageType.Thumb);
            AddImage(files, images, "thumb", imagePrefix, isInMixedFolder, ImageType.Thumb);

            PopulateBackdrops(item, images, files, imagePrefix, isInMixedFolder, directoryService);
            PopulateScreenshots(images, files, imagePrefix, isInMixedFolder);
        }

        private void PopulatePrimaryImages(IHasImages item, List<LocalImageInfo> images, List<FileSystemMetadata> files, string imagePrefix, bool isInMixedFolder)
        {
            var names = new List<string>
            {
                "cover",
                "default"
            };

            if (item is MusicAlbum || item is MusicArtist || item is PhotoAlbum)
            {
                // these prefer folder
                names.Insert(0, "poster");
                names.Insert(0, "folder");
            }
            else
            {
                names.Insert(0, "folder");
                names.Insert(0, "poster");
            }
            
            // Support plex/kodi convention
            if (item is Series)
            {
                names.Add("show");
            }

            // Support plex/kodi convention
            if (item is Video && !(item is Episode))
            {
                names.Add("movie");
            }

            var fileNameWithoutExtension = item.FileNameWithoutExtension;
            if (!string.IsNullOrEmpty(fileNameWithoutExtension))
            {
                AddImage(files, images, fileNameWithoutExtension, ImageType.Primary);
            }

            foreach (var name in names)
            {
                AddImage(files, images, imagePrefix + name, ImageType.Primary);
            }

            if (!isInMixedFolder)
            {
                foreach (var name in names)
                {
                    AddImage(files, images, name, ImageType.Primary);
                }
            }
        }

        private void PopulateBackdrops(IHasImages item, List<LocalImageInfo> images, List<FileSystemMetadata> files, string imagePrefix, bool isInMixedFolder, IDirectoryService directoryService)
        {
            if (!string.IsNullOrEmpty(item.Path))
            {
                var name = item.FileNameWithoutExtension;

                if (!string.IsNullOrEmpty(name))
                {
                    AddImage(files, images, imagePrefix + name + "-fanart", ImageType.Backdrop);

                    // Support without the prefix if it's in it's own folder
                    if (!isInMixedFolder)
                    {
                        AddImage(files, images, name + "-fanart", ImageType.Backdrop);
                    }
                }
            }

            PopulateBackdrops(images, files, imagePrefix, "fanart", "fanart-", isInMixedFolder, ImageType.Backdrop);
            PopulateBackdrops(images, files, imagePrefix, "background", "background-", isInMixedFolder, ImageType.Backdrop);
            PopulateBackdrops(images, files, imagePrefix, "art", "art-", isInMixedFolder, ImageType.Backdrop);

            var extraFanartFolder = files
                .FirstOrDefault(i => string.Equals(i.Name, "extrafanart", StringComparison.OrdinalIgnoreCase));

            if (extraFanartFolder != null)
            {
                PopulateBackdropsFromExtraFanart(extraFanartFolder.FullName, images, directoryService);
            }

            PopulateBackdrops(images, files, imagePrefix, "backdrop", "backdrop", isInMixedFolder, ImageType.Backdrop);
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

        private void PopulateScreenshots(List<LocalImageInfo> images, List<FileSystemMetadata> files, string imagePrefix, bool isInMixedFolder)
        {
            PopulateBackdrops(images, files, imagePrefix, "screenshot", "screenshot", isInMixedFolder, ImageType.Screenshot);
        }

        private void PopulateBackdrops(List<LocalImageInfo> images, List<FileSystemMetadata> files, string imagePrefix, string firstFileName, string subsequentFileNamePrefix, bool isInMixedFolder, ImageType type)
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

            // Support without the prefix
            if (!isInMixedFolder)
            {
                AddImage(files, images, firstFileName, type);

                unfound = 0;
                for (var i = 1; i <= 20; i++)
                {
                    // Screenshot Image
                    var found = AddImage(files, images, subsequentFileNamePrefix + i, type);

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

        private bool AddImage(List<FileSystemMetadata> files, List<LocalImageInfo> images, string name, string imagePrefix, bool isInMixedFolder, ImageType type)
        {
            var added = AddImage(files, images, imagePrefix + name, type);

            if (!isInMixedFolder)
            {
                if (AddImage(files, images, name, type))
                {
                    added = true;
                }
            }

            return added;
        }

        private bool AddImage(IEnumerable<FileSystemMetadata> files, List<LocalImageInfo> images, string name, ImageType type)
        {
            var image = GetImage(files, name);

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

        private FileSystemMetadata GetImage(IEnumerable<FileSystemMetadata> files, string name)
        {
            return files.FirstOrDefault(i => !i.IsDirectory && string.Equals(name, _fileSystem.GetFileNameWithoutExtension(i), StringComparison.OrdinalIgnoreCase));
        }
    }
}
