using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Plugins
{
    /// <summary>
    /// Interface ILongRunningTask
    /// </summary>
    public interface ILongRunningTask: IDisposable
    {
        /// <summary>
        /// Runs this instance.
        /// </summary>
        Task RunAsync();
    }
}
