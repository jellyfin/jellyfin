using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.FileOrganization
{
    public class EpisodeFileOrganizer
    {
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IFileOrganizationService _organizationService;
        private readonly IServerConfigurationManager _config;
        private readonly IProviderManager _providerManager;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public EpisodeFileOrganizer(IFileOrganizationService organizationService, IServerConfigurationManager config, IFileSystem fileSystem, ILogger logger, ILibraryManager libraryManager, ILibraryMonitor libraryMonitor, IProviderManager providerManager)
        {
            _organizationService = organizationService;
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
            _libraryManager = libraryManager;
            _libraryMonitor = libraryMonitor;
            _providerManager = providerManager;
        }

        public async Task<FileOrganizationResult> OrganizeEpisodeFile(string path, TvFileOrganizationOptions options, bool overwriteExisting, CancellationToken cancellationToken)
        {
            _logger.Info("Sorting file {0}", path);

            var result = new FileOrganizationResult
            {
                Date = DateTime.UtcNow,
                OriginalPath = path,
                OriginalFileName = Path.GetFileName(path),
                Type = FileOrganizerType.Episode,
                FileSize = new FileInfo(path).Length
            };

            var seriesName = TVUtils.GetSeriesNameFromEpisodeFile(path);

            if (!string.IsNullOrEmpty(seriesName))
            {
                var season = TVUtils.GetSeasonNumberFromEpisodeFile(path);

                result.ExtractedSeasonNumber = season;

                if (season.HasValue)
                {
                    // Passing in true will include a few extra regex's
                    var episode = TVUtils.GetEpisodeNumberFromFile(path, true);

                    result.ExtractedEpisodeNumber = episode;

                    if (episode.HasValue)
                    {
                        _logger.Debug("Extracted information from {0}. Series name {1}, Season {2}, Episode {3}", path, seriesName, season, episode);

                        var endingEpisodeNumber = TVUtils.GetEndingEpisodeNumberFromFile(path);

                        result.ExtractedEndingEpisodeNumber = endingEpisodeNumber;

                        await OrganizeEpisode(path, seriesName, season.Value, episode.Value, endingEpisodeNumber, options, overwriteExisting, result, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var msg = string.Format("Unable to determine episode number from {0}", path);
                        result.Status = FileSortingStatus.Failure;
                        result.StatusMessage = msg;
                        _logger.Warn(msg);
                    }
                }
                else
                {
                    var msg = string.Format("Unable to determine season number from {0}", path);
                    result.Status = FileSortingStatus.Failure;
                    result.StatusMessage = msg;
                    _logger.Warn(msg);
                }
            }
            else
            {
                var msg = string.Format("Unable to determine series name from {0}", path);
                result.Status = FileSortingStatus.Failure;
                result.StatusMessage = msg;
                _logger.Warn(msg);
            }

            var previousResult = _organizationService.GetResultBySourcePath(path);

            if (previousResult != null)
            {
                // Don't keep saving the same result over and over if nothing has changed
                if (previousResult.Status == result.Status && result.Status != FileSortingStatus.Success)
                {
                    return previousResult;
                }
            }

            await _organizationService.SaveResult(result, CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        public async Task<FileOrganizationResult> OrganizeWithCorrection(EpisodeFileOrganizationRequest request, TvFileOrganizationOptions options, CancellationToken cancellationToken)
        {
            var result = _organizationService.GetResult(request.ResultId);

            var series = (Series)_libraryManager.GetItemById(new Guid(request.SeriesId));

            await OrganizeEpisode(result.OriginalPath, series, request.SeasonNumber, request.EpisodeNumber, request.EndingEpisodeNumber, _config.Configuration.TvFileOrganizationOptions, true, result, cancellationToken).ConfigureAwait(false);

            await _organizationService.SaveResult(result, CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        private Task OrganizeEpisode(string sourcePath, string seriesName, int seasonNumber, int episodeNumber, int? endingEpiosdeNumber, TvFileOrganizationOptions options, bool overwriteExisting, FileOrganizationResult result, CancellationToken cancellationToken)
        {
            var series = GetMatchingSeries(seriesName, result);

            if (series == null)
            {
                var msg = string.Format("Unable to find series in library matching name {0}", seriesName);
                result.Status = FileSortingStatus.Failure;
                result.StatusMessage = msg;
                _logger.Warn(msg);
                return Task.FromResult(true);
            }

            return OrganizeEpisode(sourcePath, series, seasonNumber, episodeNumber, endingEpiosdeNumber, options, overwriteExisting, result, cancellationToken);
        }

        private async Task OrganizeEpisode(string sourcePath, Series series, int seasonNumber, int episodeNumber, int? endingEpiosdeNumber, TvFileOrganizationOptions options, bool overwriteExisting, FileOrganizationResult result, CancellationToken cancellationToken)
        {
            _logger.Info("Sorting file {0} into series {1}", sourcePath, series.Path);

            // Proceed to sort the file
            var newPath = await GetNewPath(sourcePath, series, seasonNumber, episodeNumber, endingEpiosdeNumber, options, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(newPath))
            {
                var msg = string.Format("Unable to sort {0} because target path could not be determined.", sourcePath);
                result.Status = FileSortingStatus.Failure;
                result.StatusMessage = msg;
                _logger.Warn(msg);
                return;
            }

            _logger.Info("Sorting file {0} to new path {1}", sourcePath, newPath);
            result.TargetPath = newPath;

            var fileExists = File.Exists(result.TargetPath);
            var otherDuplicatePaths = GetOtherDuplicatePaths(result.TargetPath, series, seasonNumber, episodeNumber, endingEpiosdeNumber);

            if (!overwriteExisting)
            {
                if (options.CopyOriginalFile && fileExists && IsSameEpisode(sourcePath, newPath))
                {
                    _logger.Info("File {0} already copied to new path {1}, stopping organization", sourcePath, newPath);
                    result.Status = FileSortingStatus.SkippedExisting;
                    result.StatusMessage = string.Empty;
                    return;
                }
                
                if (fileExists || otherDuplicatePaths.Count > 0)
                {
                    result.Status = FileSortingStatus.SkippedExisting;
                    result.StatusMessage = string.Empty;
                    result.DuplicatePaths = otherDuplicatePaths;
                    return;
                }
            }

            PerformFileSorting(options, result);

            if (overwriteExisting)
            {
                foreach (var path in otherDuplicatePaths)
                {
                    _logger.Debug("Removing duplicate episode {0}", path);

                    _libraryMonitor.ReportFileSystemChangeBeginning(path);

                    try
                    {
                        File.Delete(path);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error removing duplicate episode", ex, path);
                    }
                    finally
                    {
                        _libraryMonitor.ReportFileSystemChangeComplete(path, true);
                    }
                }
            }
        }

        private List<string> GetOtherDuplicatePaths(string targetPath, Series series, int seasonNumber, int episodeNumber, int? endingEpisodeNumber)
        {
            var episodePaths = series.RecursiveChildren
                .OfType<Episode>()
                .Where(i =>
                {
                    var locationType = i.LocationType;

                    // Must be file system based and match exactly
                    if (locationType != LocationType.Remote &&
                        locationType != LocationType.Virtual &&
                        i.ParentIndexNumber.HasValue &&
                        i.ParentIndexNumber.Value == seasonNumber &&
                        i.IndexNumber.HasValue &&
                        i.IndexNumber.Value == episodeNumber)
                    {

                        if (endingEpisodeNumber.HasValue || i.IndexNumberEnd.HasValue)
                        {
                            return endingEpisodeNumber.HasValue && i.IndexNumberEnd.HasValue &&
                                   endingEpisodeNumber.Value == i.IndexNumberEnd.Value;
                        }

                        return true;
                    }

                    return false;
                })
                .Select(i => i.Path)
                .ToList();

            var folder = Path.GetDirectoryName(targetPath);
            var targetFileNameWithoutExtension = Path.GetFileNameWithoutExtension(targetPath);

            try
            {
                var filesOfOtherExtensions = Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)
                    .Where(i => EntityResolutionHelper.IsVideoFile(i) && string.Equals(Path.GetFileNameWithoutExtension(i), targetFileNameWithoutExtension, StringComparison.OrdinalIgnoreCase));

                episodePaths.AddRange(filesOfOtherExtensions);
            }
            catch (DirectoryNotFoundException)
            {
                // No big deal. Maybe the season folder doesn't already exist.
            }

            return episodePaths.Where(i => !string.Equals(i, targetPath, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void PerformFileSorting(TvFileOrganizationOptions options, FileOrganizationResult result)
        {
            _libraryMonitor.ReportFileSystemChangeBeginning(result.TargetPath);

            Directory.CreateDirectory(Path.GetDirectoryName(result.TargetPath));

            var copy = File.Exists(result.TargetPath);

            try
            {
                if (copy || options.CopyOriginalFile)
                {
                    File.Copy(result.OriginalPath, result.TargetPath, true);
                }
                else
                {
                    File.Move(result.OriginalPath, result.TargetPath);
                }

                result.Status = FileSortingStatus.Success;
                result.StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                var errorMsg = string.Format("Failed to move file from {0} to {1}", result.OriginalPath, result.TargetPath);

                result.Status = FileSortingStatus.Failure;
                result.StatusMessage = errorMsg;
                _logger.ErrorException(errorMsg, ex);

                return;
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(result.TargetPath, true);
            }

            if (copy && !options.CopyOriginalFile)
            {
                try
                {
                    File.Delete(result.OriginalPath);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting {0}", ex, result.OriginalPath);
                }
            }
        }

        private Series GetMatchingSeries(string seriesName, FileOrganizationResult result)
        {
            int? yearInName;
            var nameWithoutYear = seriesName;
            NameParser.ParseName(nameWithoutYear, out nameWithoutYear, out yearInName);

            result.ExtractedName = nameWithoutYear;
            result.ExtractedYear = yearInName;

            return _libraryManager.RootFolder.RecursiveChildren
                .OfType<Series>()
                .Select(i => NameUtils.GetMatchScore(nameWithoutYear, yearInName, i))
                .Where(i => i.Item2 > 0)
                .OrderByDescending(i => i.Item2)
                .Select(i => i.Item1)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the new path.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="episodeNumber">The episode number.</param>
        /// <param name="endingEpisodeNumber">The ending episode number.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        private async Task<string> GetNewPath(string sourcePath, Series series, int seasonNumber, int episodeNumber, int? endingEpisodeNumber, TvFileOrganizationOptions options, CancellationToken cancellationToken)
        {
            var episodeInfo = new EpisodeInfo
            {
                IndexNumber = episodeNumber,
                IndexNumberEnd = endingEpisodeNumber,
                MetadataCountryCode = series.GetPreferredMetadataCountryCode(),
                MetadataLanguage = series.GetPreferredMetadataLanguage(),
                ParentIndexNumber = seasonNumber,
                SeriesProviderIds = series.ProviderIds
            };

            var searchResults = await _providerManager.GetRemoteSearchResults<Episode, EpisodeInfo>(new RemoteSearchQuery<EpisodeInfo>
            {
                SearchInfo = episodeInfo

            }, cancellationToken).ConfigureAwait(false);

            var episode = searchResults.FirstOrDefault();

            if (episode == null)
            {
                return null;
            }

            var newPath = GetSeasonFolderPath(series, seasonNumber, options);

            var episodeFileName = GetEpisodeFileName(sourcePath, series.Name, seasonNumber, episodeNumber, endingEpisodeNumber, episode.Name, options);

            newPath = Path.Combine(newPath, episodeFileName);

            return newPath;
        }

        /// <summary>
        /// Gets the season folder path.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        private string GetSeasonFolderPath(Series series, int seasonNumber, TvFileOrganizationOptions options)
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
                .Replace("%s", seasonNumber.ToString(_usCulture))
                .Replace("%0s", seasonNumber.ToString("00", _usCulture))
                .Replace("%00s", seasonNumber.ToString("000", _usCulture));

            return Path.Combine(path, _fileSystem.GetValidFilename(seasonFolderName));
        }

        private string GetEpisodeFileName(string sourcePath, string seriesName, int seasonNumber, int episodeNumber, int? endingEpisodeNumber, string episodeTitle, TvFileOrganizationOptions options)
        {
            seriesName = _fileSystem.GetValidFilename(seriesName).Trim();
            episodeTitle = _fileSystem.GetValidFilename(episodeTitle).Trim();

            var sourceExtension = (Path.GetExtension(sourcePath) ?? string.Empty).TrimStart('.');

            var pattern = endingEpisodeNumber.HasValue ? options.MultiEpisodeNamePattern : options.EpisodeNamePattern;

            var result = pattern.Replace("%sn", seriesName)
                .Replace("%s.n", seriesName.Replace(" ", "."))
                .Replace("%s_n", seriesName.Replace(" ", "_"))
                .Replace("%s", seasonNumber.ToString(_usCulture))
                .Replace("%0s", seasonNumber.ToString("00", _usCulture))
                .Replace("%00s", seasonNumber.ToString("000", _usCulture))
                .Replace("%ext", sourceExtension)
                .Replace("%en", episodeTitle)
                .Replace("%e.n", episodeTitle.Replace(" ", "."))
                .Replace("%e_n", episodeTitle.Replace(" ", "_"));

            if (endingEpisodeNumber.HasValue)
            {
                result = result.Replace("%ed", endingEpisodeNumber.Value.ToString(_usCulture))
                .Replace("%0ed", endingEpisodeNumber.Value.ToString("00", _usCulture))
                .Replace("%00ed", endingEpisodeNumber.Value.ToString("000", _usCulture));
            }

            return result.Replace("%e", episodeNumber.ToString(_usCulture))
                .Replace("%0e", episodeNumber.ToString("00", _usCulture))
                .Replace("%00e", episodeNumber.ToString("000", _usCulture));
        }

        private bool IsSameEpisode(string sourcePath, string newPath)
        {

                FileInfo sourceFileInfo = new FileInfo(sourcePath);
                FileInfo destinationFileInfo = new FileInfo(newPath);

                try
                {
                    if (sourceFileInfo.Length == destinationFileInfo.Length)
                    {
                        return true;
                    }
                }
                catch (FileNotFoundException)
                {
                    return false;
                }

                return false;

        }
    }
}
