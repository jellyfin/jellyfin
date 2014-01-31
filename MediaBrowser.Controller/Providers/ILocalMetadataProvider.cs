using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface ILocalMetadataProvider : IMetadataProvider
    {
        /// <summary>
        /// Determines whether [has local metadata] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local metadata] [the specified item]; otherwise, <c>false</c>.</returns>
        bool HasLocalMetadata(IHasMetadata item);
    }

    public interface ILocalMetadataProvider<TItemType> : IMetadataProvider<TItemType>, ILocalMetadataProvider
         where TItemType : IHasMetadata
    {
        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MetadataResult{`0}}.</returns>
        Task<MetadataResult<TItemType>> GetMetadata(string path, CancellationToken cancellationToken);
    }
}
