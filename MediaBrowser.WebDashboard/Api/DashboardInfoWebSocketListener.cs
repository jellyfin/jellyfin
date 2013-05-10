using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System.Threading.Tasks;

namespace MediaBrowser.WebDashboard.Api
{
    /// <summary>
    /// Class DashboardInfoWebSocketListener
    /// </summary>
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

        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// Gets or sets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardInfoWebSocketListener" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        public DashboardInfoWebSocketListener(IServerApplicationHost appHost, ILogger logger, ITaskManager taskManager, IUserManager userManager, ILibraryManager libraryManager, ISessionManager sessionManager)
            : base(logger)
        {
            _appHost = appHost;
            _taskManager = taskManager;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{IEnumerable{TaskInfo}}.</returns>
        protected override Task<DashboardInfo> GetDataToSend(object state)
        {
            return DashboardService.GetDashboardInfo(_appHost, Logger, _taskManager, _userManager, _libraryManager, _sessionManager);
        }
    }
}
