using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public class EpisodeInfo : ItemLookupInfo, IHasIdentities<EpisodeIdentity>
    {
        private List<EpisodeIdentity> _identities = new List<EpisodeIdentity>();

        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public int? IndexNumberEnd { get; set; }
        public int? AnimeSeriesIndex { get; set; }

        public EpisodeInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<EpisodeIdentity> Identities
        {
            get { return _identities; }
        }

        public async Task FindIdentities(IProviderManager providerManager, CancellationToken cancellationToken)
        {
            var identifier = new ItemIdentifier<EpisodeInfo, EpisodeIdentity>();
            _identities = (await identifier.FindIdentities(this, providerManager, cancellationToken)).ToList();
        }
    }
}