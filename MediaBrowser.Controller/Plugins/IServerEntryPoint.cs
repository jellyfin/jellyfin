using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Plugins
{
    /// <summary>
    /// Interface IServerEntryPoint
    /// </summary>
    public interface IServerEntryPoint : IDisposable
    {
        /// <summary>
        /// Runs this instance.
        /// </summary>
        Task RunAsync();
    }

    public interface IRunBeforeStartup
    {

    }
}
