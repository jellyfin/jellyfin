using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface IRemoteMetadataProvider.
    /// </summary>
    public interface IRemoteMetadataProvider : IMetadataProvider
    {
    }

    /// <summary>
    /// Interface IRemoteMetadataProvider.
    /// </summary>
    /// <typeparam name="TItemType">The type of <see cref="BaseItem" />.</typeparam>
    /// <typeparam name="TLookupInfoType">The type of <see cref="ItemLookupInfo" />.</typeparam>
    public interface IRemoteMetadataProvider<TItemType, in TLookupInfoType> : IMetadataProvider<TItemType>, IRemoteMetadataProvider, IRemoteSearchProvider<TLookupInfoType>
        where TItemType : BaseItem, IHasLookupInfo<TLookupInfoType>
        where TLookupInfoType : ItemLookupInfo, new()
    {
        /// <summary>
        /// Gets the metadata for a specific LookupInfoType.
        /// </summary>
        /// <param name="info">The LookupInfoType to get metadata for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A task returning a MetadataResult for the specific LookupInfoType.</returns>
        Task<MetadataResult<TItemType>> GetMetadata(TLookupInfoType info, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Interface IRemoteMetadataProvider.
    /// </summary>
    /// <typeparam name="TLookupInfoType">The type of <see cref="ItemLookupInfo" />.</typeparam>
    public interface IRemoteSearchProvider<in TLookupInfoType> : IRemoteSearchProvider
        where TLookupInfoType : ItemLookupInfo
    {
        /// <summary>
        /// Gets the list of <see cref="RemoteSearchResult"/> for a specific LookupInfoType.
        /// </summary>
        /// <param name="searchInfo">The LookupInfoType to search for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A task returning RemoteSearchResults for the searchInfo.</returns>
        Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TLookupInfoType searchInfo, CancellationToken cancellationToken);
    }
}
