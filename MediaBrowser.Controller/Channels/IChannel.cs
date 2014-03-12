using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannel
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the home page URL.
        /// </summary>
        /// <value>The home page URL.</value>
        string HomePageUrl { get; }

        /// <summary>
        /// Gets the capabilities.
        /// </summary>
        /// <returns>ChannelCapabilities.</returns>
        ChannelCapabilities GetCapabilities();

        /// <summary>
        /// Searches the specified search term.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelItemInfo}}.</returns>
        Task<IEnumerable<ChannelItemInfo>> Search(string searchTerm, User user, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelItem}}.</returns>
        Task<IEnumerable<ChannelItemInfo>> GetChannelItems(User user, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="categoryId">The category identifier.</param>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelItem}}.</returns>
        Task<IEnumerable<ChannelItemInfo>> GetChannelItems(string categoryId, User user, CancellationToken cancellationToken);
    }

    public class ChannelCapabilities
    {
        public bool CanSearch { get; set; }
    }
}
