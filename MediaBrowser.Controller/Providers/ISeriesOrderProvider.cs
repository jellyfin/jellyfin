using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    public interface ISeriesOrderProvider
    {
        string OrderType { get; }
        Task<int?> FindSeriesIndex(string seriesName);
    }
}