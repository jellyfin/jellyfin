using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IRemoteMetadataProvider : IMetadataProvider
    {
    }

    public interface IRemoteMetadataProvider<TItemType, TLookupInfoType> : IMetadataProvider<TItemType>, IRemoteMetadataProvider
        where TItemType : IHasMetadata, IHasLookupInfo<TLookupInfoType>
        where TLookupInfoType : ItemLookupInfo, new()
    {
        Task<MetadataResult<TItemType>> GetMetadata(TLookupInfoType info, CancellationToken cancellationToken);
    }

    public interface IRemoteSearchProvider<TLookupInfoType>
        where TLookupInfoType : ItemLookupInfo
    {
        Task<IEnumerable<SearchResult<TLookupInfoType>>> GetSearchResults(TLookupInfoType searchInfo, CancellationToken cancellationToken);
    }
    
    public class SearchResult<T>
        where T : ItemLookupInfo
    {
        public T Item { get; set; }

        public string ImageUrl { get; set; }
    }
}
