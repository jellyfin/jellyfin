using System.Collections.Generic;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Channels
{
    public class ChannelPostScanTask : ILibraryPostScanTask
    {
        private readonly IChannelManager _channelManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        public ChannelPostScanTask(IChannelManager channelManager, IUserManager userManager, ILogger logger)
        {
            _channelManager = channelManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var users = _userManager.Users
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

        private async Task DownloadContent(string user, CancellationToken cancellationToken, IProgress<double> progress)
        {
            var channels = await _channelManager.GetChannelsInternal(new ChannelQuery
            {
                UserId = user

            }, cancellationToken);

            var numComplete = 0;

            foreach (var channel in channels.Items)
            {
                var channelId = channel.Id.ToString("N");

                var features = _channelManager.GetChannelFeatures(channelId);

                const int currentRefreshLevel = 1;
                var maxRefreshLevel = features.AutoRefreshLevels ?? 1;

                try
                {
                    await GetAllItems(user, channelId, null, currentRefreshLevel, maxRefreshLevel, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting channel content", ex);
                }

                numComplete++;
                double percent = numComplete;
                percent /= channels.Items.Length;
                progress.Report(percent * 100);
            }

            progress.Report(100);

        }

        private async Task GetAllItems(string user, string channelId, string folderId, int currentRefreshLevel, int maxRefreshLevel, CancellationToken cancellationToken)
        {
            var folderItems = new List<string>();

            var result = await _channelManager.GetChannelItemsInternal(new ChannelItemQuery
            {
                ChannelId = channelId,
                UserId = user,
                FolderId = folderId

            }, cancellationToken);

            folderItems.AddRange(result.Items.Where(i => i.IsFolder).Select(i => i.Id.ToString("N")));

            var totalRetrieved = result.Items.Length;
            var totalCount = result.TotalRecordCount;

            while (totalRetrieved < totalCount)
            {
                result = await _channelManager.GetChannelItemsInternal(new ChannelItemQuery
                {
                    ChannelId = channelId,
                    UserId = user,
                    StartIndex = totalRetrieved,
                    FolderId = folderId

                }, cancellationToken);

                folderItems.AddRange(result.Items.Where(i => i.IsFolder).Select(i => i.Id.ToString("N")));
                
                totalRetrieved += result.Items.Length;
                totalCount = result.TotalRecordCount;
            }

            if (currentRefreshLevel < maxRefreshLevel)
            {
                foreach (var folder in folderItems)
                {
                    try
                    {
                        await GetAllItems(user, channelId, folder, currentRefreshLevel + 1, maxRefreshLevel, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting channel content", ex);
                    }
                }
            }
        }
    }
}
