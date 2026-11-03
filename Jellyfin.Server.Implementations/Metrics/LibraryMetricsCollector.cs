using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Collects library and media related Prometheus metrics.
    /// </summary>
    public sealed class LibraryMetricsCollector
    {
        private const char LabelSeparator = '\x1f';
        private static readonly TimeSpan _codecRefreshInterval = TimeSpan.FromMinutes(5);

        private readonly ILibraryManager _libraryManager;
        private readonly IItemCountService _itemCountService;
        private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
        private readonly HashSet<string> _libraryKeys = new();
        private readonly HashSet<string> _itemKeys = new();
        private readonly HashSet<string> _codecKeys = new();
        private DateTime _lastCodecRefresh = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryMetricsCollector"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="itemCountService">The item count service.</param>
        /// <param name="dbProvider">The database context factory.</param>
        public LibraryMetricsCollector(
            ILibraryManager libraryManager,
            IItemCountService itemCountService,
            IDbContextFactory<JellyfinDbContext> dbProvider)
        {
            _libraryManager = libraryManager;
            _itemCountService = itemCountService;
            _dbProvider = dbProvider;
        }

        /// <summary>
        /// Refreshes library and media gauges from the current library state.
        /// </summary>
        public void RefreshGauges()
        {
            RefreshLibraries();
            RefreshLibraryItems();
            RefreshCodecs();
        }

        private void RefreshLibraries()
        {
            var libraries = _libraryManager.GetVirtualFolders()
                .GroupBy(f => f.CollectionType?.ToString() ?? "unknown")
                .ToDictionary(g => g.Key, g => (double)g.Count());
            SetSnapshot(JellyfinMetrics.Libraries, libraries, _libraryKeys);
        }

        private void RefreshLibraryItems()
        {
            var counts = _itemCountService.GetItemCounts(new InternalItemsQuery());
            var items = new Dictionary<string, double>
            {
                ["movie"] = counts.MovieCount,
                ["series"] = counts.SeriesCount,
                ["episode"] = counts.EpisodeCount,
                ["album"] = counts.AlbumCount,
                ["song"] = counts.SongCount,
                ["artist"] = counts.ArtistCount,
                ["music_video"] = counts.MusicVideoCount,
                ["book"] = counts.BookCount,
                ["box_set"] = counts.BoxSetCount,
                ["trailer"] = counts.TrailerCount,
                ["program"] = counts.ProgramCount,
            };
            SetSnapshot(JellyfinMetrics.LibraryItems, items, _itemKeys);
        }

        private void RefreshCodecs()
        {
            // Codec aggregation scans all media streams, so refresh it on a slower cadence.
            if (DateTime.UtcNow - _lastCodecRefresh < _codecRefreshInterval)
            {
                return;
            }

            using var dbContext = _dbProvider.CreateDbContext();
            var codecCounts = dbContext.MediaStreamInfos
                .AsNoTracking()
                .Where(s => s.Codec != null)
                .GroupBy(s => new { s.StreamType, s.Codec })
                .Select(g => new { g.Key.StreamType, Codec = g.Key.Codec!, Count = g.Count() })
                .ToArray();

            var codecs = new Dictionary<string, double>();
            foreach (var row in codecCounts)
            {
                var kind = ((MediaStreamType)row.StreamType).ToString().ToLowerInvariant();
                var codec = Sanitize(row.Codec).ToLowerInvariant();
                codecs[Join(kind, codec)] = row.Count;
            }

            SetSnapshot(JellyfinMetrics.MediaStreams, codecs, _codecKeys);
            _lastCodecRefresh = DateTime.UtcNow;
        }

        private static void SetSnapshot(Gauge gauge, Dictionary<string, double> snapshot, HashSet<string> previousKeys)
        {
            foreach (var (key, value) in snapshot)
            {
                gauge.WithLabels(Split(key)).Set(value);
            }

            // Zero out label combinations that disappeared since the last refresh.
            foreach (var key in previousKeys)
            {
                if (!snapshot.ContainsKey(key))
                {
                    gauge.WithLabels(Split(key)).Set(0);
                }
            }

            previousKeys.Clear();
            previousKeys.UnionWith(snapshot.Keys);
        }

        private static string Sanitize(string? value)
            => string.IsNullOrEmpty(value) ? "unknown" : value.Replace(LabelSeparator, '_');

        private static string Join(params string[] values)
            => string.Join(LabelSeparator, values);

        private static string[] Split(string key)
            => key.Split(LabelSeparator);
    }
}
