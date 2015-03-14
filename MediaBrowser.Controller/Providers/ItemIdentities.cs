using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IItemIdentityProvider<in TLookupInfo, TIdentity> : IItemIdentityProvider
        where TLookupInfo : ItemLookupInfo
        where TIdentity : IItemIdentity
    {
        Task<TIdentity> FindIdentity(TLookupInfo info);
    }

    public interface IItemIdentityConverter<TIdentity> : IItemIdentityConverter
        where TIdentity : IItemIdentity
    {
        Task<TIdentity> Convert(TIdentity identity);

        string SourceType { get; }

        string ResultType { get; }
    }
}