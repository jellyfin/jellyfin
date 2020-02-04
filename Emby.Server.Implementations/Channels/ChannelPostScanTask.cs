#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Channels
{
    public class ChannelPostScanTask
    {
        private readonly IChannelManager _channelManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public ChannelPostScanTask(IChannelManager channelManager, IUserManager userManager, ILogger logger, ILibraryManager libraryManager)
        {
            _channelManager = channelManager;
            _userManager = userManager;
            _logger = logger;
            _libraryManager = libraryManager;
        }

        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            CleanDatabase(cancellationToken);

            progress.Report(100);
            return Task.CompletedTask;
        }

        private void CleanDatabase(CancellationToken cancellationToken)
        {
            var installedChannelIds = ((ChannelManager)_channelManager).GetInstalledChannelIds();

            var uninstalledChannels = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Channel).Name },
                ExcludeItemIds = installedChannelIds.ToArray()
            });

            foreach (var channel in uninstalledChannels)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CleanChannel((Channel)channel, cancellationToken);
            }
        }

        private void CleanChannel(Channel channel, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cleaning channel {0} from database", channel.Id);

            // Delete all channel items
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                ChannelIds = new[] { channel.Id }
            });

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _libraryManager.DeleteItem(
                    item,
                    new DeleteOptions
                    {
                        DeleteFileLocation = false
                    },
                    false);
            }

            // Finally, delete the channel itself
            _libraryManager.DeleteItem(
                channel,
                new DeleteOptions
                {
                    DeleteFileLocation = false
                },
                false);
        }
    }
}
