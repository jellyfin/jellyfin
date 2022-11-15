#pragma warning disable CS1591

using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Providers
{
    public interface ICustomMetadataProvider : IMetadataProvider
    {
    }

    public interface ICustomMetadataProvider<TItemType> : IMetadataProvider<TItemType>, ICustomMetadataProvider
        where TItemType : BaseItem
    {
        /// <summary>
        /// Fetches the metadata asynchronously.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The <see cref="MetadataRefreshOptions"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Task"/> fetching the <see cref="ItemUpdateType"/>.</returns>
        Task<ItemUpdateType> FetchAsync(TItemType item, MetadataRefreshOptions options, CancellationToken cancellationToken);
    }
}
