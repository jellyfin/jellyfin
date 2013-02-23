using System;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Use to indicate that a scheduled task should run
    /// </summary>
    public abstract class BaseTaskTrigger : IDisposable
    {
        /// <summary>
        /// Fires when the trigger condition is satisfied and the task should run
        /// </summary>
        internal event EventHandler<EventArgs> Triggered;

        /// <summary>
        /// Called when [triggered].
        /// </summary>
        protected async void OnTriggered()
        {
            Stop();
            
            if (Triggered != null)
            {
                Triggered(this, EventArgs.Empty);
            }

            await Task.Delay(1000).ConfigureAwait(false);

            Start(false);
        }

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        protected internal abstract void Start(bool isApplicationStartup);

        /// <summary>
        /// Stops waiting for the trigger action
        /// </summary>
        protected internal abstract void Stop();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                Stop();
            }
        }
    }
}
