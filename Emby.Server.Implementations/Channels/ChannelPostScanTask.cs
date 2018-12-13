using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public static string GetUserDistinctValue(User user)
        {
            var channels = user.Policy.EnabledChannels
                .OrderBy(i => i)
                .ToList();

            return string.Join("|", channels.ToArray());
        }

        private void CleanDatabase(CancellationToken cancellationToken)
        {
            var installedChannelIds = ((ChannelManager)_channelManager).GetInstalledChannelIds();

            var databaseIds = _libraryManager.GetItemIds(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Channel).Name }
            });

            var invalidIds = databaseIds
                .Except(installedChannelIds)
                .ToList();

            foreach (var id in invalidIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CleanChannel(id, cancellationToken);
            }
        }

        private void CleanChannel(Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cleaning channel {0} from database", id);

            // Delete all channel items
            var allIds = _libraryManager.GetItemIds(new InternalItemsQuery
            {
                ChannelIds = new[] { id }
            });

            foreach (var deleteId in allIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DeleteItem(deleteId);
            }

            // Finally, delete the channel itself
            DeleteItem(id);
        }

        private void DeleteItem(Guid id)
        {
            var item = _libraryManager.GetItemById(id);

            if (item == null)
            {
                return;
            }

            _libraryManager.DeleteItem(item, new DeleteOptions
            {
                DeleteFileLocation = false

            }, false);
        }
    }
}
