#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks
{
    /// <summary>
    /// Class RefreshMediaLibraryTask.
    /// </summary>
    public class RefreshMediaLibraryTask : IScheduledTask
    {
        /// <summary>
        /// The _library manager.
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;
        private readonly IImageGenerator _imageGenerator;
        private readonly IApplicationPaths _applicationPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshMediaLibraryTask" /> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        /// <param name="imageGenerator">Instance of the <see cref="IImageGenerator"/> interface.</param>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        public RefreshMediaLibraryTask(
            ILibraryManager libraryManager,
            ILocalizationManager localization,
            IImageGenerator imageGenerator,
            IApplicationPaths applicationPaths)
        {
            _libraryManager = libraryManager;
            _localization = localization;
            _imageGenerator = imageGenerator;
            _applicationPaths = applicationPaths;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskRefreshLibrary");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskRefreshLibraryDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

        /// <inheritdoc />
        public string Key => "RefreshLibrary";

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(12).Ticks
            };
        }

        /// <summary>
        /// Executes the internal.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(0);

            _imageGenerator.Generate(GeneratedImageType.Splashscreen, Path.Combine(_applicationPaths.DataPath, "splashscreen.webp"));

            return ((LibraryManager)_libraryManager).ValidateMediaLibraryInternal(progress, cancellationToken);
        }
    }
}
