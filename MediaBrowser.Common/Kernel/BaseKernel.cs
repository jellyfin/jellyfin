using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using System;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Represents a shared base kernel for both the Ui and server apps
    /// </summary>
    public abstract class BaseKernel : IKernel
    {
        /// <summary>
        /// Occurs when [has pending restart changed].
        /// </summary>
        public event EventHandler HasPendingRestartChanged;

        #region ApplicationUpdated Event
        /// <summary>
        /// Occurs when [application updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<Version>> ApplicationUpdated;
        /// <summary>
        /// Called when [application updated].
        /// </summary>
        /// <param name="newVersion">The new version.</param>
        public void OnApplicationUpdated(Version newVersion)
        {
            EventHelper.QueueEventIfNotNull(ApplicationUpdated, this, new GenericEventArgs<Version> { Argument = newVersion }, Logger);

            NotifyPendingRestart();
        }
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether this instance has changes that require the entire application to restart.
        /// </summary>
        /// <value><c>true</c> if this instance has pending application restart; otherwise, <c>false</c>.</value>
        public bool HasPendingRestart { get; private set; }

        /// <summary>
        /// Gets the UDP server port number.
        /// This can't be configurable because then the user would have to configure their client to discover the server.
        /// </summary>
        /// <value>The UDP server port number.</value>
        public abstract int UdpServerPortNumber { get; }

        /// <summary>
        /// Gets the name of the web application that can be used for url building.
        /// All api urls will be of the form {protocol}://{host}:{port}/{appname}/...
        /// </summary>
        /// <value>The name of the web application.</value>
        public string WebApplicationName
        {
            get { return "mediabrowser"; }
        }

        /// <summary>
        /// Gets the HTTP server URL prefix.
        /// </summary>
        /// <value>The HTTP server URL prefix.</value>
        public virtual string HttpServerUrlPrefix
        {
            get
            {
                return "http://+:" + _configurationManager.CommonConfiguration.HttpServerPortNumber + "/" + WebApplicationName + "/";
            }
        }

        /// <summary>
        /// Gets the kernel context. Subclasses will have to override.
        /// </summary>
        /// <value>The kernel context.</value>
        public abstract KernelContext KernelContext { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Gets or sets the application host.
        /// </summary>
        /// <value>The application host.</value>
        protected IApplicationHost ApplicationHost { get; private set; }

        private readonly IConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseKernel" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="logManager">The log manager.</param>
        protected BaseKernel(IApplicationHost appHost, ILogManager logManager, IConfigurationManager configurationManager)
        {
            ApplicationHost = appHost;
            _configurationManager = configurationManager;
            Logger = logManager.GetLogger("Kernel");
        }

        /// <summary>
        /// Initializes the Kernel
        /// </summary>
        /// <returns>Task.</returns>
        public void Init()
        {
            ReloadInternal();

            Logger.Info("Kernel.Init Complete");
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual void ReloadInternal()
        {
        }

        /// <summary>
        /// Notifies that the kernel that a change has been made that requires a restart
        /// </summary>
        public void NotifyPendingRestart()
        {
            HasPendingRestart = true;

            EventHelper.QueueEventIfNotNull(HasPendingRestartChanged, this, EventArgs.Empty, Logger);
        }

        /// <summary>
        /// Performs the pending restart.
        /// </summary>
        /// <returns>Task.</returns>
        public void PerformPendingRestart()
        {
            if (HasPendingRestart)
            {
                Logger.Info("Restarting the application");

                ApplicationHost.Restart();
            }
            else
            {
                Logger.Info("PerformPendingRestart - not needed");
            }
        }

        /// <summary>
        /// Gets the system status.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        public virtual SystemInfo GetSystemInfo()
        {
            return new SystemInfo
            {
                HasPendingRestart = HasPendingRestart,
                Version = ApplicationHost.ApplicationVersion.ToString(),
                IsNetworkDeployed = ApplicationHost.CanSelfUpdate,
                WebSocketPortNumber = ApplicationHost.Resolve<IServerManager>().WebSocketPortNumber,
                SupportsNativeWebSocket = ApplicationHost.Resolve<IServerManager>().SupportsNativeWebSocket,
                FailedPluginAssemblies = ApplicationHost.FailedAssemblies.ToArray()
            };
        }
    }
}
