using System;
using System.Threading.Tasks;

namespace Jellyfin.Controller.Plugins
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
