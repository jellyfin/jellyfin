using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.ScheduledTasks;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.IO
{
    /// <summary>
    /// Class DirectoryWatchers
    /// </summary>
    public class DirectoryWatchers : IDirectoryWatchers
    {
        /// <summary>
        /// The file system watchers
        /// </summary>
        private readonly ConcurrentDictionary<string, FileSystemWatcher> _fileSystemWatchers = new ConcurrentDictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// The update timer
        /// </summary>
        private Timer _updateTimer;
        /// <summary>
        /// The affected paths
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _affectedPaths = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// A dynamic list of paths that should be ignored.  Added to during our own file sytem modifications.
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _tempIgnoredPaths = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Any file name ending in any of these will be ignored by the watchers
        /// </summary>
        private readonly IReadOnlyList<string> _alwaysIgnoreFiles = new List<string> { "thumbs.db", "small.jpg", "albumart.jpg" };

        /// <summary>
        /// The timer lock
        /// </summary>
        private readonly object _timerLock = new object();

        /// <summary>
        /// Add the path to our temporary ignore list.  Use when writing to a path within our listening scope.
        /// </summary>
        /// <param name="path">The path.</param>
        public void TemporarilyIgnore(string path)
        {
            _tempIgnoredPaths[path] = path;
        }

        /// <summary>
        /// Removes the temp ignore.
        /// </summary>
        /// <param name="path">The path.</param>
        public async void RemoveTempIgnore(string path)
        {
            // This is an arbitraty amount of time, but delay it because file system writes often trigger events after RemoveTempIgnore has been called. 
            await Task.Delay(1500).ConfigureAwait(false);

            string val;
            _tempIgnoredPaths.TryRemove(path, out val);
        }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        private ITaskManager TaskManager { get; set; }

        private ILibraryManager LibraryManager { get; set; }
        private IServerConfigurationManager ConfigurationManager { get; set; }

        private readonly IFileSystem _fileSystem;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryWatchers" /> class.
        /// </summary>
        public DirectoryWatchers(ILogManager logManager, ITaskManager taskManager, ILibraryManager libraryManager, IServerConfigurationManager configurationManager, IFileSystem fileSystem)
        {
            if (taskManager == null)
            {
                throw new ArgumentNullException("taskManager");
            }

            LibraryManager = libraryManager;
            TaskManager = taskManager;
            Logger = logManager.GetLogger("DirectoryWatchers");
            ConfigurationManager = configurationManager;
            _fileSystem = fileSystem;

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        /// <summary>
        /// Handles the PowerModeChanged event of the SystemEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PowerModeChangedEventArgs"/> instance containing the event data.</param>
        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            Stop();
            Start();
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            LibraryManager.ItemAdded += LibraryManager_ItemAdded;
            LibraryManager.ItemRemoved += LibraryManager_ItemRemoved;

            var pathsToWatch = new List<string> { LibraryManager.RootFolder.Path };

            var paths = LibraryManager
                .RootFolder
                .Children
                .OfType<Folder>()
                .Where(i => i.LocationType != LocationType.Remote && i.LocationType != LocationType.Virtual)
                .SelectMany(f =>
                    {
                        try
                        {
                            // Accessing ResolveArgs could involve file system access
                            return f.ResolveArgs.PhysicalLocations;
                        }
                        catch (IOException)
                        {
                            return new string[] { };
                        }

                    })
                .Where(Path.IsPathRooted)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToList();

            foreach (var path in paths)
            {
                if (!ContainsParentFolder(pathsToWatch, path))
                {
                    pathsToWatch.Add(path);
                }
            }

            foreach (var path in pathsToWatch)
            {
                StartWatchingPath(path);
            }
        }

        /// <summary>
        /// Handles the ItemRemoved event of the LibraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void LibraryManager_ItemRemoved(object sender, ItemChangeEventArgs e)
        {
            if (e.Item.Parent is AggregateFolder)
            {
                StopWatchingPath(e.Item.Path);
            }
        }

        /// <summary>
        /// Handles the ItemAdded event of the LibraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void LibraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (e.Item.Parent is AggregateFolder)
            {
                StartWatchingPath(e.Item.Path);
            }
        }

        /// <summary>
        /// Examine a list of strings assumed to be file paths to see if it contains a parent of
        /// the provided path.
        /// </summary>
        /// <param name="lst">The LST.</param>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [contains parent folder] [the specified LST]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        private static bool ContainsParentFolder(IEnumerable<string> lst, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            path = path.TrimEnd(Path.DirectorySeparatorChar);

            return lst.Any(str =>
            {
                //this should be a little quicker than examining each actual parent folder...
                var compare = str.TrimEnd(Path.DirectorySeparatorChar);

                return (path.Equals(compare, StringComparison.OrdinalIgnoreCase) || (path.StartsWith(compare, StringComparison.OrdinalIgnoreCase) && path[compare.Length] == Path.DirectorySeparatorChar));
            });
        }

        /// <summary>
        /// Starts the watching path.
        /// </summary>
        /// <param name="path">The path.</param>
        private void StartWatchingPath(string path)
        {
            // Creating a FileSystemWatcher over the LAN can take hundreds of milliseconds, so wrap it in a Task to do them all in parallel
            Task.Run(() =>
            {
                var newWatcher = new FileSystemWatcher(path, "*") { IncludeSubdirectories = true, InternalBufferSize = 32767 };

                newWatcher.Created += watcher_Changed;
                newWatcher.Deleted += watcher_Changed;
                newWatcher.Renamed += watcher_Changed;
                newWatcher.Changed += watcher_Changed;

                newWatcher.Error += watcher_Error;

                try
                {
                    if (_fileSystemWatchers.TryAdd(path, newWatcher))
                    {
                        newWatcher.EnableRaisingEvents = true;
                        Logger.Info("Watching directory " + path);
                    }
                    else
                    {
                        Logger.Info("Unable to add directory watcher for {0}. It already exists in the dictionary." + path);
                        newWatcher.Dispose();
                    }

                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error watching path: {0}", ex, path);
                }
                catch (PlatformNotSupportedException ex)
                {
                    Logger.ErrorException("Error watching path: {0}", ex, path);
                }
            });
        }

        /// <summary>
        /// Stops the watching path.
        /// </summary>
        /// <param name="path">The path.</param>
        private void StopWatchingPath(string path)
        {
            FileSystemWatcher watcher;

            if (_fileSystemWatchers.TryGetValue(path, out watcher))
            {
                DisposeWatcher(watcher);
            }
        }

        /// <summary>
        /// Disposes the watcher.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        private void DisposeWatcher(FileSystemWatcher watcher)
        {
            Logger.Info("Stopping directory watching for path {0}", watcher.Path);

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();

            RemoveWatcherFromList(watcher);
        }

        /// <summary>
        /// Removes the watcher from list.
        /// </summary>
        /// <param name="watcher">The watcher.</param>
        private void RemoveWatcherFromList(FileSystemWatcher watcher)
        {
            FileSystemWatcher removed;

            _fileSystemWatchers.TryRemove(watcher.Path, out removed);
        }

        /// <summary>
        /// Handles the Error event of the watcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ErrorEventArgs" /> instance containing the event data.</param>
        void watcher_Error(object sender, ErrorEventArgs e)
        {
            var ex = e.GetException();
            var dw = (FileSystemWatcher)sender;

            Logger.ErrorException("Error in Directory watcher for: " + dw.Path, ex);

            DisposeWatcher(dw);
        }

        /// <summary>
        /// Handles the Changed event of the watcher control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FileSystemEventArgs" /> instance containing the event data.</param>
        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                OnWatcherChanged(e);
            }
            catch (IOException ex)
            {
                Logger.ErrorException("IOException in watcher changed. Path: {0}", ex, e.FullPath);
            }
        }

        private void OnWatcherChanged(FileSystemEventArgs e)
        {
            var name = e.Name;

            // Ignore certain files
            if (_alwaysIgnoreFiles.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            var nameFromFullPath = Path.GetFileName(e.FullPath);
            // Ignore certain files
            if (!string.IsNullOrEmpty(nameFromFullPath) && _alwaysIgnoreFiles.Contains(nameFromFullPath, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            // Ignore when someone manually creates a new folder
            if (e.ChangeType == WatcherChangeTypes.Created && name == "New folder")
            {
                return;
            }

            var tempIgnorePaths = _tempIgnoredPaths.Keys.ToList();

            // If the parent of an ignored path has a change event, ignore that too
            if (tempIgnorePaths.Any(i =>
            {
                if (string.Equals(i, e.FullPath, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Debug("Watcher ignoring change to {0}", e.FullPath);
                    return true;
                }

                // Go up a level
                var parent = Path.GetDirectoryName(i);
                if (string.Equals(parent, e.FullPath, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Debug("Watcher ignoring change to {0}", e.FullPath);
                    return true;
                }

                // Go up another level
                if (!string.IsNullOrEmpty(parent))
                {
                    parent = Path.GetDirectoryName(i);
                    if (string.Equals(parent, e.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Debug("Watcher ignoring change to {0}", e.FullPath);
                        return true;
                    }
                }

                if (i.StartsWith(e.FullPath, StringComparison.OrdinalIgnoreCase) || 
                    e.FullPath.StartsWith(i, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Debug("Watcher ignoring change to {0}", e.FullPath);
                    return true;
                }

                return false;

            }))
            {
                return;
            }

            Logger.Info("Watcher sees change of type " + e.ChangeType + " to " + e.FullPath);

            //Since we're watching created, deleted and renamed we always want the parent of the item to be the affected path
            var affectedPath = e.FullPath;

            _affectedPaths.AddOrUpdate(affectedPath, affectedPath, (key, oldValue) => affectedPath);

            lock (_timerLock)
            {
                if (_updateTimer == null)
                {
                    _updateTimer = new Timer(TimerStopped, null, TimeSpan.FromSeconds(ConfigurationManager.Configuration.FileWatcherDelay), TimeSpan.FromMilliseconds(-1));
                }
                else
                {
                    _updateTimer.Change(TimeSpan.FromSeconds(ConfigurationManager.Configuration.FileWatcherDelay), TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        /// <summary>
        /// Timers the stopped.
        /// </summary>
        /// <param name="stateInfo">The state info.</param>
        private async void TimerStopped(object stateInfo)
        {
            lock (_timerLock)
            {
                // Extend the timer as long as any of the paths are still being written to.
                if (_affectedPaths.Any(p => IsFileLocked(p.Key)))
                {
                    Logger.Info("Timer extended.");
                    _updateTimer.Change(TimeSpan.FromSeconds(ConfigurationManager.Configuration.FileWatcherDelay), TimeSpan.FromMilliseconds(-1));
                    return;
                }

                Logger.Info("Timer stopped.");

                if (_updateTimer != null)
                {
                    _updateTimer.Dispose();
                    _updateTimer = null;
                }
            }

            var paths = _affectedPaths.Keys.ToList();
            _affectedPaths.Clear();

            await ProcessPathChanges(paths).ConfigureAwait(false);
        }

        /// <summary>
        /// Try and determine if a file is locked
        /// This is not perfect, and is subject to race conditions, so I'd rather not make this a re-usable library method.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is file locked] [the specified path]; otherwise, <c>false</c>.</returns>
        private bool IsFileLocked(string path)
        {
            try
            {
                var data = _fileSystem.GetFileSystemInfo(path);

                if (!data.Exists
                    || data.Attributes.HasFlag(FileAttributes.Directory)
                    || data.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    return false;
                }
            }
            catch (IOException)
            {
                return false;
            }

            try
            {
                using (_fileSystem.GetFileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    //file is not locked
                    return false;
                }
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                Logger.Debug("{0} is locked.", path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Processes the path changes.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>Task.</returns>
        private async Task ProcessPathChanges(List<string> paths)
        {
            var itemsToRefresh = paths.Select(Path.GetDirectoryName)
                .Select(GetAffectedBaseItem)
                .Where(item => item != null)
                .Distinct()
                .ToList();

            foreach (var p in paths) Logger.Info(p + " reports change.");

            // If the root folder changed, run the library task so the user can see it
            if (itemsToRefresh.Any(i => i is AggregateFolder))
            {
                TaskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();
                return;
            }

            await Task.WhenAll(itemsToRefresh.Select(i => Task.Run(async () =>
            {
                Logger.Info(i.Name + " (" + i.Path + ") will be refreshed.");

                try
                {
                    await i.ChangedExternally().ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    // For now swallow and log. 
                    // Research item: If an IOException occurs, the item may be in a disconnected state (media unavailable)
                    // Should we remove it from it's parent?
                    Logger.ErrorException("Error refreshing {0}", ex, i.Name);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error refreshing {0}", ex, i.Name);
                }

            }))).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the affected base item.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem GetAffectedBaseItem(string path)
        {
            BaseItem item = null;

            while (item == null && !string.IsNullOrEmpty(path))
            {
                item = LibraryManager.RootFolder.FindByPath(path);

                path = Path.GetDirectoryName(path);
            }

            if (item != null)
            {
                // If the item has been deleted find the first valid parent that still exists
                while (!Directory.Exists(item.Path) && !File.Exists(item.Path))
                {
                    item = item.Parent;

                    if (item == null)
                    {
                        break;
                    }
                }
            }

            return item;
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            LibraryManager.ItemAdded -= LibraryManager_ItemAdded;
            LibraryManager.ItemRemoved -= LibraryManager_ItemRemoved;

            foreach (var watcher in _fileSystemWatchers.Values.ToList())
            {
                watcher.Changed -= watcher_Changed;
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            lock (_timerLock)
            {
                if (_updateTimer != null)
                {
                    _updateTimer.Dispose();
                    _updateTimer = null;
                }
            }

            _fileSystemWatchers.Clear();
            _affectedPaths.Clear();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                Stop();
            }
        }
    }
}
