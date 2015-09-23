using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IItemIdentityProvider<in TLookupInfo> : IItemIdentityProvider
        where TLookupInfo : ItemLookupInfo
    {
        Task Identify(TLookupInfo info);
    }

    public interface IItemIdentityConverter<in TLookupInfo> : IItemIdentityConverter
        where TLookupInfo : ItemLookupInfo
    {
        Task<bool> Convert(TLookupInfo info);
    }
}