using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace MediaBrowser.LocalMetadata.Images
{
    public class LocalImageProvider : ILocalImageProvider, IHasOrder
    {
        private readonly IFileSystem _fileSystem;

        public LocalImageProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string Name => "Local Images";

        public int Order => 0;

        public bool Supports(BaseItem item)
        {
            if (item.SupportsLocalMetadata)
            {
                // Episode has its own provider
                if (item is Episode || item is Audio || item is Photo)
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

                    if (series != null && series.IsFileProtocol)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<FileSystemMetadata> GetFiles(BaseItem item, bool includeDirectories, IDirectoryService directoryService)
        {
            if (!item.IsFileProtocol)
            {
                return new List<FileSystemMetadata>();
            }

            var path = item.ContainingFolderPath;

            // Exit if the cache dir does not exist, alternative solution is to create it, but that's a lot of empty dirs...
            if (!Directory.Exists(path))
            {
                return Array.Empty<FileSystemMetadata>();
            }

            if (includeDirectories)
            {
                return directoryService.GetFileSystemEntries(path)
                .Where(i => BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase) || i.IsDirectory)

                .OrderBy(i => Array.IndexOf(BaseItem.SupportedImageExtensions, i.Extension ?? string.Empty));
            }

            return directoryService.GetFiles(path)
                .Where(i => BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                .OrderBy(i => Array.IndexOf(BaseItem.SupportedImageExtensions, i.Extension ?? string.Empty));
        }

        public List<LocalImageInfo> GetImages(BaseItem item, IDirectoryService directoryService)
        {
            var files = GetFiles(item, true, directoryService).ToList();

            var list = new List<LocalImageInfo>();

            PopulateImages(item, list, files, true, directoryService);

            return list;
        }

        public List<LocalImageInfo> GetImages(BaseItem item, string path, bool isPathInMediaFolder, IDirectoryService directoryService)
        {
            return GetImages(item, new[] { path }, isPathInMediaFolder, directoryService);
        }

        public List<LocalImageInfo> GetImages(BaseItem item, IEnumerable<string> paths, bool arePathsInMediaFolders, IDirectoryService directoryService)
        {
            IEnumerable<FileSystemMetadata> files = paths.SelectMany(i => _fileSystem.GetFiles(i, BaseItem.SupportedImageExtensions, true, false));

            files = files
                .OrderBy(i => Array.IndexOf(BaseItem.SupportedImageExtensions, i.Extension ?? string.Empty));

            var list = new List<LocalImageInfo>();

            PopulateImages(item, list, files.ToList(), false, directoryService);

            return list;
        }

        private void PopulateImages(BaseItem item, List<LocalImageInfo> images, List<FileSystemMetadata> files, bool supportParentSeriesFiles, IDirectoryService directoryService)
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
            var isInMixedFolder = item.IsInMixedFolder;

            PopulatePrimaryImages(item, images, files, imagePrefix, isInMixedFolder);

            var added = false;
            var isEpisode = item is Episode;
            var isSong = item.GetType() == typeof(Audio);
            var isPerson = item is Person;

            // Logo
            if (!isEpisode && !isSong && !isPerson)
            {
                added = AddImage(files, images, "logo", imagePrefix, isInMixedFolder, ImageType.Logo);
                if (!added)
                {
                    added = AddImage(files, images, "clearlogo", imagePrefix, isInMixedFolder, ImageType.Logo);
                }
            }

            // Art
            if (!isEpisode && !isSong && !isPerson)
            {
                AddImage(files, images, "clearart", imagePrefix, isInMixedFolder, ImageType.Art);
            }

            // For music albums, prefer cdart before disc
            if (item is MusicAlbum)
            {
                added = AddImage(files, images, "cdart", imagePrefix, isInMixedFolder, ImageType.Disc);

                if (!added)
                {
                    added = AddImage(files, images, "disc", imagePrefix, isInMixedFolder, ImageType.Disc);
                }
            }
            else if (item is Video || item is BoxSet)
            {
                added = AddImage(files, images, "disc", imagePrefix, isInMixedFolder, ImageType.Disc);

                if (!added)
                {
                    added = AddImage(files, images, "cdart", imagePrefix, isInMixedFolder, ImageType.Disc);
                }

                if (!added)
                {
                    added = AddImage(files, images, "discart", imagePrefix, isInMixedFolder, ImageType.Disc);
                }
            }

            // Banner
            if (!isEpisode && !isSong && !isPerson)
            {
                AddImage(files, images, "banner", imagePrefix, isInMixedFolder, ImageType.Banner);
            }

            // Thumb
            if (!isEpisode && !isSong && !isPerson)
            {
                added = AddImage(files, images, "landscape", imagePrefix, isInMixedFolder, ImageType.Thumb);
                if (!added)
                {
                    added = AddImage(files, images, "thumb", imagePrefix, isInMixedFolder, ImageType.Thumb);
                }
            }

            if (!isEpisode && !isSong && !isPerson)
            {
                PopulateBackdrops(item, images, files, imagePrefix, isInMixedFolder, directoryService);
            }

            if (item is IHasScreenshots)
            {
                PopulateScreenshots(images, files, imagePrefix, isInMixedFolder);
            }
        }

        private static readonly string[] CommonImageFileNames = new[]
        {
            "poster",
            "folder",
            "cover",
            "default"
        };

        private static readonly string[] MusicImageFileNames = new[]
        {
            "folder",
            "poster",
            "cover",
            "default"
        };

        private static readonly string[] PersonImageFileNames = new[]
        {
            "folder",
            "poster"
        };

        private static readonly string[] SeriesImageFileNames = new[]
        {
            "poster",
            "folder",
            "cover",
            "default",
            "show"
        };

        private static readonly string[] VideoImageFileNames = new[]
        {
            "poster",
            "folder",
            "cover",
            "default",
            "movie"
        };

        private void PopulatePrimaryImages(BaseItem item, List<LocalImageInfo> images, List<FileSystemMetadata> files, string imagePrefix, bool isInMixedFolder)
        {
            string[] imageFileNames;

            if (item is MusicAlbum || item is MusicArtist || item is PhotoAlbum)
            {
                // these prefer folder
                imageFileNames = MusicImageFileNames;
            }
            else if (item is Person)
            {
                // these prefer folder
                imageFileNames = PersonImageFileNames;
            }
            else if (item is Series)
            {
                imageFileNames = SeriesImageFileNames;
            }
            else if (item is Video && !(item is Episode))
            {
                imageFileNames = VideoImageFileNames;
            }
            else
            {
                imageFileNames = CommonImageFileNames;
            }

            var fileNameWithoutExtension = item.FileNameWithoutExtension;
            if (!string.IsNullOrEmpty(fileNameWithoutExtension))
            {
                if (AddImage(files, images, fileNameWithoutExtension, ImageType.Primary))
                {
                    return;
                }
            }

            foreach (var name in imageFileNames)
            {
                if (AddImage(files, images, imagePrefix + name, ImageType.Primary))
                {
                    return;
                }
            }

            if (!isInMixedFolder)
            {
                foreach (var name in imageFileNames)
                {
                    if (AddImage(files, images, name, ImageType.Primary))
                    {
                        return;
                    }
                }
            }
        }

        private void PopulateBackdrops(BaseItem item, List<LocalImageInfo> images, List<FileSystemMetadata> files, string imagePrefix, bool isInMixedFolder, IDirectoryService directoryService)
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
            var imageFiles = _fileSystem.GetFiles(path, BaseItem.SupportedImageExtensions, false, false);

            images.AddRange(imageFiles.Where(i => i.Length > 0).Select(i => new LocalImageInfo
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
            if (!seasonNumber.HasValue || !series.IsFileProtocol)
            {
                return;
            }

            var seriesFiles = GetFiles(series, false, directoryService).ToList();

            // Try using the season name
            var prefix = season.Name.ToLowerInvariant().Replace(" ", string.Empty);

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
            return files.FirstOrDefault(i => !i.IsDirectory && string.Equals(name, _fileSystem.GetFileNameWithoutExtension(i), StringComparison.OrdinalIgnoreCase) && i.Length > 0);
        }
    }
}
