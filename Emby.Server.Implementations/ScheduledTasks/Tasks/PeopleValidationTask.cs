using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Class PeopleValidationTask.
    /// </summary>
    public class PeopleValidationTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeopleValidationTask" /> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        public PeopleValidationTask(ILibraryManager libraryManager, ILocalizationManager localization)
        {
            _libraryManager = libraryManager;
            _localization = localization;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskRefreshPeople");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskRefreshPeopleDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

        /// <inheritdoc />
        public string Key => "RefreshPeople";

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{TaskTriggerInfo}"/> containing the default trigger infos for this task.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.IntervalTrigger,
                    IntervalTicks = TimeSpan.FromDays(7).Ticks
                }
            };
        }

        /// <inheritdoc />
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return _libraryManager.ValidatePeopleAsync(progress, cancellationToken);
        }
    }
}
