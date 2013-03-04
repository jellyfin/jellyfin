using MediaBrowser.Model.System;
using System;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Interface IKernel
    /// </summary>
    public interface IKernel
    {
        /// <summary>
        /// Occurs when [has pending restart changed].
        /// </summary>
        event EventHandler HasPendingRestartChanged;

        /// <summary>
        /// Gets the kernel context.
        /// </summary>
        /// <value>The kernel context.</value>
        KernelContext KernelContext { get; }

        /// <summary>
        /// Inits this instance.
        /// </summary>
        /// <returns>Task.</returns>
        void Init();

        /// <summary>
        /// Gets or sets a value indicating whether this instance has pending kernel reload.
        /// </summary>
        /// <value><c>true</c> if this instance has pending kernel reload; otherwise, <c>false</c>.</value>
        bool HasPendingRestart { get; }

        /// <summary>
        /// Gets the system status.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        SystemInfo GetSystemInfo();

        /// <summary>
        /// Called when [application updated].
        /// </summary>
        /// <param name="newVersion">The new version.</param>
        void OnApplicationUpdated(Version newVersion);

        /// <summary>
        /// Gets the name of the web application.
        /// </summary>
        /// <value>The name of the web application.</value>
        string WebApplicationName { get; }

        /// <summary>
        /// Performs the pending restart.
        /// </summary>
        void PerformPendingRestart();

        /// <summary>
        /// Gets the UDP server port number.
        /// </summary>
        /// <value>The UDP server port number.</value>
        int UdpServerPortNumber { get; }

        /// <summary>
        /// Gets the HTTP server URL prefix.
        /// </summary>
        /// <value>The HTTP server URL prefix.</value>
        string HttpServerUrlPrefix { get; }

        /// <summary>
        /// Notifies the pending restart.
        /// </summary>
        void NotifyPendingRestart();
    }
}
