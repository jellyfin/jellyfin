using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelDownloadScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IChannelManager _manager;
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        public ChannelDownloadScheduledTask(IChannelManager manager, IServerConfigurationManager config, ILogger logger, IHttpClient httpClient, IFileSystem fileSystem, ILibraryManager libraryManager, IUserManager userManager)
        {
            _manager = manager;
            _config = config;
            _logger = logger;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        public string Name
        {
            get { return "Download channel content"; }
        }

        public string Description
        {
            get { return "Downloads channel content based on configuration."; }
        }

        public string Category
        {
            get { return "Channels"; }
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CleanChannelContent(cancellationToken);

            var users = _userManager.Users.Select(i => i.Id.ToString("N")).ToList();

            var numComplete = 0;

            foreach (var user in users)
            {
                double percentPerUser = 1;
                percentPerUser /= users.Count;
                var startingPercent = numComplete * percentPerUser * 100;

                var innerProgress = new ActionableProgress<double>();
                innerProgress.RegisterAction(p => progress.Report(startingPercent + (.8 * p)));

                await DownloadContent(user, cancellationToken, innerProgress).ConfigureAwait(false);

                numComplete++;
                double percent = numComplete;
                percent /= users.Count;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        private async Task DownloadContent(string user,
            CancellationToken cancellationToken,
            IProgress<double> progress)
        {
            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(0 + (.8 * p)));
            await DownloadAllChannelContent(user, cancellationToken, innerProgress).ConfigureAwait(false);
            progress.Report(80);

            innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(80 + (.2 * p)));
            await DownloadLatestChannelContent(user, cancellationToken, progress).ConfigureAwait(false);
            progress.Report(100);
        }

        private async Task DownloadLatestChannelContent(string userId,
            CancellationToken cancellationToken,
            IProgress<double> progress)
        {
            var result = await _manager.GetLatestChannelItems(new AllChannelMediaQuery
            {
                UserId = userId

            }, cancellationToken).ConfigureAwait(false);

            progress.Report(5);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(5 + (.95 * p)));

            var path = _manager.ChannelDownloadPath;

            await DownloadChannelContent(result, path, cancellationToken, innerProgress).ConfigureAwait(false);
        }

        private async Task DownloadAllChannelContent(string userId,
            CancellationToken cancellationToken,
            IProgress<double> progress)
        {
            var result = await _manager.GetAllMedia(new AllChannelMediaQuery
            {
                UserId = userId

            }, cancellationToken).ConfigureAwait(false);

            progress.Report(5);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(5 + (.95 * p)));

            var path = _manager.ChannelDownloadPath;

            await DownloadChannelContent(result, path, cancellationToken, innerProgress).ConfigureAwait(false);
        }

        private async Task DownloadChannelContent(QueryResult<BaseItemDto> result,
            string path,
            CancellationToken cancellationToken,
            IProgress<double> progress)
        {
            var numComplete = 0;

            foreach (var item in result.Items)
            {
                if (_config.Configuration.ChannelOptions.DownloadingChannels.Contains(item.ChannelId))
                {
                    try
                    {
                        await DownloadChannelItem(item, cancellationToken, path);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error downloading channel content for {0}", ex, item.Name);
                    }
                }

                numComplete++;
                double percent = numComplete;
                percent /= result.Items.Length;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        private async Task DownloadChannelItem(BaseItemDto item,
            CancellationToken cancellationToken,
            string path)
        {
            var sources = await _manager.GetChannelItemMediaSources(item.Id, cancellationToken)
                .ConfigureAwait(false);

            var list = sources.ToList();

            var cachedVersions = list.Where(i => i.Protocol == MediaProtocol.File).ToList();

            if (cachedVersions.Count > 0)
            {
                await RefreshMediaSourceItems(cachedVersions, cancellationToken).ConfigureAwait(false);
                return;
            }

            var source = list.FirstOrDefault(i => i.Protocol == MediaProtocol.Http);

            if (source == null)
            {
                return;
            }

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = source.Path,
                Progress = new Progress<double>()
            };

            foreach (var header in source.RequiredHttpHeaders)
            {
                options.RequestHeaders[header.Key] = header.Value;
            }

            var destination = Path.Combine(path, item.ChannelId, item.Id);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));

            // Determine output extension
            var response = await _httpClient.GetTempFileResponse(options).ConfigureAwait(false);

            if (item.IsVideo && response.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                var extension = response.ContentType.Split('/')
                        .Last();

                destination += "." + extension;
            }
            else if (item.IsAudio && response.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                var extension = response.ContentType.Replace("audio/mpeg", "audio/mp3", StringComparison.OrdinalIgnoreCase)
                        .Split('/')
                        .Last();

                destination += "." + extension;
            }
            else
            {
                File.Delete(response.TempFilePath);

                throw new ApplicationException("Unexpected response type encountered: " + response.ContentType);
            }

            File.Move(response.TempFilePath, destination);

            await RefreshMediaSourceItem(destination, cancellationToken).ConfigureAwait(false);

            try
            {
                File.Delete(response.TempFilePath);
            }
            catch
            {
                
            }
        }

        private async Task RefreshMediaSourceItems(IEnumerable<MediaSourceInfo> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                await RefreshMediaSourceItem(item.Path, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RefreshMediaSourceItem(string path, CancellationToken cancellationToken)
        {
            var item = _libraryManager.ResolvePath(new FileInfo(path));

            if (item != null)
            {
                // Get the version from the database
                item = _libraryManager.GetItemById(item.Id) ?? item;

                await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
            }
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new IntervalTrigger{ Interval = TimeSpan.FromHours(6)},
                };
        }

        private void CleanChannelContent(CancellationToken cancellationToken)
        {
            if (!_config.Configuration.ChannelOptions.MaxDownloadAge.HasValue)
            {
                return;
            }

            var minDateModified = DateTime.UtcNow.AddDays(0 - _config.Configuration.ChannelOptions.MaxDownloadAge.Value);

            var path = _manager.ChannelDownloadPath;

            try
            {
                DeleteCacheFilesFromDirectory(cancellationToken, path, minDateModified, new Progress<double>());
            }
            catch (DirectoryNotFoundException)
            {
                // No biggie here. Nothing to delete
            }
        }

        /// <summary>
        /// Deletes the cache files from directory with a last write time less than a given date
        /// </summary>
        /// <param name="cancellationToken">The task cancellation token.</param>
        /// <param name="directory">The directory.</param>
        /// <param name="minDateModified">The min date modified.</param>
        /// <param name="progress">The progress.</param>
        private void DeleteCacheFilesFromDirectory(CancellationToken cancellationToken, string directory, DateTime minDateModified, IProgress<double> progress)
        {
            var filesToDelete = new DirectoryInfo(directory).EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(f => _fileSystem.GetLastWriteTimeUtc(f) < minDateModified)
                .ToList();

            var index = 0;

            foreach (var file in filesToDelete)
            {
                double percent = index;
                percent /= filesToDelete.Count;

                progress.Report(100 * percent);

                cancellationToken.ThrowIfCancellationRequested();

                DeleteFile(file.FullName);

                index++;
            }

            progress.Report(100);
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="path">The path.</param>
        private void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                _logger.ErrorException("Error deleting file {0}", ex, path);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is hidden.
        /// </summary>
        /// <value><c>true</c> if this instance is hidden; otherwise, <c>false</c>.</value>
        public bool IsHidden
        {
            get
            {
                return !_manager.GetAllChannelFeatures().Any();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is enabled; otherwise, <c>false</c>.</value>
        public bool IsEnabled
        {
            get
            {
                return true;
            }
        }
    }
}
