using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;

namespace MediaBrowser.WebDashboard.Api
{
    /// <summary>
    /// Class DashboardInfo
    /// </summary>
    public class DashboardInfo
    {
        /// <summary>
        /// Gets or sets the system info.
        /// </summary>
        /// <value>The system info.</value>
        public SystemInfo SystemInfo { get; set; }

        /// <summary>
        /// Gets or sets the running tasks.
        /// </summary>
        /// <value>The running tasks.</value>
        public List<TaskInfo> RunningTasks { get; set; }

        /// <summary>
        /// Gets or sets the application update task id.
        /// </summary>
        /// <value>The application update task id.</value>
        public Guid ApplicationUpdateTaskId { get; set; }

        /// <summary>
        /// Gets or sets the active connections.
        /// </summary>
        /// <value>The active connections.</value>
        public List<SessionInfoDto> ActiveConnections { get; set; }
    }

}
