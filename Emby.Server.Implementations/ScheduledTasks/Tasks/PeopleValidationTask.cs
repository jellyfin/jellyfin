using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Globalization;

namespace Emby.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class PeopleValidationTask.
    /// </summary>
    public class PeopleValidationTask : IScheduledTask
    {
        /// <summary>
        /// The library manager.
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly IServerApplicationHost _appHost;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeopleValidationTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="appHost">The server application host</param>
        public PeopleValidationTask(ILibraryManager libraryManager, IServerApplicationHost appHost, ILocalizationManager localization)
        {
            _libraryManager = libraryManager;
            _appHost = appHost;
            _localization = localization;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerInterval,
                    IntervalTicks = TimeSpan.FromDays(7).Ticks
                }
            };
        }

        /// <summary>
        /// Returns the task to be executed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return _libraryManager.ValidatePeople(cancellationToken, progress);
        }

        public string Name => _localization.GetLocalizedString("TaskRefreshPeople");

        public string Description => _localization.GetLocalizedString("TaskRefreshPeopleDescription");

        public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

        public string Key => "RefreshPeople";

        public bool IsHidden => false;

        public bool IsEnabled => true;

        public bool IsLogged => true;
    }
}
