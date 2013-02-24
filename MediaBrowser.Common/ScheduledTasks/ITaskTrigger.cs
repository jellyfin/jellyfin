using System;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Interface ITaskTrigger
    /// </summary>
    public interface ITaskTrigger
    {
        /// <summary>
        /// Fires when the trigger condition is satisfied and the task should run
        /// </summary>
        event EventHandler<EventArgs> Triggered;

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        void Start(bool isApplicationStartup);

        /// <summary>
        /// Stops waiting for the trigger action
        /// </summary>
        void Stop();
    }
}