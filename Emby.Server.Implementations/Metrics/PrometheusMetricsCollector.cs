using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Metrics;
using Prometheus;
using Prometheus.DotNetRuntime;

namespace Emby.Server.Implementations.Metrics
{
    /// <summary>
    /// Prometheus backend for metrics.
    /// </summary>
    public class PrometheusMetricsCollector : IMetricsCollector
    {
        private readonly ILibraryManager _libraryManager;

        private static readonly Gauge _itemCountGauge = Prometheus.Metrics.CreateGauge("jellyfin_library_items_total", "The number of items in the library", new[] { "item_type" });
        private static readonly string[] _metricTypes = new string[]
        {
            nameof(Movie),
            nameof(Series), nameof(Season), nameof(Episode),
            nameof(MusicArtist), nameof(MusicAlbum), nameof(MusicVideo), nameof(Audio),
            nameof(Book),
            nameof(PhotoAlbum), nameof(Photo)
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="PrometheusMetricsCollector" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager</param>
        public PrometheusMetricsCollector(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
            _libraryManager.ItemAdded += IncrementItemCount;
            _libraryManager.ItemRemoved += (s, e) => UpdateItemCount();
        }

        /// <inheritdoc />
        public void Initialize()
        {
            DotNetRuntimeStatsBuilder.Default().StartCollecting();
            UpdateItemCount();
        }

        private void UpdateItemCount()
        {
            foreach (var type in _metricTypes)
            {
                var query = new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { type }
                };
                int count = _libraryManager.GetCount(query);
                _itemCountGauge.WithLabels(type).Set(count);
            }
        }

        private void IncrementItemCount(object? sender, ItemChangeEventArgs e)
        {
            var item = e.Item;
            var typeName = item.GetType().Name;
            if (_metricTypes.Contains(typeName))
            {
                _itemCountGauge.WithLabels(typeName).Inc();
            }
        }
    }
}
