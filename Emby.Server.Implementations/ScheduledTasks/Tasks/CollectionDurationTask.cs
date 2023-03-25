using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Class CollectionDurationTask.
    /// </summary>
    public class CollectionDurationTask : IScheduledTask
    {
        /// <summary>
        /// The _library manager.
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly ICollectionManager _collectionManager;

        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionDurationTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>.
        /// <param name="collectionManager">The collection manager.</param>.
        /// <param name="localization">The localization manager.</param>
        public CollectionDurationTask(
            ILibraryManager libraryManager,
            ICollectionManager collectionManager,
            ILocalizationManager localization)
        {
            _libraryManager = libraryManager;
            _collectionManager = collectionManager;
            _localization = localization;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskCollectionDuration");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskCollectionDurationDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

        /// <inheritdoc />
        public string Key => "CollectionDuration";

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(4).Ticks,
                    MaxRuntimeTicks = TimeSpan.FromHours(5).Ticks
                }
            };
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var collections = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IsFolder = true,
                Recursive = true,
                SourceTypes = new SourceType[] { SourceType.Library },
            })
                .OfType<BoxSet>()
                .ToList();

            var numComplete = 0;

            foreach (var collection in collections)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var collectionChildren = new List<BaseItem>();

                    foreach (var child in collection.LinkedChildren)
                    {
                        if (child.ItemId.HasValue)
                        {
                            var item = _libraryManager.GetItemById((Guid)child.ItemId);

                            if (item != null)
                            {
                                _collectionManager.GetItemsForRunTimeTicksCount(item).ToList<BaseItem>().ForEach((i) => collectionChildren.Add(i));
                            }
                        }
                    }

                    collection.UpdateRunTimeTicksToItems(collectionChildren);

                    await collection.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(true);

                    numComplete++;
                    double percent = numComplete;
                    percent /= collections.Count;

                    progress.Report(100 * percent);
                }
                catch (Exception)
                {
                    break;
                }
            }
        }
    }
}
