using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;

namespace Emby.Common.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class SystemEventTrigger
    /// </summary>
    public class SystemEventTrigger : ITaskTrigger
    {
        /// <summary>
        /// Gets or sets the system event.
        /// </summary>
        /// <value>The system event.</value>
        public SystemEvent SystemEvent { get; set; }

        /// <summary>
        /// Gets the execution properties of this task.
        /// </summary>
        /// <value>
        /// The execution properties of this task.
        /// </value>
        public TaskExecutionOptions TaskOptions { get; set; }

        private readonly ISystemEvents _systemEvents;

        public SystemEventTrigger(ISystemEvents systemEvents)
        {
            _systemEvents = systemEvents;
        }

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="lastResult">The last result.</param>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public void Start(TaskResult lastResult, ILogger logger, string taskName, bool isApplicationStartup)
        {
            switch (SystemEvent)
            {
                case SystemEvent.WakeFromSleep:
                    _systemEvents.Resume += _systemEvents_Resume;
                    break;
            }
        }

        private async void _systemEvents_Resume(object sender, EventArgs e)
        {
            if (SystemEvent == SystemEvent.WakeFromSleep)
            {
                // This value is a bit arbitrary, but add a delay to help ensure network connections have been restored before running the task
                await Task.Delay(10000).ConfigureAwait(false);

                OnTriggered();
            }
        }

        /// <summary>
        /// Stops waiting for the trigger action
        /// </summary>
        public void Stop()
        {
            _systemEvents.Resume -= _systemEvents_Resume;
        }

        /// <summary>
        /// Occurs when [triggered].
        /// </summary>
        public event EventHandler<GenericEventArgs<TaskExecutionOptions>> Triggered;

        /// <summary>
        /// Called when [triggered].
        /// </summary>
        private void OnTriggered()
        {
            if (Triggered != null)
            {
                Triggered(this, new GenericEventArgs<TaskExecutionOptions>(TaskOptions));
            }
        }
    }
}
