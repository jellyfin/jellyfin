using MediaBrowser.Common.IO;
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

        public async Task Organize(TvFileOrganizationOptions options, CancellationToken cancellationToken, IProgress<double> progress)
        {
            var minFileBytes = options.MinFileSizeMb * 1024 * 1024;

            var watchLocations = options.WatchLocations.ToList();

            var eligibleFiles = watchLocations.SelectMany(GetFilesToOrganize)
                .OrderBy(_fileSystem.GetCreationTimeUtc)
                .Where(i => _libraryManager.IsVideoFile(i.FullName) && i.Length >= minFileBytes)
                .ToList();

            progress.Report(10);

            var scanLibrary = false;

            if (eligibleFiles.Count > 0)
            {
                var numComplete = 0;

                foreach (var file in eligibleFiles)
                {
                    var organizer = new EpisodeFileOrganizer(_organizationService, _config, _fileSystem, _logger, _libraryManager,
                        _libraryMonitor, _providerManager);

                    var result = await organizer.OrganizeEpisodeFile(file.FullName, options, options.OverwriteExistingEpisodes, cancellationToken).ConfigureAwait(false);

                    if (result.Status == FileSortingStatus.Success)
                    {
                        scanLibrary = true;
                    }

                    numComplete++;
                    double percent = numComplete;
                    percent /= eligibleFiles.Count;

                    progress.Report(10 + (89 * percent));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(99);

            foreach (var path in watchLocations)
            {
                var deleteExtensions = options.LeftOverFileExtensionsToDelete
                    .Select(i => i.Trim().TrimStart('.'))
                    .Where(i => !string.IsNullOrEmpty(i))
                    .Select(i => "." + i)
                    .ToList();

                if (deleteExtensions.Count > 0)
                {
                    DeleteLeftOverFiles(path, deleteExtensions);
                }

                if (options.DeleteEmptyFolders)
                {
                    foreach (var subfolder in GetDirectories(path).ToList())
                    {
                        DeleteEmptyFolders(subfolder);
                    }
                }
            }

            if (scanLibrary)
            {
                await _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None)
                        .ConfigureAwait(false);
            }

            progress.Report(100);
        }

        /// <summary>
        /// Gets the directories.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> GetDirectories(string path)
        {
            try
            {
                return Directory
                    .EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly)
                    .ToList();
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error getting files from {0}", ex, path);

                return new List<string>();
            }
        }

        /// <summary>
        /// Gets the files to organize.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{FileInfo}.</returns>
        private IEnumerable<FileInfo> GetFilesToOrganize(string path)
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
                foreach (var d in Directory.EnumerateDirectories(path))
                {
                    DeleteEmptyFolders(d);
                }

                var entries = Directory.EnumerateFileSystemEntries(path);

                if (!entries.Any())
                {
                    try
                    {
                        _logger.Debug("Deleting empty directory {0}", path);
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
