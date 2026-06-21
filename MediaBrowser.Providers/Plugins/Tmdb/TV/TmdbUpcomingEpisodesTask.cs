using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    /// <summary>
    /// Scheduled task that re-checks TMDb for newly announced unaired and missing episodes and creates
    /// the corresponding virtual items. This keeps the "Upcoming" view current for series whose local
    /// files have not changed, which an ordinary library scan would never re-examine.
    /// </summary>
    public class TmdbUpcomingEpisodesTask : IScheduledTask
    {
        private const int DefaultIntervalDays = 7;

        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<TmdbUpcomingEpisodesTask> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbUpcomingEpisodesTask"/> class.
        /// </summary>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
        /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
        /// <param name="logger">The <see cref="ILogger{TmdbUpcomingEpisodesTask}"/>.</param>
        public TmdbUpcomingEpisodesTask(
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            ILogger<TmdbUpcomingEpisodesTask> logger)
        {
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Refresh upcoming and missing episodes (TheMovieDb)";

        /// <inheritdoc />
        public string Description => "Checks TheMovieDb for newly announced episodes and creates virtual entries for unaired and missing episodes, according to the TMDb plugin settings. When both options are disabled, removes any virtual entries previously created.";

        /// <inheritdoc />
        public string Category => "Library";

        /// <inheritdoc />
        public string Key => "TmdbRefreshUpcomingEpisodes";

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var intervalDays = Plugin.Instance?.Configuration.MissingEpisodeRefreshIntervalDays ?? DefaultIntervalDays;
            if (intervalDays <= 0)
            {
                intervalDays = DefaultIntervalDays;
            }

            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromDays(intervalDays).Ticks
            };
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var configuration = Plugin.Instance?.Configuration;
            if (configuration is null)
            {
                progress.Report(100);
                return;
            }

            // The feature is fully disabled: remove every virtual episode (and now-empty virtual season)
            // this provider previously created, across all libraries, then stop.
            if (!configuration.ImportUnairedEpisodes && !configuration.ImportMissingEpisodes)
            {
                RemoveAllVirtualItems(progress, cancellationToken);
                return;
            }

            // Process non-ended series (they may have gained episodes) plus any series in a library that
            // has been opted out (regardless of status) so the provider can prune the virtual episodes it
            // previously created there. Ended series in enabled libraries cannot change, so they're skipped.
            var series = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Series],
                Recursive = true
            })
                .OfType<Series>()
                .Where(s => s.HasProviderId(MetadataProvider.Tmdb)
                    && (s.Status != SeriesStatus.Ended || !IsEnabledForLibrary(s)))
                .ToList();

            if (series.Count == 0)
            {
                progress.Report(100);
                return;
            }

            // ValidateChildren (rather than a bare RefreshMetadata) is required so the created episodes
            // are immediately visible.
            var refreshOptions = new MetadataRefreshOptions(new DirectoryService(_fileSystem))
            {
                MetadataRefreshMode = MetadataRefreshMode.Default,
                ImageRefreshMode = MetadataRefreshMode.ValidationOnly,
                IsAutomated = true
            };

            for (var i = 0; i < series.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await series[i].ValidateChildren(new Progress<double>(), refreshOptions, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing upcoming episodes for series {SeriesName}", series[i].Name);
                }

                progress.Report(100.0 * (i + 1) / series.Count);
            }
        }

        private bool IsEnabledForLibrary(BaseItem item)
        {
            var disabledLibraries = Plugin.Instance?.Configuration.DisabledMissingEpisodeLibraries;
            if (disabledLibraries is null || disabledLibraries.Length == 0)
            {
                return true;
            }

            // A series can live under more than one collection folder; treat it as disabled only when
            // every containing library is opted out.
            var collectionFolders = _libraryManager.GetCollectionFolders(item);
            if (collectionFolders.Count == 0)
            {
                return true;
            }

            return collectionFolders.Any(folder =>
                !disabledLibraries.Contains(folder.Id.ToString("N", CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Removes every virtual episode this provider created (identified by being virtual and carrying
        /// a TMDb id), plus any virtual season left without episodes as a result. Used when both import
        /// options are disabled so turning the feature off cleans up its placeholders.
        /// </summary>
        private void RemoveAllVirtualItems(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var deleteOptions = new DeleteOptions { DeleteFileLocation = false };

            var virtualEpisodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Episode],
                IsVirtualItem = true,
                HasTmdbId = true,
                Recursive = true
            });

            for (var i = 0; i < virtualEpisodes.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Removing virtual episode {Name}: the TMDb missing episode provider is disabled", virtualEpisodes[i].Name);
                _libraryManager.DeleteItem(virtualEpisodes[i], deleteOptions, false);

                progress.Report(95.0 * (i + 1) / virtualEpisodes.Count);
            }

            // Remove virtual seasons that are now empty (mirrors the cleanup an ordinary series refresh does).
            // Seasons created by this provider carry a TVDB id (from TMDb's external ids), not a TMDb id,
            // so they cannot be filtered by HasTmdbId; any virtual season left without episodes is obsolete.
            var virtualSeasons = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Season],
                IsVirtualItem = true,
                Recursive = true
            });

            foreach (var season in virtualSeasons.OfType<Season>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (season.GetEpisodes().Count == 0)
                {
                    _libraryManager.DeleteItem(season, deleteOptions, false);
                }
            }

            progress.Report(100);
        }
    }
}
