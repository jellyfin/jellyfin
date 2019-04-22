using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Controller.Entities;
using Jellyfin.Model.Providers;

namespace Jellyfin.Controller.Providers
{
    public interface IRemoteMetadataProvider : IMetadataProvider
    {
    }

    public interface IRemoteMetadataProvider<TItemType, in TLookupInfoType> : IMetadataProvider<TItemType>, IRemoteMetadataProvider, IRemoteSearchProvider<TLookupInfoType>
        where TItemType : BaseItem, IHasLookupInfo<TLookupInfoType>
        where TLookupInfoType : ItemLookupInfo, new()
    {
        Task<MetadataResult<TItemType>> GetMetadata(TLookupInfoType info, CancellationToken cancellationToken);
    }

    public interface IRemoteSearchProvider<in TLookupInfoType> : IRemoteSearchProvider
        where TLookupInfoType : ItemLookupInfo
    {
        Task<IEnumerable<RemoteSearchResult>> GetSearchResults(TLookupInfoType searchInfo, CancellationToken cancellationToken);
    }
}
