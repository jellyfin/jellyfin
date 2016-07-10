
using System;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasSeries
    {
        /// <summary>
        /// Gets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        string SeriesName { get; set; }
        string FindSeriesName();
        string SeriesSortName { get; set; }
        string FindSeriesSortName();
        Guid? SeriesId { get; set; }
        Guid? FindSeriesId();
    }
}
