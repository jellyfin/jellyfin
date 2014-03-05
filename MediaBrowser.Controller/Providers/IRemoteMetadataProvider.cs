using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IRemoteMetadataProvider : IMetadataProvider
    {
    }

    public interface IRemoteMetadataProvider<TItemType, in TLookupInfoType> : IMetadataProvider<TItemType>, IRemoteMetadataProvider, IRemoteSearchProvider<TLookupInfoType>
        where TItemType : IHasMetadata, IHasLookupInfo<TLookupInfoType>
        where TLookupInfoType : ItemLookupInfo, new()
    {
        Task<MetadataResult<TItemType>> GetMetadata(TLookupInfoType info, CancellationToken cancellationToken);
    }

    public interface IRemoteSearchProvider : IMetadataProvider
    {
        /// <summary>
        /// Gets the image response.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken);
    }

    public interface IRemoteSearchProvider<in TLookupInfoType> : IRemoteSearchProvider
        where TLookupInfoType : ItemLookupInfo
    {
        Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TLookupInfoType searchInfo, CancellationToken cancellationToken);
    }
    
    public class RemoteSearchQuery<T>
        where T : ItemLookupInfo
    {
        public T SearchInfo { get; set; }

        /// <summary>
        /// If set will only search within the given provider
        /// </summary>
        public string SearchProviderName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include disabled providers].
        /// </summary>
        /// <value><c>true</c> if [include disabled providers]; otherwise, <c>false</c>.</value>
        public bool IncludeDisabledProviders { get; set; }
    }
}
