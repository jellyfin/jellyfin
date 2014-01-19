using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Server.Implementations.FileSorting
{
    public class TvFileSorter
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        public TvFileSorter(ILibraryManager libraryManager, ILogger logger, IFileSystem fileSystem)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public void Sort(FileSortingOptions options, CancellationToken cancellationToken, IProgress<double> progress)
        {
            var minFileBytes = options.MinFileSizeMb * 1024 * 1024;

            var eligibleFiles = options.TvWatchLocations.SelectMany(GetEligibleFiles)
                .Where(i => EntityResolutionHelper.IsVideoFile(i.FullName) && i.Length >= minFileBytes)
                .ToList();

            progress.Report(10);
            
            if (eligibleFiles.Count > 0)
            {
                var allSeries = _libraryManager.RootFolder
                    .RecursiveChildren.OfType<Series>()
                    .Where(i => i.LocationType == LocationType.FileSystem)
                    .ToList();

                var numComplete = 0;
                
                foreach (var file in eligibleFiles)
                {
                    SortFile(file.FullName, options, allSeries);

                    numComplete++;
                    double percent = numComplete;
                    percent /= eligibleFiles.Count;

                    progress.Report(100 * percent);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(99);

            if (!options.EnableTrialMode)
            {
                foreach (var path in options.TvWatchLocations)
                {
                    if (options.LeftOverFileExtensionsToDelete.Length > 0)
                    {
                        DeleteLeftOverFiles(path, options.LeftOverFileExtensionsToDelete);
                    }

                    if (options.DeleteEmptyFolders)
                    {
                        DeleteEmptyFolders(path);
                    }
                }
            }

            progress.Report(100);
        }

        private IEnumerable<FileInfo> GetEligibleFiles(string path)
        {
            try
            {
                return new DirectoryInfo(path)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .ToList();
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error getting files from {0}", ex, path);

                return new List<FileInfo>(); 
            }
        }

        private void SortFile(string path, FileSortingOptions options, IEnumerable<Series> allSeries)
        {
            _logger.Info("Sorting file {0}", path);

            var seriesName = TVUtils.GetSeriesNameFromEpisodeFile(path);

            if (!string.IsNullOrEmpty(seriesName))
            {
                var season = TVUtils.GetSeasonNumberFromEpisodeFile(path);

                if (season.HasValue)
                {
                    // Passing in true will include a few extra regex's
                    var episode = TVUtils.GetEpisodeNumberFromFile(path, true);

                    if (episode.HasValue)
                    {
                        _logger.Debug("Extracted information from {0}. Series name {1}, Season {2}, Episode {3}", path, seriesName, season, episode);

                        SortFile(path, seriesName, season.Value, episode.Value, options, allSeries);
                    }
                    else
                    {
                        _logger.Warn("Unable to determine episode number from {0}", path);
                    }
                }
                else
                {
                    _logger.Warn("Unable to determine season number from {0}", path);
                }
            }
            else
            {
                _logger.Warn("Unable to determine series name from {0}", path);
            }
        }

        private void SortFile(string path, string seriesName, int seasonNumber, int episodeNumber, FileSortingOptions options, IEnumerable<Series> allSeries)
        {
            var series = GetMatchingSeries(seriesName, allSeries);

            if (series == null)
            {
                _logger.Warn("Unable to find series in library matching name {0}", seriesName);
                return;
            }

            _logger.Info("Sorting file {0} into series {1}", path, series.Path);

            // Proceed to sort the file
            var newPath = GetNewPath(series, seasonNumber, episodeNumber, options);

            if (string.IsNullOrEmpty(newPath))
            {
                _logger.Warn("Unable to sort {0} because target path could not be found.", path);
                return;
            }

            _logger.Info("Sorting file {0} to new path {1}", path, newPath);
        }

        private string GetNewPath(Series series, int seasonNumber, int episodeNumber, FileSortingOptions options)
        {
            var currentEpisodes = series.RecursiveChildren.OfType<Episode>()
                .Where(i => i.IndexNumber.HasValue && i.IndexNumber.Value == episodeNumber && i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == seasonNumber)
                .ToList();

            if (currentEpisodes.Count == 0)
            {
                return null;
            }

            var newPath = currentEpisodes
                .Where(i => i.LocationType == LocationType.FileSystem)
                .Select(i => i.Path)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(newPath))
            {
                newPath = GetSeasonFolderPath(series, seasonNumber, options);

                var episode = currentEpisodes.First();

                var episodeFileName = string.Format("{0} - {1}x{2} - {3}",

                    _fileSystem.GetValidFilename(series.Name),
                    seasonNumber.ToString(UsCulture),
                    episodeNumber.ToString("00", UsCulture),
                    _fileSystem.GetValidFilename(episode.Name)
                    );

                newPath = Path.Combine(newPath, episodeFileName);
            }

            return newPath;
        }

        /// <summary>
        /// Gets the season folder path.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        private string GetSeasonFolderPath(Series series, int seasonNumber, FileSortingOptions options)
        {
            // If there's already a season folder, use that
            var season = series
                .RecursiveChildren
                .OfType<Season>()
                .FirstOrDefault(i => i.LocationType == LocationType.FileSystem && i.IndexNumber.HasValue && i.IndexNumber.Value == seasonNumber);

            if (season != null)
            {
                return season.Path;
            }

            var path = series.Path;

            if (series.ContainsEpisodesWithoutSeasonFolders)
            {
                return path;
            }

            if (seasonNumber == 0)
            {
                return Path.Combine(path, _fileSystem.GetValidFilename(options.SeasonZeroFolderName));
            }

            var seasonFolderName = options.SeasonFolderPattern
                .Replace("%s", seasonNumber.ToString(UsCulture))
                .Replace("%0s", seasonNumber.ToString("00", UsCulture))
                .Replace("%00s", seasonNumber.ToString("000", UsCulture));

            return Path.Combine(path, _fileSystem.GetValidFilename(seasonFolderName));
        }

        /// <summary>
        /// Gets the matching series.
        /// </summary>
        /// <param name="seriesName">Name of the series.</param>
        /// <param name="allSeries">All series.</param>
        /// <returns>Series.</returns>
        private Series GetMatchingSeries(string seriesName, IEnumerable<Series> allSeries)
        {
            int? yearInName;
            var nameWithoutYear = seriesName;
            NameParser.ParseName(nameWithoutYear, out nameWithoutYear, out yearInName);

            return allSeries.Select(i => GetMatchScore(nameWithoutYear, yearInName, i))
                .Where(i => i.Item2 > 0)
                .OrderByDescending(i => i.Item2)
                .Select(i => i.Item1)
                .FirstOrDefault();
        }

        private Tuple<Series, int> GetMatchScore(string sortedName, int? year, Series series)
        {
            var score = 0;

            // TODO: Improve this
            if (string.Equals(sortedName, series.Name, StringComparison.OrdinalIgnoreCase))
            {
                score++;

                if (year.HasValue && series.ProductionYear.HasValue)
                {
                    if (year.Value == series.ProductionYear.Value)
                    {
                        score++;
                    }
                    else
                    {
                        // Regardless of name, return a 0 score if the years don't match
                        return new Tuple<Series, int>(series, 0);
                    }
                }
            }

            return new Tuple<Series, int>(series, score);
        }

        /// <summary>
        /// Deletes the left over files.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="extensions">The extensions.</param>
        private void DeleteLeftOverFiles(string path, IEnumerable<string> extensions)
        {
            var eligibleFiles = new DirectoryInfo(path)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(i => extensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in eligibleFiles)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch (IOException ex)
                {
                    _logger.ErrorException("Error deleting file {0}", ex, file.FullName);
                }
            }
        }

        /// <summary>
        /// Deletes the empty folders.
        /// </summary>
        /// <param name="path">The path.</param>
        private void DeleteEmptyFolders(string path)
        {
            try
            {
                foreach (var d in Directory.EnumerateDirectories(path))
                {
                    DeleteEmptyFolders(d);
                }

                var entries = Directory.EnumerateFileSystemEntries(path);

                if (!entries.Any())
                {
                    try
                    {
                        Directory.Delete(path);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
        }
    }
}
