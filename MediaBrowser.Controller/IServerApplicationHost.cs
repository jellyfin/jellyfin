using MediaBrowser.Common;
using MediaBrowser.Model.System;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Interface IServerApplicationHost
    /// </summary>
    public interface IServerApplicationHost : IApplicationHost
    {
        /// <summary>
        /// Gets the system info.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        SystemInfo GetSystemInfo();
    }
}
