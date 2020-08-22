#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

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

    public interface ISupportsLatestMedia
    {
        /// <summary>
        /// Gets the latest media.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelItemInfo}}.</returns>
        Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken);
    }

    public interface ISupportsDelete
    {
        bool CanDelete(BaseItem item);

        Task DeleteItem(string id, CancellationToken cancellationToken);
    }

    public interface IDisableMediaSourceDisplay
    {
    }

    public interface ISupportsMediaProbe
    {
    }

    public interface IHasFolderAttributes
    {
        string[] Attributes { get; }
    }
}
