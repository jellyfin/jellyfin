using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IRemoteMetadataProvider : IMetadataProvider
    {
    }

    public interface IRemoteMetadataProvider<TItemType> : IMetadataProvider<TItemType>, IRemoteMetadataProvider
        where TItemType : IHasMetadata
    {
        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MetadataResult{`0}}.</returns>
        Task<MetadataResult<TItemType>> GetMetadata(ItemId id, CancellationToken cancellationToken);
    }
}
