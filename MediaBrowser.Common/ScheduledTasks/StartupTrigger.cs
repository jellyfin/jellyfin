using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Class StartupTaskTrigger
    /// </summary>
    public class StartupTrigger : BaseTaskTrigger
    {
        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        /// <param name="isApplicationStartup">if set to <c>true</c> [is application startup].</param>
        protected internal async override void Start(bool isApplicationStartup)
        {
            if (isApplicationStartup)
            {
                await Task.Delay(2000).ConfigureAwait(false);

                OnTriggered();
            }
        }

        /// <summary>
        /// Stops waiting for the trigger action
        /// </summary>
        protected internal override void Stop()
        {
        }
    }
}
