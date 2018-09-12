using System;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasSeries : IHasSeriesName
    {
        /// <summary>
        /// Gets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        string FindSeriesName();
        string FindSeriesSortName();
        Guid SeriesId { get; set; }
        Guid FindSeriesId();
        string SeriesPresentationUniqueKey { get; set; }
        string FindSeriesPresentationUniqueKey();
    }

    public interface IHasSeriesName
    {
        string SeriesName { get; set; }
    }
}
