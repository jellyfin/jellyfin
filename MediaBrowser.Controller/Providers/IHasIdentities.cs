using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface IHasIdentities<out TIdentity>
        where TIdentity : IItemIdentity
    {
        IEnumerable<TIdentity> Identities { get; }

        Task FindIdentities(IProviderManager providerManager, CancellationToken cancellationToken);
    }
}