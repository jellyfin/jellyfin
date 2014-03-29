using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common;

namespace MediaBrowser.Controller.Providers
{
    public interface ISeriesOrderProvider
    {
        string OrderType { get; }
        Task<int?> FindSeriesIndex(string seriesName);
    }

    public static class SeriesOrderTypes
    {
        public const string Anime = "Anime";
    }

    public interface ISeriesOrderManager
    {
        Task<int?> FindSeriesIndex(string orderType, string seriesName);
        void AddParts(IEnumerable<ISeriesOrderProvider> orderProviders);
    }
}
