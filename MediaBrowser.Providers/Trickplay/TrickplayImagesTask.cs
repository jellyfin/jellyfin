using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Trickplay
{
    /// <summary>
    /// Class TrickplayImagesTask.
    /// </summary>
    public class TrickplayImagesTask : IScheduledTask
    {
        private readonly ILogger<TrickplayImagesTask> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;
        private readonly ITrickplayManager _trickplayManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrickplayImagesTask"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="localization">The localization manager.</param>
        /// <param name="trickplayManager">The trickplay manager.</param>
        public TrickplayImagesTask(
            ILogger<TrickplayImagesTask> logger,
            ILibraryManager libraryManager,
            ILocalizationManager localization,
            ITrickplayManager trickplayManager)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _localization = localization;
            _trickplayManager = trickplayManager;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskRefreshTrickplayImages");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskRefreshTrickplayImagesDescription");

        /// <inheritdoc />
        public string Key => "RefreshTrickplayImages";

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
                }
            };
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                MediaTypes = new[] { MediaType.Video },
                IsVirtualItem = false,
                IsFolder = false,
                Recursive = true
            }).OfType<Video>().ToList();

            var numComplete = 0;

            foreach (var item in items)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _trickplayManager.RefreshTrickplayData(item, false, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error creating trickplay files for {ItemName}: {Msg}", item.Name, ex);
                }

                numComplete++;
                double percent = numComplete;
                percent /= items.Count;
                percent *= 100;

                progress.Report(percent);
            }
        }
    }
}
