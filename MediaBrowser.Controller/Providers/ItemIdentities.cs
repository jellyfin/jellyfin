using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IItemIdentity
    {
        string Type { get; }
    }

    public interface IHasIdentities<out TIdentity>
        where TIdentity : IItemIdentity
    {
        IEnumerable<TIdentity> Identities { get; }

        Task FindIdentities(IProviderManager providerManager, CancellationToken cancellationToken);
    }

    public interface IItemIdentityProvider : IHasOrder { }

    public interface IItemIdentityProvider<in TLookupInfo, TIdentity> : IItemIdentityProvider
        where TLookupInfo : ItemLookupInfo
        where TIdentity : IItemIdentity
    {
        Task<TIdentity> FindIdentity(TLookupInfo info);
    }

    public interface IItemIdentityConverter : IHasOrder { }

    public interface IItemIdentityConverter<TIdentity> : IItemIdentityConverter
        where TIdentity : IItemIdentity
    {
        Task<TIdentity> Convert(TIdentity identity);

        string SourceType { get; }

        string ResultType { get; }
    }
}