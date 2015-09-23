using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public static class ItemIdentifier<TLookupInfo>
        where TLookupInfo : ItemLookupInfo
    {
        public static async Task FindIdentities(TLookupInfo item, IProviderManager providerManager, CancellationToken cancellationToken)
        {
            var providers = providerManager.GetItemIdentityProviders<TLookupInfo>();
            var converters = providerManager.GetItemIdentityConverters<TLookupInfo>().ToList();
            
            foreach (var provider in providers)
            {
                await provider.Identify(item);
            }

            bool changesMade = true;

            while (changesMade)
            {
                changesMade = false;

                foreach (var converter in converters)
                {
                    if (await converter.Convert(item))
                    {
                        changesMade = true;
                    }
                }
            }
        }
    }
}