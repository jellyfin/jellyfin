using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.Library;
using MediaBrowser.Server.Implementations.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

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

        public Task<FileOrganizationResult> OrganizeEpisodeFile(string path, CancellationToken cancellationToken)
        {
            var options = _config.GetAutoOrganizeOptions();

            return OrganizeEpisodeFile(path, options, false, cancellationToken);
        }

        public async Task<FileOrganizationResult> OrganizeEpisodeFile(string path, AutoOrganizeOptions options, bool overwriteExisting, CancellationToken cancellationToken)
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

            if (_libraryMonitor.IsPathLocked(path))
            {
                result.Status = FileSortingStatus.Failure;
                result.StatusMessage = "Path is locked by other processes. Please try again later.";
                return result;
            }

            var namingOptions = ((LibraryManager)_libraryManager).GetNamingOptions();
            var resolver = new Naming.TV.EpisodeResolver(namingOptions, new PatternsLogger());

            var episodeInfo = resolver.Resolve(path, false) ??
                new Naming.TV.EpisodeInfo();

            var seriesName = episodeInfo.SeriesName;

            if (!string.IsNullOrEmpty(seriesName))
            {
                var seasonNumber = episodeInfo.SeasonNumber;

                result.ExtractedSeasonNumber = seasonNumber;

                // Passing in true will include a few extra regex's
                var episodeNumber = episodeInfo.EpisodeNumber;

                result.ExtractedEpisodeNumber = episodeNumber;

                var premiereDate = episodeInfo.IsByDate ?
                    new DateTime(episodeInfo.Year.Value, episodeInfo.Month.Value, episodeInfo.Day.Value) :
                    (DateTime?)null;

                if (episodeInfo.IsByDate || (seasonNumber.HasValue && episodeNumber.HasValue))
                {
                    if (episodeInfo.IsByDate)
                    {
                        _logger.Debug("Extracted information from {0}. Series name {1}, Date {2}", path, seriesName, premiereDate.Value);
                    }
                    else
                    {
                        _logger.Debug("Extracted information from {0}. Series name {1}, Season {2}, Episode {3}", path, seriesName, seasonNumber, episodeNumber);
                    }

                    var endingEpisodeNumber = episodeInfo.EndingEpsiodeNumber;

                    result.ExtractedEndingEpisodeNumber = endingEpisodeNumber;

                    await OrganizeEpisode(path,
                        seriesName,
                        seasonNumber,
                        episodeNumber,
                        endingEpisodeNumber,
                        premiereDate,
                        options,
                        overwriteExisting,
                        false,
                        result,
                        cancellationToken).ConfigureAwait(false);
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
                var msg = string.Format("Unable to determine series name from {0}", path);
                result.Status = FileSortingStatus.Failure;
                result.StatusMessage = msg;
                _logger.Warn(msg);
            }

            var previousResult = _organizationService.GetResultBySourcePath(path);

            if (previousResult != null)
            {
                // Don't keep saving the same result over and over if nothing has changed
                if (previousResult.Status == result.Status && previousResult.StatusMessage == result.StatusMessage && result.Status != FileSortingStatus.Success)
                {
                    return previousResult;
                }
            }

            await _organizationService.SaveResult(result, CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        public async Task<FileOrganizationResult> OrganizeWithCorrection(EpisodeFileOrganizationRequest request, AutoOrganizeOptions options, CancellationToken cancellationToken)
        {
            var result = _organizationService.GetResult(request.ResultId);

            Series series = null;

            if (request.NewSeriesProviderIds.Count > 0)
            {
                // We're having a new series here
                SeriesInfo seriesRequest = new SeriesInfo();
                seriesRequest.ProviderIds = request.NewSeriesProviderIds;

                var refreshOptions = new MetadataRefreshOptions(_fileSystem);
                series = new Series();
                series.Id = Guid.NewGuid();
                series.Name = request.NewSeriesName;

                int year;
                if (int.TryParse(request.NewSeriesYear, out year))
                {
                    series.ProductionYear = year;
                }

                var seriesFolderName = series.Name;
                if (series.ProductionYear.HasValue)
                {
                    seriesFolderName = string.Format("{0} ({1})", seriesFolderName, series.ProductionYear);
                }

                series.Path = Path.Combine(request.TargetFolder, seriesFolderName);

                series.ProviderIds = request.NewSeriesProviderIds;

                await series.RefreshMetadata(refreshOptions, cancellationToken);
            }

            if (series == null)
            {
                // Existing Series
                series = (Series)_libraryManager.GetItemById(new Guid(request.SeriesId));
            }

            await OrganizeEpisode(result.OriginalPath,
                series,
                request.SeasonNumber,
                request.EpisodeNumber,
                request.EndingEpisodeNumber,
                null,
                options,
                true,
                request.RememberCorrection,
                result,
                cancellationToken).ConfigureAwait(false);

            await _organizationService.SaveResult(result, CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        private Task OrganizeEpisode(string sourcePath,
            string seriesName,
            int? seasonNumber,
            int? episodeNumber,
            int? endingEpiosdeNumber,
            DateTime? premiereDate,
            AutoOrganizeOptions options,
            bool overwriteExisting,
            bool rememberCorrection,
            FileOrganizationResult result,
            CancellationToken cancellationToken)
        {
            var series = GetMatchingSeries(seriesName, result, options);

            if (series == null)
            {
                var msg = string.Format("Unable to find series in library matching name {0}", seriesName);
                result.Status = FileSortingStatus.Failure;
                result.StatusMessage = msg;
                _logger.Warn(msg);
                return Task.FromResult(true);
            }

            return OrganizeEpisode(sourcePath,
                series,
                seasonNumber,
                episodeNumber,
                endingEpiosdeNumber,
                premiereDate,
                options,
                overwriteExisting,
                rememberCorrection,
                result,
                cancellationToken);
        }

        private async Task OrganizeEpisode(string sourcePath,
            Series series,
            int? seasonNumber,
            int? episodeNumber,
            int? endingEpiosdeNumber,
            DateTime? premiereDate,
            AutoOrganizeOptions options,
            bool overwriteExisting,
            bool rememberCorrection,
            FileOrganizationResult result,
            CancellationToken cancellationToken)
        {
            _logger.Info("Sorting file {0} into series {1}", sourcePath, series.Path);

            var originalExtractedSeriesString = result.ExtractedName;

            // Proceed to sort the file
            var newPath = await GetNewPath(sourcePath, series, seasonNumber, episodeNumber, endingEpiosdeNumber, premiereDate, options.TvOptions, cancellationToken).ConfigureAwait(false);

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

            var fileExists = _fileSystem.FileExists(result.TargetPath);
            var otherDuplicatePaths = GetOtherDuplicatePaths(result.TargetPath, series, seasonNumber, episodeNumber, endingEpiosdeNumber);

            if (!overwriteExisting)
            {
                if (options.TvOptions.CopyOriginalFile && fileExists && IsSameEpisode(sourcePath, newPath))
                {
                    var msg = string.Format("File '{0}' already copied to new path '{1}', stopping organization", sourcePath, newPath);
                    _logger.Info(msg);
                    result.Status = FileSortingStatus.SkippedExisting;
                    result.StatusMessage = msg;
                    return;
                }

                if (fileExists)
                {
                    var msg = string.Format("File '{0}' already exists as '{1}', stopping organization", sourcePath, newPath);
                    _logger.Info(msg);
                    result.Status = FileSortingStatus.SkippedExisting;
                    result.StatusMessage = msg;
                    result.TargetPath = newPath;
                    return;
                }

                if (otherDuplicatePaths.Count > 0)
                {
                    var msg = string.Format("File '{0}' already exists as these:'{1}'. Stopping organization", sourcePath, string.Join("', '", otherDuplicatePaths));
                    _logger.Info(msg);
                    result.Status = FileSortingStatus.SkippedExisting;
                    result.StatusMessage = msg;
                    result.DuplicatePaths = otherDuplicatePaths;
                    return;
                }
            }

            PerformFileSorting(options.TvOptions, result);

            if (overwriteExisting)
            {
                var hasRenamedFiles = false;

                foreach (var path in otherDuplicatePaths)
                {
                    _logger.Debug("Removing duplicate episode {0}", path);

                    _libraryMonitor.ReportFileSystemChangeBeginning(path);

                    var renameRelatedFiles = !hasRenamedFiles &&
                        string.Equals(Path.GetDirectoryName(path), Path.GetDirectoryName(result.TargetPath), StringComparison.OrdinalIgnoreCase);

                    if (renameRelatedFiles)
                    {
                        hasRenamedFiles = true;
                    }

                    try
                    {
                        DeleteLibraryFile(path, renameRelatedFiles, result.TargetPath);
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

            if (rememberCorrection)
            {
                SaveSmartMatchString(originalExtractedSeriesString, series, options);
            }
        }

        private void SaveSmartMatchString(string matchString, Series series, AutoOrganizeOptions options)
        {
            if (string.IsNullOrEmpty(matchString) || matchString.Length < 3)
            {
                return;
            }

            SmartMatchInfo info = options.SmartMatchInfos.FirstOrDefault(i => string.Equals(i.ItemName, series.Name, StringComparison.OrdinalIgnoreCase));

            if (info == null)
            {
                info = new SmartMatchInfo();
                info.ItemName = series.Name;
                info.OrganizerType = FileOrganizerType.Episode;
                info.DisplayName = series.Name;
                var list = options.SmartMatchInfos.ToList();
                list.Add(info);
                options.SmartMatchInfos = list.ToArray();
            }

            if (!info.MatchStrings.Contains(matchString, StringComparer.OrdinalIgnoreCase))
            {
                var list = info.MatchStrings.ToList();
                list.Add(matchString);
                info.MatchStrings = list.ToArray();
                _config.SaveAutoOrganizeOptions(options);
            }
        }

        private void DeleteLibraryFile(string path, bool renameRelatedFiles, string targetPath)
        {
            _fileSystem.DeleteFile(path);

            if (!renameRelatedFiles)
            {
                return;
            }

            // Now find other files
            var originalFilenameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(originalFilenameWithoutExtension) && !string.IsNullOrWhiteSpace(directory))
            {
                // Get all related files, e.g. metadata, images, etc
                var files = _fileSystem.GetFilePaths(directory)
                    .Where(i => (Path.GetFileNameWithoutExtension(i) ?? string.Empty).StartsWith(originalFilenameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var targetFilenameWithoutExtension = Path.GetFileNameWithoutExtension(targetPath);

                foreach (var file in files)
                {
                    directory = Path.GetDirectoryName(file);
                    var filename = Path.GetFileName(file);

                    filename = filename.Replace(originalFilenameWithoutExtension, targetFilenameWithoutExtension,
                        StringComparison.OrdinalIgnoreCase);

                    var destination = Path.Combine(directory, filename);

                    _fileSystem.MoveFile(file, destination);
                }
            }
        }

        private List<string> GetOtherDuplicatePaths(string targetPath,
            Series series,
            int? seasonNumber,
            int? episodeNumber,
            int? endingEpisodeNumber)
        {
            // TODO: Support date-naming?
            if (!seasonNumber.HasValue || !episodeNumber.HasValue)
            {
                return new List<string>();
            }

            var episodePaths = series.GetRecursiveChildren()
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
            var targetFileNameWithoutExtension = _fileSystem.GetFileNameWithoutExtension(targetPath);

            try
            {
                var filesOfOtherExtensions = _fileSystem.GetFilePaths(folder)
                    .Where(i => _libraryManager.IsVideoFile(i) && string.Equals(_fileSystem.GetFileNameWithoutExtension(i), targetFileNameWithoutExtension, StringComparison.OrdinalIgnoreCase));

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

            _fileSystem.CreateDirectory(Path.GetDirectoryName(result.TargetPath));

            var targetAlreadyExists = _fileSystem.FileExists(result.TargetPath);

            try
            {
                if (targetAlreadyExists || options.CopyOriginalFile)
                {
                    _fileSystem.CopyFile(result.OriginalPath, result.TargetPath, true);
                }
                else
                {
                    _fileSystem.MoveFile(result.OriginalPath, result.TargetPath);
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

            if (targetAlreadyExists && !options.CopyOriginalFile)
            {
                try
                {
                    _fileSystem.DeleteFile(result.OriginalPath);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting {0}", ex, result.OriginalPath);
                }
            }
        }

        private Series GetMatchingSeries(string seriesName, FileOrganizationResult result, AutoOrganizeOptions options)
        {
            var parsedName = _libraryManager.ParseName(seriesName);

            var yearInName = parsedName.Year;
            var nameWithoutYear = parsedName.Name;

            result.ExtractedName = nameWithoutYear;
            result.ExtractedYear = yearInName;

            var series = _libraryManager.GetItemList(new Controller.Entities.InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                Recursive = true
            })
                .Cast<Series>()
                .Select(i => NameUtils.GetMatchScore(nameWithoutYear, yearInName, i))
                .Where(i => i.Item2 > 0)
                .OrderByDescending(i => i.Item2)
                .Select(i => i.Item1)
                .FirstOrDefault();

            if (series == null)
            {
                SmartMatchInfo info = options.SmartMatchInfos.FirstOrDefault(e => e.MatchStrings.Contains(nameWithoutYear, StringComparer.OrdinalIgnoreCase));

                if (info != null)
                {
                    series = _libraryManager.GetItemList(new Controller.Entities.InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { typeof(Series).Name },
                        Recursive = true,
                        Name = info.ItemName

                    }).Cast<Series>().FirstOrDefault();
                }
            }

            return series;
        }

        /// <summary>
        /// Gets the new path.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="episodeNumber">The episode number.</param>
        /// <param name="endingEpisodeNumber">The ending episode number.</param>
        /// <param name="premiereDate">The premiere date.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>System.String.</returns>
        private async Task<string> GetNewPath(string sourcePath,
            Series series,
            int? seasonNumber,
            int? episodeNumber,
            int? endingEpisodeNumber,
            DateTime? premiereDate,
            TvFileOrganizationOptions options,
            CancellationToken cancellationToken)
        {
            var episodeInfo = new EpisodeInfo
            {
                IndexNumber = episodeNumber,
                IndexNumberEnd = endingEpisodeNumber,
                MetadataCountryCode = series.GetPreferredMetadataCountryCode(),
                MetadataLanguage = series.GetPreferredMetadataLanguage(),
                ParentIndexNumber = seasonNumber,
                SeriesProviderIds = series.ProviderIds,
                PremiereDate = premiereDate
            };

            var searchResults = await _providerManager.GetRemoteSearchResults<Episode, EpisodeInfo>(new RemoteSearchQuery<EpisodeInfo>
            {
                SearchInfo = episodeInfo

            }, cancellationToken).ConfigureAwait(false);

            var episode = searchResults.FirstOrDefault();

            if (episode == null)
            {
                var msg = string.Format("No provider metadata found for {0} season {1} episode {2}", series.Name, seasonNumber, episodeNumber);
                _logger.Warn(msg);
                return null;
            }

            var episodeName = episode.Name;

            //if (string.IsNullOrWhiteSpace(episodeName))
            //{
            //    var msg = string.Format("No provider metadata found for {0} season {1} episode {2}", series.Name, seasonNumber, episodeNumber);
            //    _logger.Warn(msg);
            //    return null;
            //}

            seasonNumber = seasonNumber ?? episode.ParentIndexNumber;
            episodeNumber = episodeNumber ?? episode.IndexNumber;

            var newPath = GetSeasonFolderPath(series, seasonNumber.Value, options);

            // MAX_PATH - trailing <NULL> charachter - drive component: 260 - 1 - 3 = 256
            // Usually newPath would include the drive component, but use 256 to be sure
            var maxFilenameLength = 256 - newPath.Length;

            if (!newPath.EndsWith(@"\"))
            {
                // Remove 1 for missing backslash combining path and filename
                maxFilenameLength--;
            }

            // Remove additional 4 chars to prevent PathTooLongException for downloaded subtitles (eg. filename.ext.eng.srt)
            maxFilenameLength -= 4;

            var episodeFileName = GetEpisodeFileName(sourcePath, series.Name, seasonNumber.Value, episodeNumber.Value, endingEpisodeNumber, episodeName, options, maxFilenameLength);

            if (string.IsNullOrEmpty(episodeFileName))
            {
                // cause failure
                return string.Empty;
            }

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
                .GetRecursiveChildren(i => i is Season && i.LocationType == LocationType.FileSystem && i.IndexNumber.HasValue && i.IndexNumber.Value == seasonNumber)
                .FirstOrDefault();

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

        private string GetEpisodeFileName(string sourcePath, string seriesName, int seasonNumber, int episodeNumber, int? endingEpisodeNumber, string episodeTitle, TvFileOrganizationOptions options, int? maxLength)
        {
            seriesName = _fileSystem.GetValidFilename(seriesName).Trim();

            if (string.IsNullOrWhiteSpace(episodeTitle))
            {
                episodeTitle = string.Empty;
            }
            else
            {
                episodeTitle = _fileSystem.GetValidFilename(episodeTitle).Trim();
            }

            var sourceExtension = (Path.GetExtension(sourcePath) ?? string.Empty).TrimStart('.');

            var pattern = endingEpisodeNumber.HasValue ? options.MultiEpisodeNamePattern : options.EpisodeNamePattern;

            var result = pattern.Replace("%sn", seriesName)
                .Replace("%s.n", seriesName.Replace(" ", "."))
                .Replace("%s_n", seriesName.Replace(" ", "_"))
                .Replace("%s", seasonNumber.ToString(_usCulture))
                .Replace("%0s", seasonNumber.ToString("00", _usCulture))
                .Replace("%00s", seasonNumber.ToString("000", _usCulture))
                .Replace("%ext", sourceExtension)
                .Replace("%en", "%#1")
                .Replace("%e.n", "%#2")
                .Replace("%e_n", "%#3");

            if (endingEpisodeNumber.HasValue)
            {
                result = result.Replace("%ed", endingEpisodeNumber.Value.ToString(_usCulture))
                .Replace("%0ed", endingEpisodeNumber.Value.ToString("00", _usCulture))
                .Replace("%00ed", endingEpisodeNumber.Value.ToString("000", _usCulture));
            }

            result = result.Replace("%e", episodeNumber.ToString(_usCulture))
                .Replace("%0e", episodeNumber.ToString("00", _usCulture))
                .Replace("%00e", episodeNumber.ToString("000", _usCulture));

            if (maxLength.HasValue && result.Contains("%#"))
            {
                // Substract 3 for the temp token length (%#1, %#2 or %#3)  
                int maxRemainingTitleLength = maxLength.Value - result.Length + 3;
                string shortenedEpisodeTitle = string.Empty;

                if (maxRemainingTitleLength > 5)
                {
                    // A title with fewer than 5 letters wouldn't be of much value
                    shortenedEpisodeTitle = episodeTitle.Substring(0, Math.Min(maxRemainingTitleLength, episodeTitle.Length));
                }

                result = result.Replace("%#1", shortenedEpisodeTitle)
                    .Replace("%#2", shortenedEpisodeTitle.Replace(" ", "."))
                    .Replace("%#3", shortenedEpisodeTitle.Replace(" ", "_"));
            }

            if (maxLength.HasValue && result.Length > maxLength.Value)
            {
                // There may be cases where reducing the title length may still not be sufficient to
                // stay below maxLength
                var msg = string.Format("Unable to generate an episode file name shorter than {0} characters to constrain to the max path limit", maxLength);
                _logger.Warn(msg);
                return string.Empty;
            }

            return result;
        }

        private bool IsSameEpisode(string sourcePath, string newPath)
        {
            try
            {
                var sourceFileInfo = new FileInfo(sourcePath);
                var destinationFileInfo = new FileInfo(newPath);

                if (sourceFileInfo.Length == destinationFileInfo.Length)
                {
                    return true;
                }
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }

            return false;
        }
    }
}
