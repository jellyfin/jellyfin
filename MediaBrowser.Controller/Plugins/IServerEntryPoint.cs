using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Plugins
{
    /// <summary>
    /// Represents an entry point for a module in the application. This interface is scanned for automatically and
    /// provides a hook to initialize the module at application start.
    /// The entry point can additionally be flagged as a pre-startup task by implementing the
    /// <see cref="IRunBeforeStartup"/> interface.
    /// </summary>
    public interface IServerEntryPoint : IDisposable
    {
        /// <summary>
        /// Run the initialization for this module. This method is invoked at application start.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RunAsync();
    }
}
