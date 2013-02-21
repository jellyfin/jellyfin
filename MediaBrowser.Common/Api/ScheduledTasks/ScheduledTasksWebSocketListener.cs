using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Api.ScheduledTasks
{
    /// <summary>
    /// Class ScheduledTasksWebSocketListener
    /// </summary>
    [Export(typeof(IWebSocketListener))]
    public class ScheduledTasksWebSocketListener : BasePeriodicWebSocketListener<IKernel, IEnumerable<TaskInfo>, object>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "ScheduledTasksInfo"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTasksWebSocketListener" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        [ImportingConstructor]
        public ScheduledTasksWebSocketListener([Import("logger")] ILogger logger)
            : base(logger)
        {

        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{IEnumerable{TaskInfo}}.</returns>
        protected override Task<IEnumerable<TaskInfo>> GetDataToSend(object state)
        {
            return Task.FromResult(Kernel.ScheduledTasks.OrderBy(i => i.Name)
                         .Select(ScheduledTaskHelpers.GetTaskInfo));
        }
    }
}
