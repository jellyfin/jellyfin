using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;

namespace Emby.Server.Implementations.IO
{
    /// <summary>
    /// <see cref="IServerEntryPoint" /> which is responsible for starting the library monitor.
    /// </summary>
    public sealed class LibraryMonitorStartup : IServerEntryPoint
    {
        private readonly ILibraryMonitor _monitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryMonitorStartup"/> class.
        /// </summary>
        /// <param name="monitor">The library monitor.</param>
        public LibraryMonitorStartup(ILibraryMonitor monitor)
        {
            _monitor = monitor;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _monitor.Start();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
