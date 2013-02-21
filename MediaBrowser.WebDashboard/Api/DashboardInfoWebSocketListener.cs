using MediaBrowser.Common.Kernel;
using MediaBrowser.Controller;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace MediaBrowser.WebDashboard.Api
{
    /// <summary>
    /// Class DashboardInfoWebSocketListener
    /// </summary>
    [Export(typeof(IWebSocketListener))]
    class DashboardInfoWebSocketListener : BasePeriodicWebSocketListener<IKernel, DashboardInfo, object>
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
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{IEnumerable{TaskInfo}}.</returns>
        protected override Task<DashboardInfo> GetDataToSend(object state)
        {
            return Task.FromResult(DashboardService.GetDashboardInfo((Kernel)Kernel));
        }
    }
}
