using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.FileSorting
{
    public class TvFileSorter
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        public TvFileSorter(ILibraryManager libraryManager, ILogger logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        public void Sort(string path, FileSortingOptions options)
        {
            var minFileBytes = options.MinFileSizeMb * 1024 * 1024;

            var allSeries = _libraryManager.RootFolder
                .RecursiveChildren.OfType<Series>()
                .Where(i => i.LocationType == LocationType.FileSystem)
                .ToList();

            var eligibleFiles = new DirectoryInfo(path)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(i => EntityResolutionHelper.IsVideoFile(i.FullName) && i.Length >= minFileBytes)
                .ToList();

            foreach (var file in eligibleFiles)
            {
                SortFile(file.FullName, options, allSeries);
            }

            if (options.LeftOverFileExtensionsToDelete.Length > 0)
            {
                DeleteLeftOverFiles(path, options.LeftOverFileExtensionsToDelete);
            }

            if (options.DeleteEmptyFolders)
            {
                DeleteEmptyFolders(path);
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
        }

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

            if (year.HasValue)
            {
                if (series.ProductionYear.HasValue && year.Value == series.ProductionYear.Value)
                {
                    score++;
                }
            }

            // TODO: Improve this
            if (string.Equals(sortedName, series.Name, StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }

            return new Tuple<Series, int>(series, score);
        }

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
