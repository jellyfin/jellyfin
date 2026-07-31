using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.Events.Consumers.Users
{
    /// <summary>
    /// Provisions the default list when a user is created.
    /// </summary>
    public class ItemListProvisioner : IEventConsumer<UserCreatedEventArgs>
    {
        private readonly IItemListManager _itemListManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemListProvisioner"/> class.
        /// </summary>
        /// <param name="itemListManager">The item list manager.</param>
        public ItemListProvisioner(IItemListManager itemListManager)
        {
            _itemListManager = itemListManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(UserCreatedEventArgs eventArgs)
        {
            await _itemListManager.GetOrCreateDefaultListAsync(eventArgs.Argument.Id).ConfigureAwait(false);
        }
    }
}
