using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Configuration;
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
using CommonIO;
using MediaBrowser.Controller;

namespace MediaBrowser.Server.Implementations.IO
{
    public class LibraryMonitor : ILibraryMonitor
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
        private readonly IReadOnlyList<string> _alwaysIgnoreFiles = new List<string>
        {
            "thumbs.db", 
            "small.jpg", 
            "albumart.jpg",

            // WMC temp recording directories that will constantly be written to
            "TempRec",
            "TempSBE"
        };

        /// <summary>
        /// The timer lock
        /// </summary>
        private readonly object _timerLock = new object();

        /// <summary>
        /// Add the path to our temporary ignore list.  Use when writing to a path within our listening scope.
        /// </summary>
        /// <param name="path">The path.</param>
        private void TemporarilyIgnore(string path)
        {
            _tempIgnoredPaths[path] = path;
        }

        public void ReportFileSystemChangeBeginning(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            TemporarilyIgnore(path);
        }

        public bool IsPathLocked(string path)
        {
            var lockedPaths = _tempIgnoredPaths.Keys.ToList();
            return lockedPaths.Any(i => string.Equals(i, path, StringComparison.OrdinalIgnoreCase) || _fileSystem.ContainsSubPath(i, path));
        }

        public async void ReportFileSystemChangeComplete(string path, bool refreshPath)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            // This is an arbitraty amount of time, but delay it because file system writes often trigger events long after the file was actually written to.
            // Seeing long delays in some situations, especially over the network, sometimes up to 45 seconds
            // But if we make this delay too high, we risk missing legitimate changes, such as user adding a new file, or hand-editing metadata
            await Task.Delay(25000).ConfigureAwait(false);

            string val;
            _tempIgnoredPaths.TryRemove(path, out val);

            if (refreshPath)
            {
                ReportFileSystemChanged(path);
            }
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
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryMonitor" /> class.
        /// </summary>
        public LibraryMonitor(ILogManager logManager, ITaskManager taskManager, ILibraryManager libraryManager, IServerConfigurationManager configurationManager, IFileSystem fileSystem, IServerApplicationHost appHost)
        {
            if (taskManager == null)
            {
                throw new ArgumentNullException("taskManager");
            }

            LibraryManager = libraryManager;
            TaskManager = taskManager;
            Logger = logManager.GetLogger(GetType().Name);
            ConfigurationManager = configurationManager;
            _fileSystem = fileSystem;
            _appHost = appHost;

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        /// <summary>
        /// Handles the PowerModeChanged event of the SystemEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PowerModeChangedEventArgs"/> instance containing the event data.</param>
        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            Restart();
        }

        private void Restart()
        {
            Stop();
            Start();
        }

        private bool EnableLibraryMonitor
        {
            get
            {
                switch (ConfigurationManager.Configuration.EnableLibraryMonitor)
                {
                    case AutoOnOff.Auto:
                        return Environment.OSVersion.Platform == PlatformID.Win32NT;
                    case AutoOnOff.Enabled:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public void Start()
        {
            if (EnableLibraryMonitor)
            {
                StartInternal();
            }
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        private void StartInternal()
        {
            LibraryManager.ItemAdded += LibraryManager_ItemAdded;
            LibraryManager.ItemRemoved += LibraryManager_ItemRemoved;

            var pathsToWatch = new List<string> { LibraryManager.RootFolder.Path };

            var paths = LibraryManager
                .RootFolder
                .Children
                .OfType<Folder>()
                .SelectMany(f => f.PhysicalLocations)
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
            if (e.Item.GetParent() is AggregateFolder)
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
            if (e.Item.GetParent() is AggregateFolder)
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

                return path.Equals(compare, StringComparison.OrdinalIgnoreCase) || (path.StartsWith(compare, StringComparison.OrdinalIgnoreCase) && path[compare.Length] == Path.DirectorySeparatorChar);
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
                try
                {
                    var newWatcher = new FileSystemWatcher(path, "*")
                    {
                        IncludeSubdirectories = true
                    };

                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        newWatcher.InternalBufferSize = 32767;
                    }

                    newWatcher.NotifyFilter = NotifyFilters.CreationTime |
                        NotifyFilters.DirectoryName |
                        NotifyFilters.FileName |
                        NotifyFilters.LastWrite |
                        NotifyFilters.Size |
                        NotifyFilters.Attributes;

                    newWatcher.Created += watcher_Changed;
                    newWatcher.Deleted += watcher_Changed;
                    newWatcher.Renamed += watcher_Changed;
                    newWatcher.Changed += watcher_Changed;

                    newWatcher.Error += watcher_Error;

                    if (_fileSystemWatchers.TryAdd(path, newWatcher))
                    {
                        newWatcher.EnableRaisingEvents = true;
                        Logger.Info("Watching directory " + path);
                    }
                    else
                    {
                        Logger.Info("Unable to add directory watcher for {0}. It already exists in the dictionary.", path);
                        newWatcher.Dispose();
                    }

                }
                catch (Exception ex)
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
            try
            {
                using (watcher)
                {
                    Logger.Info("Stopping directory watching for path {0}", watcher.Path);

                    watcher.EnableRaisingEvents = false;
                }
            }
            catch
            {

            }
            finally
            {
                RemoveWatcherFromList(watcher);
            }
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

            if (ConfigurationManager.Configuration.EnableLibraryMonitor == AutoOnOff.Auto)
            {
                Logger.Info("Disabling realtime monitor to prevent future instability");

                ConfigurationManager.Configuration.EnableLibraryMonitor = AutoOnOff.Disabled;
                Stop();
            }
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
                Logger.Debug("Changed detected of type " + e.ChangeType + " to " + e.FullPath);

                ReportFileSystemChanged(e.FullPath);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Exception in ReportFileSystemChanged. Path: {0}", ex, e.FullPath);
            }
        }

        public void ReportFileSystemChanged(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var filename = Path.GetFileName(path);

            var monitorPath = !(!string.IsNullOrEmpty(filename) && _alwaysIgnoreFiles.Contains(filename, StringComparer.OrdinalIgnoreCase));

            // Ignore certain files
            var tempIgnorePaths = _tempIgnoredPaths.Keys.ToList();

            // If the parent of an ignored path has a change event, ignore that too
            if (tempIgnorePaths.Any(i =>
            {
                if (string.Equals(i, path, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Debug("Ignoring change to {0}", path);
                    return true;
                }

                if (_fileSystem.ContainsSubPath(i, path))
                {
                    Logger.Debug("Ignoring change to {0}", path);
                    return true;
                }

                // Go up a level
                var parent = Path.GetDirectoryName(i);
                if (!string.IsNullOrEmpty(parent))
                {
                    if (string.Equals(parent, path, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Debug("Ignoring change to {0}", path);
                        return true;
                    }
                }

                return false;

            }))
            {
                monitorPath = false;
            }

            if (monitorPath)
            {
                // Avoid implicitly captured closure
                var affectedPath = path;
                _affectedPaths.AddOrUpdate(path, path, (key, oldValue) => affectedPath);
            }

            RestartTimer();
        }

        private void RestartTimer()
        {
            lock (_timerLock)
            {
                if (_updateTimer == null)
                {
                    _updateTimer = new Timer(TimerStopped, null, TimeSpan.FromSeconds(ConfigurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
                else
                {
                    _updateTimer.Change(TimeSpan.FromSeconds(ConfigurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        /// <summary>
        /// Timers the stopped.
        /// </summary>
        /// <param name="stateInfo">The state info.</param>
        private async void TimerStopped(object stateInfo)
        {
            // Extend the timer as long as any of the paths are still being written to.
            if (_affectedPaths.Any(p => IsFileLocked(p.Key)))
            {
                Logger.Info("Timer extended.");
                RestartTimer();
                return;
            }

            Logger.Debug("Timer stopped.");

            DisposeTimer();

            var paths = _affectedPaths.Keys.ToList();
            _affectedPaths.Clear();

            try
            {
                await ProcessPathChanges(paths).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error processing directory changes", ex);
            }
        }

        private bool IsFileLocked(string path)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                // Causing lockups on linux
                return false;
            }

            try
            {
                var data = _fileSystem.GetFileSystemInfo(path);

                if (!data.Exists
                    || data.IsDirectory

                    // Opening a writable stream will fail with readonly files
                    || data.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    return false;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error getting file system info for: {0}", ex, path);
                return false;
            }

            // In order to determine if the file is being written to, we have to request write access
            // But if the server only has readonly access, this is going to cause this entire algorithm to fail
            // So we'll take a best guess about our access level
            var requestedFileAccess = ConfigurationManager.Configuration.SaveLocalMeta
                ? FileAccess.ReadWrite
                : FileAccess.Read;

            try
            {
                using (_fileSystem.GetFileStream(path, FileMode.Open, requestedFileAccess, FileShare.ReadWrite))
                {
                    if (_updateTimer != null)
                    {
                        //file is not locked
                        return false;
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                // File may have been deleted
                return false;
            }
            catch (FileNotFoundException)
            {
                // File may have been deleted
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
            catch (Exception ex)
            {
                Logger.ErrorException("Error determining if file is locked: {0}", ex, path);
                return false;
            }

            return false;
        }

        private void DisposeTimer()
        {
            lock (_timerLock)
            {
                if (_updateTimer != null)
                {
                    _updateTimer.Dispose();
                    _updateTimer = null;
                }
            }
        }

        /// <summary>
        /// Processes the path changes.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>Task.</returns>
        private async Task ProcessPathChanges(List<string> paths)
        {
            var itemsToRefresh = paths
                .Select(GetAffectedBaseItem)
                .Where(item => item != null)
                .Distinct()
                .ToList();

            foreach (var p in paths)
            {
                Logger.Info(p + " reports change.");
            }

            // If the root folder changed, run the library task so the user can see it
            if (itemsToRefresh.Any(i => i is AggregateFolder))
            {
                TaskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();
                return;
            }

            foreach (var item in itemsToRefresh)
            {
                Logger.Info(item.Name + " (" + item.Path + ") will be refreshed.");

                try
                {
                    await item.ChangedExternally().ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    // For now swallow and log. 
                    // Research item: If an IOException occurs, the item may be in a disconnected state (media unavailable)
                    // Should we remove it from it's parent?
                    Logger.ErrorException("Error refreshing {0}", ex, item.Name);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error refreshing {0}", ex, item.Name);
                }
            }
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
                item = LibraryManager.FindByPath(path);

                path = Path.GetDirectoryName(path);
            }

            if (item != null)
            {
                // If the item has been deleted find the first valid parent that still exists
				while (!_fileSystem.DirectoryExists(item.Path) && !_fileSystem.FileExists(item.Path))
                {
                    item = item.GetParent();

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
                watcher.Created -= watcher_Changed;
                watcher.Deleted -= watcher_Changed;
                watcher.Renamed -= watcher_Changed;
                watcher.Changed -= watcher_Changed;

                try
                {
                    watcher.EnableRaisingEvents = false;
                }
                catch (InvalidOperationException)
                {
                    // Seeing this under mono on linux sometimes
                    // Collection was modified; enumeration operation may not execute.
                }

                watcher.Dispose();
            }

            DisposeTimer();

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

    public class LibraryMonitorStartup : IServerEntryPoint
    {
        private readonly ILibraryMonitor _monitor;

        public LibraryMonitorStartup(ILibraryMonitor monitor)
        {
            _monitor = monitor;
        }

        public void Run()
        {
            _monitor.Start();
        }

        public void Dispose()
        {
        }
    }
}
