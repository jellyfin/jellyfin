using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// The affected paths
        /// </summary>
        private readonly List<FileRefresher> _activeRefreshers = new List<FileRefresher>();

        /// <summary>
        /// A dynamic list of paths that should be ignored.  Added to during our own file sytem modifications.
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _tempIgnoredPaths = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Any file name ending in any of these will be ignored by the watchers
        /// </summary>
        private readonly IReadOnlyList<string> _alwaysIgnoreFiles = new List<string>
        {
            "small.jpg",
            "albumart.jpg",

            // WMC temp recording directories that will constantly be written to
            "TempRec",
            "TempSBE"
        };

        private readonly IReadOnlyList<string> _alwaysIgnoreSubstrings = new List<string>
        {
            // Synology
            "eaDir",
            "#recycle",
            ".wd_tv",
            ".actors"
        };

        private readonly IReadOnlyList<string> _alwaysIgnoreExtensions = new List<string>
        {
            // thumbs.db
            ".db",

            // bts sync files
            ".bts",
            ".sync"
        };

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
            await Task.Delay(45000).ConfigureAwait(false);

            string val;
            _tempIgnoredPaths.TryRemove(path, out val);

            if (refreshPath)
            {
                try
                {
                    ReportFileSystemChanged(path);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in ReportFileSystemChanged for {0}", ex, path);
                }
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

        private bool IsLibraryMonitorEnabaled(BaseItem item)
        {
            if (item is BasePluginFolder)
            {
                return false;
            }

            var options = LibraryManager.GetLibraryOptions(item);

            if (options != null)
            {
                return options.EnableRealtimeMonitor;
            }

            return false;
        }

        public void Start()
        {
            LibraryManager.ItemAdded += LibraryManager_ItemAdded;
            LibraryManager.ItemRemoved += LibraryManager_ItemRemoved;

            var pathsToWatch = new List<string> { };

            var paths = LibraryManager
                .RootFolder
                .Children
                .Where(IsLibraryMonitorEnabaled)
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

        private void StartWatching(BaseItem item)
        {
            if (IsLibraryMonitorEnabaled(item))
            {
                StartWatchingPath(item.Path);
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
                StartWatching(e.Item);
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
            if (string.IsNullOrWhiteSpace(path))
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

                var path = e.FullPath;

                // For deletes, use the parent path
                if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    var parentPath = Path.GetDirectoryName(path);

                    if (!string.IsNullOrWhiteSpace(parentPath))
                    {
                        path = parentPath;
                    }
                }

                ReportFileSystemChanged(path);
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
            
            var monitorPath = !string.IsNullOrEmpty(filename) &&
                !_alwaysIgnoreFiles.Contains(filename, StringComparer.OrdinalIgnoreCase) &&
                !_alwaysIgnoreExtensions.Contains(Path.GetExtension(path) ?? string.Empty, StringComparer.OrdinalIgnoreCase) &&
                _alwaysIgnoreSubstrings.All(i => path.IndexOf(i, StringComparison.OrdinalIgnoreCase) == -1);

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
                CreateRefresher(path);
            }
        }

        private void CreateRefresher(string path)
        {
            var parentPath = Path.GetDirectoryName(path);

            lock (_activeRefreshers)
            {
                var refreshers = _activeRefreshers.ToList();
                foreach (var refresher in refreshers)
                {
                    // Path is already being refreshed
                    if (string.Equals(path, refresher.Path, StringComparison.Ordinal))
                    {
                        refresher.RestartTimer();
                        return;
                    }

                    // Parent folder is already being refreshed
                    if (_fileSystem.ContainsSubPath(refresher.Path, path))
                    {
                        refresher.AddPath(path);
                        return;
                    }

                    // New path is a parent
                    if (_fileSystem.ContainsSubPath(path, refresher.Path))
                    {
                        refresher.ResetPath(path, null);
                        return;
                    }

                    // They are siblings. Rebase the refresher to the parent folder.
                    if (string.Equals(parentPath, Path.GetDirectoryName(refresher.Path), StringComparison.Ordinal))
                    {
                        refresher.ResetPath(parentPath, path);
                        return;
                    }
                }

                var newRefresher = new FileRefresher(path, _fileSystem, ConfigurationManager, LibraryManager, TaskManager, Logger);
                newRefresher.Completed += NewRefresher_Completed;
                _activeRefreshers.Add(newRefresher);
            }
        }

        private void NewRefresher_Completed(object sender, EventArgs e)
        {
            var refresher = (FileRefresher)sender;
            DisposeRefresher(refresher);
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

            _fileSystemWatchers.Clear();
            DisposeRefreshers();
        }

        private void DisposeRefresher(FileRefresher refresher)
        {
            lock (_activeRefreshers)
            {
                refresher.Dispose();
                _activeRefreshers.Remove(refresher);
            }
        }

        private void DisposeRefreshers()
        {
            lock (_activeRefreshers)
            {
                foreach (var refresher in _activeRefreshers.ToList())
                {
                    refresher.Dispose();
                }
                _activeRefreshers.Clear();
            }
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
