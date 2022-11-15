#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Channels
{
    public interface ISearchableChannel
    {
        /// <summary>
        /// Searches the specified search term.
        /// </summary>
        /// <param name="searchInfo">The search information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelItemInfo}}.</returns>
        Task<IEnumerable<ChannelItemInfo>> Search(ChannelSearchInfo searchInfo, CancellationToken cancellationToken);
    }
}
