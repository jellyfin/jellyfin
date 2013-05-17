using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using System.Linq;
using System.Threading;

namespace MediaBrowser.ServerApplication.EntryPoints
{
    public class LibraryChangedNotifier : IServerEntryPoint
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly ISessionManager _sessionManager;
        private readonly IServerManager _serverManager;
   
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

        public LibraryChangedNotifier(ILibraryManager libraryManager, ISessionManager sessionManager, IServerManager serverManager)
        {
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _serverManager = serverManager;
        }

        public void Run()
        {
            _libraryManager.ItemAdded += libraryManager_ItemAdded;
            _libraryManager.ItemUpdated += libraryManager_ItemUpdated;
            _libraryManager.ItemRemoved += libraryManager_ItemRemoved;

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
            }
        }
    }
}
