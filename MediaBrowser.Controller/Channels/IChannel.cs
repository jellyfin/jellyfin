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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelItemInfo}}.</returns>
        Task<IEnumerable<ChannelItemInfo>> Search(string searchTerm, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelItem}}.</returns>
        Task<IEnumerable<ChannelItemInfo>> GetChannelItems(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="categoryId">The category identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelItem}}.</returns>
        Task<IEnumerable<ChannelItemInfo>> GetChannelItems(string categoryId, CancellationToken cancellationToken);
    }

    public class ChannelCapabilities
    {
        public bool CanSearch { get; set; }

        public bool CanBeIndexed { get; set; }
    }
}
