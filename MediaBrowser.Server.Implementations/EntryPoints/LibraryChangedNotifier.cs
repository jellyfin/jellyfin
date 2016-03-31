using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Entities.Audio;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class LibraryChangedNotifier : IServerEntryPoint
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly ISessionManager _sessionManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        /// <summary>
        /// The _library changed sync lock
        /// </summary>
        private readonly object _libraryChangedSyncLock = new object();

        private readonly List<Folder> _foldersAddedTo = new List<Folder>();
        private readonly List<Folder> _foldersRemovedFrom = new List<Folder>();

        private readonly List<BaseItem> _itemsAdded = new List<BaseItem>();
        private readonly List<BaseItem> _itemsRemoved = new List<BaseItem>();
        private readonly List<BaseItem> _itemsUpdated = new List<BaseItem>();

        /// <summary>
        /// Gets or sets the library update timer.
        /// </summary>
        /// <value>The library update timer.</value>
        private Timer LibraryUpdateTimer { get; set; }

        /// <summary>
        /// The library update duration
        /// </summary>
        private const int LibraryUpdateDuration = 5000;

        public LibraryChangedNotifier(ILibraryManager libraryManager, ISessionManager sessionManager, IUserManager userManager, ILogger logger)
        {
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _userManager = userManager;
            _logger = logger;
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
            if (!FilterItem(e.Item))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
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
                    _foldersAddedTo.Add(e.Item.Parent);
                }

                _itemsAdded.Add(e.Item);
            }
        }

        /// <summary>
        /// Handles the ItemUpdated event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void libraryManager_ItemUpdated(object sender, ItemChangeEventArgs e)
        {
            if (!FilterItem(e.Item))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration,
                                                   Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }

                _itemsUpdated.Add(e.Item);
            }
        }

        /// <summary>
        /// Handles the ItemRemoved event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void libraryManager_ItemRemoved(object sender, ItemChangeEventArgs e)
        {
            if (!FilterItem(e.Item))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
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
                    _foldersRemovedFrom.Add(e.Item.Parent);
                }

                _itemsRemoved.Add(e.Item);
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
                var foldersAddedTo = _foldersAddedTo.DistinctBy(i => i.Id).ToList();

                var foldersRemovedFrom = _foldersRemovedFrom.DistinctBy(i => i.Id).ToList();

                var itemsUpdated = _itemsUpdated
                    .Where(i => !_itemsAdded.Contains(i))
                    .DistinctBy(i => i.Id)
                    .ToList();

                SendChangeNotifications(_itemsAdded.ToList(), itemsUpdated, _itemsRemoved.ToList(), foldersAddedTo, foldersRemovedFrom, CancellationToken.None);

                if (LibraryUpdateTimer != null)
                {
                    LibraryUpdateTimer.Dispose();
                    LibraryUpdateTimer = null;
                }

                _itemsAdded.Clear();
                _itemsRemoved.Clear();
                _itemsUpdated.Clear();
                _foldersAddedTo.Clear();
                _foldersRemovedFrom.Clear();
            }
        }

        /// <summary>
        /// Sends the change notifications.
        /// </summary>
        /// <param name="itemsAdded">The items added.</param>
        /// <param name="itemsUpdated">The items updated.</param>
        /// <param name="itemsRemoved">The items removed.</param>
        /// <param name="foldersAddedTo">The folders added to.</param>
        /// <param name="foldersRemovedFrom">The folders removed from.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async void SendChangeNotifications(List<BaseItem> itemsAdded, List<BaseItem> itemsUpdated, List<BaseItem> itemsRemoved, List<Folder> foldersAddedTo, List<Folder> foldersRemovedFrom, CancellationToken cancellationToken)
        {
            foreach (var user in _userManager.Users.ToList())
            {
                var id = user.Id;
                var userSessions = _sessionManager.Sessions
                    .Where(u => u.UserId.HasValue && u.UserId.Value == id && u.SessionController != null && u.IsActive)
                    .ToList();

                if (userSessions.Count > 0)
                {
                    LibraryUpdateInfo info;

                    try
                    {
                         info = GetLibraryUpdateInfo(itemsAdded, itemsUpdated, itemsRemoved, foldersAddedTo,
                                                        foldersRemovedFrom, id);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error in GetLibraryUpdateInfo", ex);
                        return;
                    }

                    foreach (var userSession in userSessions)
                    {
                        try
                        {
                            await userSession.SessionController.SendLibraryUpdateInfo(info, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error sending LibraryChanged message", ex);
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Gets the library update info.
        /// </summary>
        /// <param name="itemsAdded">The items added.</param>
        /// <param name="itemsUpdated">The items updated.</param>
        /// <param name="itemsRemoved">The items removed.</param>
        /// <param name="foldersAddedTo">The folders added to.</param>
        /// <param name="foldersRemovedFrom">The folders removed from.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>LibraryUpdateInfo.</returns>
        private LibraryUpdateInfo GetLibraryUpdateInfo(IEnumerable<BaseItem> itemsAdded, IEnumerable<BaseItem> itemsUpdated, IEnumerable<BaseItem> itemsRemoved, IEnumerable<Folder> foldersAddedTo, IEnumerable<Folder> foldersRemovedFrom, Guid userId)
        {
            var user = _userManager.GetUserById(userId);

            return new LibraryUpdateInfo
            {
                ItemsAdded = itemsAdded.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user)).Select(i => i.Id.ToString("N")).Distinct().ToList(),

                ItemsUpdated = itemsUpdated.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user)).Select(i => i.Id.ToString("N")).Distinct().ToList(),

                ItemsRemoved = itemsRemoved.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user, true)).Select(i => i.Id.ToString("N")).Distinct().ToList(),

                FoldersAddedTo = foldersAddedTo.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user)).Select(i => i.Id.ToString("N")).Distinct().ToList(),

                FoldersRemovedFrom = foldersRemovedFrom.SelectMany(i => TranslatePhysicalItemToUserLibrary(i, user)).Select(i => i.Id.ToString("N")).Distinct().ToList()
            };
        }

        private bool FilterItem(BaseItem item)
        {
            if (!item.IsFolder && item.LocationType == LocationType.Virtual)
            {
                return false;
            }

            if (item is IItemByName && !(item is MusicArtist))
            {
                return false;
            }

            return item.SourceType == SourceType.Library;
        }

        /// <summary>
        /// Translates the physical item to user library.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <param name="includeIfNotFound">if set to <c>true</c> [include if not found].</param>
        /// <returns>IEnumerable{``0}.</returns>
        private IEnumerable<T> TranslatePhysicalItemToUserLibrary<T>(T item, User user, bool includeIfNotFound = false)
            where T : BaseItem
        {
            // If the physical root changed, return the user root
            if (item is AggregateFolder)
            {
                return new[] { user.RootFolder as T };
            }

            // Return it only if it's in the user's library
            if (includeIfNotFound || item.IsVisibleStandalone(user))
            {
                return new[] { item };
            }

            return new T[] { };
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
