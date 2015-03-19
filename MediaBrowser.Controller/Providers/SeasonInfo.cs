using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public class SeasonInfo : ItemLookupInfo, IHasIdentities<SeasonIdentity>
    {
        private List<SeasonIdentity> _identities = new List<SeasonIdentity>();

        public Dictionary<string, string> SeriesProviderIds { get; set; }
        public int? AnimeSeriesIndex { get; set; }

        public SeasonInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<SeasonIdentity> Identities
        {
            get { return _identities; }
        }

        public async Task FindIdentities(IProviderManager providerManager, CancellationToken cancellationToken)
        {
            var identifier = new ItemIdentifier<SeasonInfo, SeasonIdentity>();
            _identities = (await identifier.FindIdentities(this, providerManager, cancellationToken)).ToList();
        }
    }
}