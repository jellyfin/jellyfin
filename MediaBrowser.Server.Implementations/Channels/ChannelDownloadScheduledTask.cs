using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Querying;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly ISecurityManager _security;

        public ChannelDownloadScheduledTask(IChannelManager manager, IServerConfigurationManager config, ILogger logger, IHttpClient httpClient, IFileSystem fileSystem, ILibraryManager libraryManager, IUserManager userManager, ISecurityManager security)
        {
            _manager = manager;
            _config = config;
            _logger = logger;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _security = security;
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

            var users = _userManager.Users
                .DistinctBy(GetUserDistinctValue)
                .Select(i => i.Id.ToString("N"))
                .ToList();

            var numComplete = 0;

            foreach (var user in users)
            {
                double percentPerUser = 1;
                percentPerUser /= users.Count;
                var startingPercent = numComplete * percentPerUser * 100;

                var innerProgress = new ActionableProgress<double>();
                innerProgress.RegisterAction(p => progress.Report(startingPercent + (percentPerUser * p)));

                await DownloadContent(user, cancellationToken, innerProgress).ConfigureAwait(false);

                numComplete++;
                double percent = numComplete;
                percent /= users.Count;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        public static string GetUserDistinctValue(User user)
        {
            var channels = user.Configuration.BlockedChannels
                .OrderBy(i => i)
                .ToList();

            return string.Join("|", channels.ToArray());
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
            var result = await _manager.GetLatestChannelItemsInternal(new AllChannelMediaQuery
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
            var result = await _manager.GetAllMediaInternal(new AllChannelMediaQuery
            {
                UserId = userId

            }, cancellationToken).ConfigureAwait(false);

            progress.Report(5);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report(5 + (.95 * p)));

            var path = _manager.ChannelDownloadPath;

            await DownloadChannelContent(result, path, cancellationToken, innerProgress).ConfigureAwait(false);
        }

        private async Task DownloadChannelContent(QueryResult<BaseItem> result,
            string path,
            CancellationToken cancellationToken,
            IProgress<double> progress)
        {
            var numComplete = 0;

            var options = _config.GetChannelsConfiguration();

            foreach (var item in result.Items)
            {
                var channelItem = (IChannelItem)item;
                if (options.DownloadingChannels.Contains(channelItem.ChannelId))
                {
                    try
                    {
                        await DownloadChannelItem(item, options, cancellationToken, path);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ChannelDownloadException)
                    {
                        // Logged at lower levels
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

        private double? GetDownloadLimit(ChannelOptions channelOptions)
        {
            return channelOptions.DownloadSizeLimit;
        }

        private async Task DownloadChannelItem(BaseItem item,
            ChannelOptions channelOptions,
            CancellationToken cancellationToken,
            string path)
        {
            var limit = GetDownloadLimit(channelOptions);

            if (limit.HasValue)
            {
                if (IsSizeLimitReached(path, limit.Value))
                {
                    return;
                }
            }

            var itemId = item.Id.ToString("N");
            var sources = await _manager.GetChannelItemMediaSources(itemId, false, cancellationToken)
                .ConfigureAwait(false);

            var cachedVersions = sources.Where(i => i.Protocol == MediaProtocol.File).ToList();

            if (cachedVersions.Count > 0)
            {
                await RefreshMediaSourceItems(cachedVersions, cancellationToken).ConfigureAwait(false);
                return;
            }

            var channelItem = (IChannelMediaItem)item;

            var destination = Path.Combine(path, channelItem.ChannelId, itemId);

            await _manager.DownloadChannelItem(channelItem, destination, new Progress<double>(), cancellationToken)
                    .ConfigureAwait(false);

            await RefreshMediaSourceItem(destination, cancellationToken).ConfigureAwait(false);
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

        private bool IsSizeLimitReached(string path, double gbLimit)
        {
            try
            {
                var byteLimit = gbLimit * 1000000000;

                long total = 0;

                foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    total += file.Length;

                    if (total >= byteLimit)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new IntervalTrigger{ Interval = TimeSpan.FromHours(3)},
                };
        }

        private void CleanChannelContent(CancellationToken cancellationToken)
        {
            var options = _config.GetChannelsConfiguration();

            if (!options.MaxDownloadAge.HasValue)
            {
                return;
            }

            var minDateModified = DateTime.UtcNow.AddDays(0 - options.MaxDownloadAge.Value);

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
