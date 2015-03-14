using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public class SeriesInfo : ItemLookupInfo, IHasIdentities<SeriesIdentity>
    {
        private List<SeriesIdentity> _identities = new List<SeriesIdentity>();

        public int? AnimeSeriesIndex { get; set; }

        public IEnumerable<SeriesIdentity> Identities
        {
            get { return _identities; }
        }

        public async Task FindIdentities(IProviderManager providerManager, CancellationToken cancellationToken)
        {
            var identifier = new ItemIdentifier<SeriesInfo, SeriesIdentity>();
            _identities = (await identifier.FindIdentities(this, providerManager, cancellationToken)).ToList();
        }
    }
}