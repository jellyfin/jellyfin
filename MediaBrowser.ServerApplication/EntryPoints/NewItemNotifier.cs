using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.ServerApplication.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace MediaBrowser.ServerApplication.EntryPoints
{
    /// <summary>
    /// Class NewItemNotifier
    /// </summary>
    public class NewItemNotifier
    {
        /// <summary>
        /// Holds the list of new items to display when the NewItemTimer expires
        /// </summary>
        private readonly List<BaseItem> _newlyAddedItems = new List<BaseItem>();

        /// <summary>
        /// The amount of time to wait before showing a new item notification
        /// This allows us to group items together into one notification
        /// </summary>
        private const int NewItemDelay = 60000;

        /// <summary>
        /// The current new item timer
        /// </summary>
        /// <value>The new item timer.</value>
        private Timer NewItemTimer { get; set; }

        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewItemNotifier" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logManager">The log manager.</param>
        public NewItemNotifier(ILibraryManager libraryManager, ILogManager logManager)
        {
            _logger = logManager.GetLogger("NewItemNotifier");
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            _libraryManager.LibraryChanged += libraryManager_LibraryChanged;
        }

        /// <summary>
        /// Handles the LibraryChanged event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ChildrenChangedEventArgs" /> instance containing the event data.</param>
        void libraryManager_LibraryChanged(object sender, ChildrenChangedEventArgs e)
        {
            var newItems = e.ItemsAdded.Where(i => !i.IsFolder).ToList();

            // Use a timer to prevent lots of these notifications from showing in a short period of time
            if (newItems.Count > 0)
            {
                lock (_newlyAddedItems)
                {
                    _newlyAddedItems.AddRange(newItems);

                    if (NewItemTimer == null)
                    {
                        NewItemTimer = new Timer(NewItemTimerCallback, null, NewItemDelay, Timeout.Infinite);
                    }
                    else
                    {
                        NewItemTimer.Change(NewItemDelay, Timeout.Infinite);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the new item timer expires
        /// </summary>
        /// <param name="state">The state.</param>
        private void NewItemTimerCallback(object state)
        {
            List<BaseItem> newItems;

            // Lock the list and release all resources
            lock (_newlyAddedItems)
            {
                newItems = _newlyAddedItems.ToList();
                _newlyAddedItems.Clear();

                NewItemTimer.Dispose();
                NewItemTimer = null;
            }

            // Show the notification
            if (newItems.Count > 0)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var window = Application.Current.Windows.OfType<MainWindow>().First();

                    window.Dispatcher.InvokeAsync(() => window.MbTaskbarIcon.ShowCustomBalloon(new ItemUpdateNotification(_logger)
                    {
                        DataContext = newItems[0]

                    }, PopupAnimation.Slide, 6000));
                });
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _libraryManager.LibraryChanged -= libraryManager_LibraryChanged;
        }
    }
}
