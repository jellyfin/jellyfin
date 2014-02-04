using MediaBrowser.Controller.Library;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface ICustomMetadataProvider : IMetadataProvider
    {
    }

    public interface ICustomMetadataProvider<TItemType> : IMetadataProvider<TItemType>, ICustomMetadataProvider
        where TItemType : IHasMetadata
    {
        Task<ItemUpdateType> FetchAsync(TItemType item, CancellationToken cancellationToken);
    }
}
