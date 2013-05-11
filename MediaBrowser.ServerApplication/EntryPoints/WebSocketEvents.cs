using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Updates;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Updates;
using System;
using System.Linq;
using System.Threading;

namespace MediaBrowser.ServerApplication.EntryPoints
{
    /// <summary>
    /// Class WebSocketEvents
    /// </summary>
    public class WebSocketEvents : IServerEntryPoint
    {
        /// <summary>
        /// The _server manager
        /// </summary>
        private readonly IServerManager _serverManager;
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The _installation manager
        /// </summary>
        private readonly IInstallationManager _installationManager;

        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// The _task manager
        /// </summary>
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// The _library changed sync lock
        /// </summary>
        private readonly object _libraryChangedSyncLock = new object();

        /// <summary>
        /// Gets or sets the library update info.
        /// </summary>
        /// <value>The library update info.</value>
        private LibraryUpdateInfo LibraryUpdateInfo { get; set; }

        /// <summary>
        /// Gets or sets the library update timer.
        /// </summary>
        /// <value>The library update timer.</value>
        private Timer LibraryUpdateTimer { get; set; }

        /// <summary>
        /// The library update duration
        /// </summary>
        private const int LibraryUpdateDuration = 60000;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketEvents" /> class.
        /// </summary>
        /// <param name="serverManager">The server manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userManager">The user manager.</param>
        public WebSocketEvents(IServerManager serverManager, IServerApplicationHost appHost, ILogger logger, IUserManager userManager, ILibraryManager libraryManager, IInstallationManager installationManager, ITaskManager taskManager)
        {
            _serverManager = serverManager;
            _logger = logger;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _installationManager = installationManager;
            _appHost = appHost;
            _taskManager = taskManager;
        }

        public void Run()
        {
            _userManager.UserDeleted += userManager_UserDeleted;
            _userManager.UserUpdated += userManager_UserUpdated;

            _appHost.HasPendingRestartChanged += kernel_HasPendingRestartChanged;

            _libraryManager.ItemAdded += libraryManager_ItemAdded;
            _libraryManager.ItemUpdated += libraryManager_ItemUpdated;
            _libraryManager.ItemRemoved += libraryManager_ItemRemoved;

            _installationManager.PluginUninstalled += InstallationManager_PluginUninstalled;
            _installationManager.PackageInstalling += installationManager_PackageInstalling;
            _installationManager.PackageInstallationCancelled += installationManager_PackageInstallationCancelled;
            _installationManager.PackageInstallationCompleted += installationManager_PackageInstallationCompleted;
            _installationManager.PackageInstallationFailed += installationManager_PackageInstallationFailed;

            _taskManager.TaskExecuting += _taskManager_TaskExecuting;
            _taskManager.TaskCompleted += _taskManager_TaskCompleted;
        }

        void _taskManager_TaskCompleted(object sender, GenericEventArgs<TaskResult> e)
        {
            _serverManager.SendWebSocketMessage("ScheduledTaskEnded", e.Argument);
        }

        void _taskManager_TaskExecuting(object sender, EventArgs e)
        {
            var task = (IScheduledTask)sender;
            _serverManager.SendWebSocketMessage("ScheduledTaskStarted", task.Name);
        }

        /// <summary>
        /// Installations the manager_ package installation failed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void installationManager_PackageInstallationFailed(object sender, GenericEventArgs<InstallationInfo> e)
        {
            _serverManager.SendWebSocketMessage("PackageInstallationFailed", e.Argument);
        }

        /// <summary>
        /// Installations the manager_ package installation completed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void installationManager_PackageInstallationCompleted(object sender, GenericEventArgs<InstallationInfo> e)
        {
            _serverManager.SendWebSocketMessage("PackageInstallationCompleted", e.Argument);
        }

        /// <summary>
        /// Installations the manager_ package installation cancelled.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void installationManager_PackageInstallationCancelled(object sender, GenericEventArgs<InstallationInfo> e)
        {
            _serverManager.SendWebSocketMessage("PackageInstallationCancelled", e.Argument);
        }

        /// <summary>
        /// Installations the manager_ package installing.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void installationManager_PackageInstalling(object sender, GenericEventArgs<InstallationInfo> e)
        {
            _serverManager.SendWebSocketMessage("PackageInstalling", e.Argument);
        }

        /// <summary>
        /// Handles the ItemAdded event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateInfo == null)
                {
                    LibraryUpdateInfo = new LibraryUpdateInfo();
                }

                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration,
                                                   Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }

                if (e.Item.Parent != null)
                {
                    LibraryUpdateInfo.FoldersAddedTo.Add(e.Item.Parent.Id);
                }

                LibraryUpdateInfo.ItemsAdded.Add(e.Item.Id);
            }
        }

        /// <summary>
        /// Handles the ItemUpdated event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void libraryManager_ItemUpdated(object sender, ItemChangeEventArgs e)
        {
            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateInfo == null)
                {
                    LibraryUpdateInfo = new LibraryUpdateInfo();
                }

                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration,
                                                   Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }

                LibraryUpdateInfo.ItemsUpdated.Add(e.Item.Id);
            }
        }

        /// <summary>
        /// Handles the ItemRemoved event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void libraryManager_ItemRemoved(object sender, ItemChangeEventArgs e)
        {
            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateInfo == null)
                {
                    LibraryUpdateInfo = new LibraryUpdateInfo();
                }

                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration,
                                                   Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }

                if (e.Item.Parent != null)
                {
                    LibraryUpdateInfo.FoldersRemovedFrom.Add(e.Item.Parent.Id);
                }

                LibraryUpdateInfo.ItemsRemoved.Add(e.Item.Id);
            }
        }

        /// <summary>
        /// Libraries the update timer callback.
        /// </summary>
        /// <param name="state">The state.</param>
        private void LibraryUpdateTimerCallback(object state)
        {
            lock (_libraryChangedSyncLock)
            {
                // Remove dupes in case some were saved multiple times
                LibraryUpdateInfo.FoldersAddedTo = LibraryUpdateInfo.FoldersAddedTo.Distinct().ToList();

                LibraryUpdateInfo.FoldersRemovedFrom = LibraryUpdateInfo.FoldersRemovedFrom.Distinct().ToList();

                LibraryUpdateInfo.ItemsUpdated = LibraryUpdateInfo.ItemsUpdated
                    .Where(i => !LibraryUpdateInfo.ItemsAdded.Contains(i))
                    .Distinct()
                    .ToList();

                _serverManager.SendWebSocketMessage("LibraryChanged", LibraryUpdateInfo);

                if (LibraryUpdateTimer != null)
                {
                    LibraryUpdateTimer.Dispose();
                    LibraryUpdateTimer = null;
                }

                LibraryUpdateInfo = null;
            }
        }

        /// <summary>
        /// Installations the manager_ plugin uninstalled.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void InstallationManager_PluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            _serverManager.SendWebSocketMessage("PluginUninstalled", e.Argument.GetPluginInfo());
        }

        /// <summary>
        /// Handles the HasPendingRestartChanged event of the kernel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void kernel_HasPendingRestartChanged(object sender, EventArgs e)
        {
            _serverManager.SendWebSocketMessage("RestartRequired", _appHost.GetSystemInfo());
        }

        /// <summary>
        /// Users the manager_ user updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        async void userManager_UserUpdated(object sender, GenericEventArgs<User> e)
        {
            var dto = await new UserDtoBuilder(_logger).GetUserDto(e.Argument).ConfigureAwait(false);

            _serverManager.SendWebSocketMessage("UserUpdated", dto);
        }

        /// <summary>
        /// Users the manager_ user deleted.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        void userManager_UserDeleted(object sender, GenericEventArgs<User> e)
        {
            _serverManager.SendWebSocketMessage("UserDeleted", e.Argument.Id.ToString("N"));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (LibraryUpdateTimer != null)
                {
                    LibraryUpdateTimer.Dispose();
                    LibraryUpdateTimer = null;
                }

                _libraryManager.ItemAdded -= libraryManager_ItemAdded;
                _libraryManager.ItemUpdated -= libraryManager_ItemUpdated;
                _libraryManager.ItemRemoved -= libraryManager_ItemRemoved;
                
                _userManager.UserDeleted -= userManager_UserDeleted;
                _userManager.UserUpdated -= userManager_UserUpdated;

                _installationManager.PluginUninstalled -= InstallationManager_PluginUninstalled;
                _installationManager.PackageInstalling -= installationManager_PackageInstalling;
                _installationManager.PackageInstallationCancelled -= installationManager_PackageInstallationCancelled;
                _installationManager.PackageInstallationCompleted -= installationManager_PackageInstallationCompleted;
                _installationManager.PackageInstallationFailed -= installationManager_PackageInstallationFailed;

                _appHost.HasPendingRestartChanged -= kernel_HasPendingRestartChanged;
            }
        }
    }
}
