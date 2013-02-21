using MediaBrowser.Common.Kernel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Api
{
    /// <summary>
    /// Class SystemInfoWebSocketListener
    /// </summary>
    [Export(typeof(IWebSocketListener))]
    public class SystemInfoWebSocketListener : BasePeriodicWebSocketListener<IKernel, Model.System.SystemInfo, object>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "SystemInfo"; }
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<Model.System.SystemInfo> GetDataToSend(object state)
        {
            return Task.FromResult(Kernel.GetSystemInfo());
        }
    }
}
