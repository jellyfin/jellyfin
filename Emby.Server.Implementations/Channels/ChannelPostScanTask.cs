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
    /// <summary>
    /// A task to remove all non-installed channels from the database.
    /// </summary>
    public class ChannelPostScanTask
    {
        private readonly IChannelManager _channelManager;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelPostScanTask"/> class.
        /// </summary>
        /// <param name="channelManager">The channel manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        public ChannelPostScanTask(IChannelManager channelManager, ILogger logger, ILibraryManager libraryManager)
        {
            _channelManager = channelManager;
            _logger = logger;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Runs this task.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The completed task.</returns>
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
