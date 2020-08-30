using System.Threading.Tasks;
using Emby.Server.Implementations.Events.ConsumerArgs;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Events;

namespace Emby.Server.Implementations.Events.Consumers.Library
{
    /// <summary>
    /// Adds base item to notification queue when item is added to library.
    /// </summary>
    public class BaseItemAddedNotifier : IEventConsumer<BaseItemAddedEventArgs>
    {
        private readonly BaseItemAddedNotifierQueue _notifierQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemAddedNotifier"/> class.
        /// </summary>
        /// <param name="notifierQueue">The <see cref="BaseItemAddedNotifierQueue"/>.</param>
        public BaseItemAddedNotifier(BaseItemAddedNotifierQueue notifierQueue)
        {
            _notifierQueue = notifierQueue;
        }

        /// <inheritdoc />
        public Task OnEvent(BaseItemAddedEventArgs eventArgs)
        {
            if (!FilterItem(eventArgs.Argument))
            {
                return Task.CompletedTask;
            }

            _notifierQueue.EnqueueItem(eventArgs.Argument);
            return Task.CompletedTask;
        }

        private static bool FilterItem(BaseItem item)
        {
            if (item.IsFolder)
            {
                return false;
            }

            if (!item.HasPathProtocol)
            {
                return false;
            }

            if (item is IItemByName)
            {
                return false;
            }

            return item.SourceType == SourceType.Library;
        }
    }
}
