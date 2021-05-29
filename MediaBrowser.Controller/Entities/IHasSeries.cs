#nullable disable

#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasSeries
    {
        /// <summary>
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        string SeriesName { get; set; }

        Guid SeriesId { get; set; }

        string SeriesPresentationUniqueKey { get; set; }

        string FindSeriesName();

        string FindSeriesSortName();

        Guid FindSeriesId();

        string FindSeriesPresentationUniqueKey();
    }
}
