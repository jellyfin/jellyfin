using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.TV
{
    /// <summary>
    /// Class SeriesResolver
    /// </summary>
    public class SeriesResolver : FolderResolver<Series>
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public SeriesResolver(IFileSystem fileSystem, ILogger logger, ILibraryManager libraryManager)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get
            {
                return ResolverPriority.Second;
            }
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Series.</returns>
        protected override Series Resolve(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                // Avoid expensive tests against VF's and all their children by not allowing this
                if (args.Parent == null || args.Parent.IsRoot)
                {
                    return null;
                }
                
                // Optimization to avoid running these tests against Seasons
                if (args.Parent is Series || args.Parent is Season || args.Parent is MusicArtist || args.Parent is MusicAlbum)
                {
                    return null;
                }

                var collectionType = args.GetCollectionType();

                var isTvShowsFolder = string.Equals(collectionType, CollectionType.TvShows,
                    StringComparison.OrdinalIgnoreCase);

                // If there's a collection type and it's not tv, it can't be a series
                if (!string.IsNullOrEmpty(collectionType) &&
                    !isTvShowsFolder &&
                    !string.Equals(collectionType, CollectionType.BoxSets, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (IsSeriesFolder(args.Path, isTvShowsFolder, args.FileSystemChildren, args.DirectoryService, _fileSystem, _logger, _libraryManager))
                {
                    return new Series
                    {
                        Path = args.Path,
                        Name = Path.GetFileName(args.Path)
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether [is series folder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="considerSeasonlessEntries">if set to <c>true</c> [consider seasonless entries].</param>
        /// <param name="fileSystemChildren">The file system children.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <returns><c>true</c> if [is series folder] [the specified path]; otherwise, <c>false</c>.</returns>
        public static bool IsSeriesFolder(string path, bool considerSeasonlessEntries, IEnumerable<FileSystemInfo> fileSystemChildren, IDirectoryService directoryService, IFileSystem fileSystem, ILogger logger, ILibraryManager libraryManager)
        {
            foreach (var child in fileSystemChildren)
            {
                var attributes = child.Attributes;

                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    //logger.Debug("Igoring series file or folder marked hidden: {0}", child.FullName);
                    continue;
                }

                // Can't enforce this because files saved by Bitcasa are always marked System
                //if ((attributes & FileAttributes.System) == FileAttributes.System)
                //{
                //    logger.Debug("Igoring series subfolder marked system: {0}", child.FullName);
                //    continue;
                //}

                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (IsSeasonFolder(child.FullName, directoryService, fileSystem))
                    {
                        //logger.Debug("{0} is a series because of season folder {1}.", path, child.FullName);
                        return true;
                    }
                }
                else
                {
                    var fullName = child.FullName;

                    if (libraryManager.IsVideoFile(fullName) || IsVideoPlaceHolder(fullName))
                    {
                        if (GetEpisodeNumberFromFile(fullName, considerSeasonlessEntries).HasValue)
                        {
                            return true;
                        }
                    }
                }
            }

            logger.Debug("{0} is not a series folder.", path);
            return false;
        }

        /// <summary>
        /// Determines whether [is place holder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is place holder] [the specified path]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        private static bool IsVideoPlaceHolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var extension = Path.GetExtension(path);

            return string.Equals(extension, ".disc", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether [is season folder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <returns><c>true</c> if [is season folder] [the specified path]; otherwise, <c>false</c>.</returns>
        private static bool IsSeasonFolder(string path, IDirectoryService directoryService, IFileSystem fileSystem)
        {
            var seasonNumber = GetSeasonNumberFromPath(path);
            var hasSeasonNumber = seasonNumber != null;

            if (!hasSeasonNumber)
            {
                return false;
            }

            //// It's a season folder if it's named as such and does not contain any audio files, apart from theme.mp3
            //foreach (var fileSystemInfo in directoryService.GetFileSystemEntries(path))
            //{
            //    var attributes = fileSystemInfo.Attributes;

            //    if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            //    {
            //        continue;
            //    }

            //    // Can't enforce this because files saved by Bitcasa are always marked System
            //    //if ((attributes & FileAttributes.System) == FileAttributes.System)
            //    //{
            //    //    continue;
            //    //}

            //    if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            //    {
            //        //if (IsBadFolder(fileSystemInfo.Name))
            //        //{
            //        //    return false;
            //        //}
            //    }
            //    else
            //    {
            //        if (EntityResolutionHelper.IsAudioFile(fileSystemInfo.FullName) &&
            //            !string.Equals(fileSystem.GetFileNameWithoutExtension(fileSystemInfo), BaseItem.ThemeSongFilename))
            //        {
            //            return false;
            //        }
            //    }
            //}

            return true;
        }

        /// <summary>
        /// A season folder must contain one of these somewhere in the name
        /// </summary>
        private static readonly string[] SeasonFolderNames =
        {
            "season",
            "sæson",
            "temporada",
            "saison",
            "staffel",
            "series",
            "сезон"
        };

        /// <summary>
        /// Used to detect paths that represent episodes, need to make sure they don't also
        /// match movie titles like "2001 A Space..."
        /// Currently we limit the numbers here to 2 digits to try and avoid this
        /// </summary>
        private static readonly Regex[] EpisodeExpressions =
        {
            new Regex(
                @".*(\\|\/)[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)[sS](?<seasonnumber>\d{1,4})[x,X]?[eE](?<epnumber>\d{1,3})[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d{1,3})[^\\\/]*$",
                RegexOptions.Compiled)
        };
        private static readonly Regex[] MultipleEpisodeExpressions =
        {
            new Regex(
                @".*(\\|\/)[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})((-| - )\d{1,4}[eExX](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})((-| - )\d{1,4}[xX][eE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})((-| - )?[xXeE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})(-[xE]?[eE]?(?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))((-| - )\d{1,4}[xXeE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))((-| - )\d{1,4}[xX][eE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))((-| - )?[xXeE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\\/])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))(-[xX]?[eE]?(?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d{1,3})((-| - )?[xXeE](?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled),
            new Regex(
                @".*(\\|\/)(?<seriesname>[^\\\/]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d{1,3})(-[xX]?[eE]?(?<endingepnumber>\d{1,3}))+[^\\\/]*$",
                RegexOptions.Compiled)
        };

        /// <summary>
        /// To avoid the following matching movies they are only valid when contained in a folder which has been matched as a being season, or the media type is TV series
        /// </summary>
        private static readonly Regex[] EpisodeExpressionsWithoutSeason =
        {
            new Regex(
                @".*[\\\/](?<epnumber>\d{1,3})(-(?<endingepnumber>\d{2,3}))*\.\w+$",
                RegexOptions.Compiled),
            // "01.avi"
            new Regex(
                @".*(\\|\/)(?<epnumber>\d{1,3})(-(?<endingepnumber>\d{2,3}))*\s?-\s?[^\\\/]*$",
                RegexOptions.Compiled),
            // "01 - blah.avi", "01-blah.avi"
             new Regex(
                @".*(\\|\/)(?<epnumber>\d{1,3})(-(?<endingepnumber>\d{2,3}))*\.[^\\\/]+$",
                RegexOptions.Compiled),
            // "01.blah.avi"
            new Regex(
                @".*[\\\/][^\\\/]* - (?<epnumber>\d{1,3})(-(?<endingepnumber>\d{2,3}))*[^\\\/]*$",
                RegexOptions.Compiled),
            // "blah - 01.avi", "blah 2 - 01.avi", "blah - 01 blah.avi", "blah 2 - 01 blah", "blah - 01 - blah.avi", "blah 2 - 01 - blah"
        };

        /// <summary>
        /// Gets the season number from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        public static int? GetSeasonNumberFromPath(string path)
        {
            var filename = Path.GetFileName(path);

            if (string.Equals(filename, "specials", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            int val;
            if (int.TryParse(filename, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
            {
                return val;
            }

            if (filename.StartsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                var testFilename = filename.Substring(1);

                if (int.TryParse(testFilename, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                {
                    return val;
                }
            }

            // Look for one of the season folder names
            foreach (var name in SeasonFolderNames)
            {
                var index = filename.IndexOf(name, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    return GetSeasonNumberFromPathSubstring(filename.Substring(index + name.Length));
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the season number from the second half of the Season folder name (everything after "Season", or "Staffel")
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private static int? GetSeasonNumberFromPathSubstring(string path)
        {
            var numericStart = -1;
            var length = 0;

            // Find out where the numbers start, and then keep going until they end
            for (var i = 0; i < path.Length; i++)
            {
                if (char.IsNumber(path, i))
                {
                    if (numericStart == -1)
                    {
                        numericStart = i;
                    }
                    length++;
                }
                else if (numericStart != -1)
                {
                    break;
                }
            }

            if (numericStart == -1)
            {
                return null;
            }

            return int.Parse(path.Substring(numericStart, length), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Episodes the number from file.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <param name="considerSeasonlessNames">if set to <c>true</c> [is in season].</param>
        /// <returns>System.String.</returns>
        public static int? GetEpisodeNumberFromFile(string fullPath, bool considerSeasonlessNames)
        {
            string fl = fullPath.ToLower();
            foreach (var r in EpisodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                    return ParseEpisodeNumber(m.Groups["epnumber"].Value);
            }
            if (considerSeasonlessNames)
            {
                var match = EpisodeExpressionsWithoutSeason.Select(r => r.Match(fl))
                    .FirstOrDefault(m => m.Success);

                if (match != null)
                {
                    return ParseEpisodeNumber(match.Groups["epnumber"].Value);
                }
            }

            return null;
        }

        public static int? GetEndingEpisodeNumberFromFile(string fullPath)
        {
            var fl = fullPath.ToLower();
            foreach (var r in MultipleEpisodeExpressions)
            {
                var m = r.Match(fl);
                if (m.Success && !string.IsNullOrEmpty(m.Groups["endingepnumber"].Value))
                    return ParseEpisodeNumber(m.Groups["endingepnumber"].Value);
            }
            foreach (var r in EpisodeExpressionsWithoutSeason)
            {
                var m = r.Match(fl);
                if (m.Success && !string.IsNullOrEmpty(m.Groups["endingepnumber"].Value))
                    return ParseEpisodeNumber(m.Groups["endingepnumber"].Value);
            }
            return null;
        }

        /// <summary>
        /// Seasons the number from episode file.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <returns>System.String.</returns>
        public static int? GetSeasonNumberFromEpisodeFile(string fullPath)
        {
            string fl = fullPath.ToLower();
            foreach (var r in EpisodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                {
                    Group g = m.Groups["seasonnumber"];
                    if (g != null)
                    {
                        var val = g.Value;

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int num;

                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out num))
                            {
                                return num;
                            }
                        }
                    }
                    return null;
                }
            }
            return null;
        }

        public static string GetSeriesNameFromEpisodeFile(string fullPath)
        {
            var fl = fullPath.ToLower();
            foreach (var r in EpisodeExpressions)
            {
                var m = r.Match(fl);
                if (m.Success)
                {
                    var g = m.Groups["seriesname"];
                    if (g != null)
                    {
                        var val = g.Value;

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            return val;
                        }
                    }
                    return null;
                }
            }
            return null;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private static int? ParseEpisodeNumber(string val)
        {
            int num;

            if (!string.IsNullOrEmpty(val) && int.TryParse(val, NumberStyles.Integer, UsCulture, out num))
            {
                return num;
            }

            return null;
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(Series item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            SetProviderIdFromPath(item, args.Path);
        }

        /// <summary>
        /// Sets the provider id from path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="path">The path.</param>
        private void SetProviderIdFromPath(Series item, string path)
        {
            var justName = Path.GetFileName(path);

            var id = justName.GetAttributeValue("tvdbid");

            if (!string.IsNullOrEmpty(id))
            {
                item.SetProviderId(MetadataProviders.Tvdb, id);
            }
        }
    }
}
