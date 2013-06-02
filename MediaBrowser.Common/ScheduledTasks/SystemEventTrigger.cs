using MediaBrowser.Model.Tasks;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks
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
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        public void Start(bool isApplicationStartup)
        {
            switch (SystemEvent)
            {
                case SystemEvent.WakeFromSleep:
                    SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                    break;
            }
        }

        /// <summary>
        /// Stops waiting for the trigger action
        /// </summary>
        public void Stop()
        {
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
        }

        /// <summary>
        /// Handles the PowerModeChanged event of the SystemEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PowerModeChangedEventArgs" /> instance containing the event data.</param>
        async void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume && SystemEvent == SystemEvent.WakeFromSleep)
            {
                // This value is a bit arbitrary, but add a delay to help ensure network connections have been restored before running the task
                await Task.Delay(10000).ConfigureAwait(false);

                OnTriggered();
            }
        }

        /// <summary>
        /// Occurs when [triggered].
        /// </summary>
        public event EventHandler<EventArgs> Triggered;

        /// <summary>
        /// Called when [triggered].
        /// </summary>
        private void OnTriggered()
        {
            if (Triggered != null)
            {
                Triggered(this, EventArgs.Empty);
            }
        }
    }
}
