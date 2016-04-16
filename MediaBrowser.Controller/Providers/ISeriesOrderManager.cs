using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface ISeriesOrderManager
    {
        Task<int?> FindSeriesIndex(string orderType, string seriesName);
        void AddParts(IEnumerable<ISeriesOrderProvider> orderProviders);
    }
}
