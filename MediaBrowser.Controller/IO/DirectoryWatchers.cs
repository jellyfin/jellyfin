using MediaBrowser.Controller.Entities;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.IO
{
    public class DirectoryWatchers
    {
        private readonly List<FileSystemWatcher> FileSystemWatchers = new List<FileSystemWatcher>();
        private Timer updateTimer;
        private List<string> affectedPaths = new List<string>();

        private const int TimerDelayInSeconds = 30;

        public void Start()
        {
            var pathsToWatch = new List<string>();

            var rootFolder = Kernel.Instance.RootFolder;

            pathsToWatch.Add(rootFolder.Path);

            foreach (Folder folder in rootFolder.Children.OfType<Folder>())
            {
                foreach (string path in folder.PhysicalLocations)
                {
                    if (Path.IsPathRooted(path) && !pathsToWatch.ContainsParentFolder(path))
                    {
                        pathsToWatch.Add(path);
                    }
                }
            }

            foreach (string path in pathsToWatch)
            {
                Logger.LogInfo("Watching directory " + path + " for changes.");

                var watcher = new FileSystemWatcher(path, "*") { }; 
                watcher.IncludeSubdirectories = true;

                //watcher.Changed += watcher_Changed;

                // All the others seem to trigger change events on the parent, so let's keep it simple for now.
                //   Actually, we really need to only watch created, deleted and renamed as changed fires too much -ebr
                watcher.Created += watcher_Changed;
                watcher.Deleted += watcher_Changed;
                watcher.Renamed += watcher_Changed;

                watcher.EnableRaisingEvents = true;
                FileSystemWatchers.Add(watcher);
            }
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            Logger.LogDebugInfo("****** Watcher sees change of type " + e.ChangeType.ToString() + " to " + e.FullPath);
            lock (affectedPaths)
            {
                //Since we're watching created, deleted and renamed we always want the parent of the item to be the affected path
                var affectedPath = Path.GetDirectoryName(e.FullPath);
                
                if (e.ChangeType == WatcherChangeTypes.Renamed)
                {
                    var renamedArgs = e as RenamedEventArgs;
                    if (affectedPaths.Contains(renamedArgs.OldFullPath))
                    {
                    Logger.LogDebugInfo("****** Removing " + renamedArgs.OldFullPath + " from affected paths.");
                    affectedPaths.Remove(renamedArgs.OldFullPath);
                    }
                }

                //If anything underneath this path was already marked as affected - remove it as it will now get captured by this one
                affectedPaths.RemoveAll(p => p.StartsWith(e.FullPath, StringComparison.OrdinalIgnoreCase));
                
                if (!affectedPaths.ContainsParentFolder(affectedPath))
                {
                    Logger.LogDebugInfo("****** Adding " + affectedPath + " to affected paths.");
                    affectedPaths.Add(affectedPath);
                }
            }

            if (updateTimer == null)
            {
                updateTimer = new Timer(TimerStopped, null, TimeSpan.FromSeconds(TimerDelayInSeconds), TimeSpan.FromMilliseconds(-1));
            }
            else
            {
                updateTimer.Change(TimeSpan.FromSeconds(TimerDelayInSeconds), TimeSpan.FromMilliseconds(-1));
            }
        }

        private async void TimerStopped(object stateInfo)
        {
            updateTimer.Dispose();
            updateTimer = null;
            List<string> paths;
            lock (affectedPaths)
            {
                paths = affectedPaths;
                affectedPaths = new List<string>();
            }

            await ProcessPathChanges(paths).ConfigureAwait(false);
        }

        private Task ProcessPathChanges(IEnumerable<string> paths)
        {
            var itemsToRefresh = new List<BaseItem>();

            foreach (BaseItem item in paths.Select(p => GetAffectedBaseItem(p)))
            {
                if (item != null && !itemsToRefresh.Contains(item))
                {
                    itemsToRefresh.Add(item);
                }
            }

            if (itemsToRefresh.Any(i =>
                {
                    var folder = i as Folder;

                    return folder != null && folder.IsRoot;
                }))
            {
                return Kernel.Instance.ReloadRoot();
            }

            foreach (var p in paths) Logger.LogDebugInfo("*********  "+ p + " reports change.");
            foreach (var i in itemsToRefresh) Logger.LogDebugInfo("*********  "+i.Name + " ("+ i.Path + ") will be refreshed.");
            return Task.WhenAll(itemsToRefresh.Select(i => i.ChangedExternally()));
        }

        private BaseItem GetAffectedBaseItem(string path)
        {
            BaseItem item = null;

            while (item == null && !string.IsNullOrEmpty(path))
            {
                item = Kernel.Instance.RootFolder.FindByPath(path);

                path = Path.GetDirectoryName(path);
            }

            return item;
        }

        public void Stop()
        {
            foreach (FileSystemWatcher watcher in FileSystemWatchers)
            {
                watcher.Changed -= watcher_Changed;
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            if (updateTimer != null)
            {
                updateTimer.Dispose();
                updateTimer = null;
            }

            FileSystemWatchers.Clear();
            affectedPaths.Clear();
        }
    }
}
