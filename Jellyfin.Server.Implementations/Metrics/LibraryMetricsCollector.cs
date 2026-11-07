using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Collects library and media related Prometheus metrics.
    /// </summary>
    public sealed class LibraryMetricsCollector
    {
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
            var virtualFolders = _libraryManager.GetVirtualFolders();
            RefreshLibraries(virtualFolders);
            RefreshLibraryItems();
            RefreshCodecs(virtualFolders);
        }

        private void RefreshLibraries(IReadOnlyCollection<VirtualFolderInfo> virtualFolders)
        {
            var libraries = virtualFolders
                .GroupBy(f => MetricLabels.CollectionType(f.CollectionType))
                .ToDictionary(g => g.Key, g => (double)g.Count());
            MetricsSnapshot.SetSnapshot(JellyfinMetrics.Libraries, libraries, _libraryKeys);
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
            MetricsSnapshot.SetSnapshot(JellyfinMetrics.LibraryItems, items, _itemKeys);
        }

        private void RefreshCodecs(IReadOnlyCollection<VirtualFolderInfo> virtualFolders)
        {
            // Media stream aggregation scans the library database, so refresh it less often.
            if (DateTime.UtcNow - _lastCodecRefresh < _codecRefreshInterval)
            {
                return;
            }

            var librariesById = new Dictionary<Guid, string>();
            foreach (var virtualFolder in virtualFolders)
            {
                if (Guid.TryParse(virtualFolder.ItemId, out var libraryId))
                {
                    librariesById.TryAdd(libraryId, MetricsSnapshot.Sanitize(virtualFolder.Name));
                }
            }

            using var dbContext = _dbProvider.CreateDbContext();
            var codecCounts = dbContext.MediaStreamInfos
                .AsNoTracking()
                .Where(s => s.Codec != null)
                .Join(
                    dbContext.BaseItems.AsNoTracking(),
                    stream => stream.ItemId,
                    item => item.Id,
                    (stream, item) => new { item.TopParentId, stream.StreamType, stream.Codec })
                .GroupBy(s => new { s.TopParentId, s.StreamType, s.Codec })
                .Select(g => new { g.Key.TopParentId, g.Key.StreamType, Codec = g.Key.Codec, Count = g.Count() })
                .ToArray();

            var codecs = new Dictionary<string, double>();
            foreach (var row in codecCounts.Where(row => row.Codec is not null))
            {
                var libraryId = row.TopParentId?.ToString("N") ?? "unknown";
                var libraryName = row.TopParentId.HasValue && librariesById.TryGetValue(row.TopParentId.Value, out var name)
                    ? name
                    : "unknown";
                var kind = MetricLabels.StreamKind(row.StreamType);
                var codec = MetricsSnapshot.Sanitize(row.Codec).ToLowerInvariant();
                codecs[MetricsSnapshot.Join(libraryName, libraryId, kind, codec)] = row.Count;
            }

            MetricsSnapshot.SetSnapshot(JellyfinMetrics.MediaStreams, codecs, _codecKeys);
            _lastCodecRefresh = DateTime.UtcNow;
        }
    }
}
