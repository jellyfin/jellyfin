using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.Providers.Manager
{
    public class SeriesOrderManager : ISeriesOrderManager
    {
        private Dictionary<string, ISeriesOrderProvider[]> _providers;

        public void AddParts(IEnumerable<ISeriesOrderProvider> orderProviders)
        {
            _providers = orderProviders
                .GroupBy(p => p.OrderType)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public async Task<int?> FindSeriesIndex(string orderType, string seriesName)
        {
            ISeriesOrderProvider[] providers;
            if (!_providers.TryGetValue(orderType, out providers))
                return null;

            foreach (ISeriesOrderProvider provider in providers)
            {
                int? index = await provider.FindSeriesIndex(seriesName);
                if (index != null)
                    return index;
            }

            return null;
        }
    }
}