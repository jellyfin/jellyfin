using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Server.Implementations.FileOrganization
{
    public class TvFolderOrganizer
    {
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IFileOrganizationService _organizationService;
        private readonly IServerConfigurationManager _config;
        private readonly IProviderManager _providerManager;

        public TvFolderOrganizer(ILibraryManager libraryManager, ILogger logger, IFileSystem fileSystem, ILibraryMonitor libraryMonitor, IFileOrganizationService organizationService, IServerConfigurationManager config, IProviderManager providerManager)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _fileSystem = fileSystem;
            _libraryMonitor = libraryMonitor;
            _organizationService = organizationService;
            _config = config;
            _providerManager = providerManager;
        }

        private bool EnableOrganization(FileSystemMetadata fileInfo, TvFileOrganizationOptions options)
        {
            var minFileBytes = options.MinFileSizeMb * 1024 * 1024;

            try
            {
                return _libraryManager.IsVideoFile(fileInfo.FullName) && fileInfo.Length >= minFileBytes;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error organizing file {0}", ex, fileInfo.Name);
            }

            return false;
        }

        public async Task Organize(AutoOrganizeOptions options, CancellationToken cancellationToken, IProgress<double> progress)
        {
            var watchLocations = options.TvOptions.WatchLocations.ToList();

            var eligibleFiles = watchLocations.SelectMany(GetFilesToOrganize)
                .OrderBy(_fileSystem.GetCreationTimeUtc)
                .Where(i => EnableOrganization(i, options.TvOptions))
                .ToList();

            var processedFolders = new HashSet<string>();

            progress.Report(10);

            if (eligibleFiles.Count > 0)
            {
                var numComplete = 0;

                foreach (var file in eligibleFiles)
                {
                    var organizer = new EpisodeFileOrganizer(_organizationService, _config, _fileSystem, _logger, _libraryManager,
                        _libraryMonitor, _providerManager);

                    try
                    {
                        var result = await organizer.OrganizeEpisodeFile(file.FullName, options, options.TvOptions.OverwriteExistingEpisodes, cancellationToken).ConfigureAwait(false);
                        if (result.Status == FileSortingStatus.Success && !processedFolders.Contains(file.DirectoryName, StringComparer.OrdinalIgnoreCase))
                        {
                            processedFolders.Add(file.DirectoryName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error organizing episode {0}", ex, file.FullName);
                    }

                    numComplete++;
                    double percent = numComplete;
                    percent /= eligibleFiles.Count;

                    progress.Report(10 + 89 * percent);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(99);

            foreach (var path in processedFolders)
            {
                var deleteExtensions = options.TvOptions.LeftOverFileExtensionsToDelete
                    .Select(i => i.Trim().TrimStart('.'))
                    .Where(i => !string.IsNullOrEmpty(i))
                    .Select(i => "." + i)
                    .ToList();

                if (deleteExtensions.Count > 0)
                {
                    DeleteLeftOverFiles(path, deleteExtensions);
                }

                if (options.TvOptions.DeleteEmptyFolders)
                {
                    if (!IsWatchFolder(path, watchLocations))
                    {
                        DeleteEmptyFolders(path);
                    }
                }
            }

            progress.Report(100);
        }

        /// <summary>
        /// Gets the files to organize.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{FileInfo}.</returns>
        private List<FileSystemMetadata> GetFilesToOrganize(string path)
        {
            try
            {
                return _fileSystem.GetFiles(path, true)
                    .ToList();
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error getting files from {0}", ex, path);

                return new List<FileSystemMetadata>();
            }
        }

        /// <summary>
        /// Deletes the left over files.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="extensions">The extensions.</param>
        private void DeleteLeftOverFiles(string path, IEnumerable<string> extensions)
        {
            var eligibleFiles = _fileSystem.GetFiles(path, true)
                .Where(i => extensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in eligibleFiles)
            {
                try
                {
                    _fileSystem.DeleteFile(file.FullName);
                }
                catch (Exception ex)
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
                foreach (var d in _fileSystem.GetDirectoryPaths(path))
                {
                    DeleteEmptyFolders(d);
                }

                var entries = _fileSystem.GetFileSystemEntryPaths(path);

                if (!entries.Any())
                {
                    try
                    {
                        _logger.Debug("Deleting empty directory {0}", path);
                        _fileSystem.DeleteDirectory(path, false);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// Determines if a given folder path is contained in a folder list
        /// </summary>
        /// <param name="path">The folder path to check.</param>
        /// <param name="watchLocations">A list of folders.</param>
        private bool IsWatchFolder(string path, IEnumerable<string> watchLocations)
        {
            return watchLocations.Contains(path, StringComparer.OrdinalIgnoreCase);
        }
    }
}