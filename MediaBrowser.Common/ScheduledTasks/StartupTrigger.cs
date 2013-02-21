using MediaBrowser.Common.Kernel;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Class StartupTaskTrigger
    /// </summary>
    public class StartupTrigger : BaseTaskTrigger
    {
        /// <summary>
        /// Gets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        protected IKernel Kernel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupTrigger" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public StartupTrigger(IKernel kernel)
        {
            Kernel = kernel;
        }

        /// <summary>
        /// Stars waiting for the trigger action
        /// </summary>
        protected internal override void Start()
        {
            Kernel.ReloadCompleted += Kernel_ReloadCompleted;
        }

        async void Kernel_ReloadCompleted(object sender, EventArgs e)
        {
            await Task.Delay(2000).ConfigureAwait(false);

            OnTriggered();
        }

        /// <summary>
        /// Stops waiting for the trigger action
        /// </summary>
        protected internal override void Stop()
        {
            Kernel.ReloadCompleted -= Kernel_ReloadCompleted;
        }
    }
}
