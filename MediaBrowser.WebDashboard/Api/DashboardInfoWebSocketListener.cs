using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace MediaBrowser.WebDashboard.Api
{
    /// <summary>
    /// Class DashboardInfoWebSocketListener
    /// </summary>
    [Export(typeof(IWebSocketListener))]
    class DashboardInfoWebSocketListener : BasePeriodicWebSocketListener<DashboardInfo, object>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "DashboardInfo"; }
        }

        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly Kernel _kernel;

        /// <summary>
        /// Gets or sets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardInfoWebSocketListener" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="userManager">The user manager.</param>
        public DashboardInfoWebSocketListener(Kernel kernel, ILogger logger, ITaskManager taskManager, IUserManager userManager)
            : base(logger)
        {
            _kernel = kernel;
            _taskManager = taskManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{IEnumerable{TaskInfo}}.</returns>
        protected override Task<DashboardInfo> GetDataToSend(object state)
        {
            return Task.FromResult(DashboardService.GetDashboardInfo(_kernel, Logger, _taskManager, _userManager));
        }
    }
}
