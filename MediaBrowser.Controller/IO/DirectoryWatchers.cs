using MediaBrowser.Controller.Entities;
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
        private List<FileSystemWatcher> FileSystemWatchers = new List<FileSystemWatcher>();
        private Timer updateTimer = null;
        private List<string> affectedPaths = new List<string>();

        private const int TimerDelayInSeconds = 5;

        public void Start()
        {
            List<string> pathsToWatch = new List<string>();

            var rootFolder = Kernel.Instance.RootFolder;

            pathsToWatch.Add(rootFolder.Path);

            foreach (Folder folder in rootFolder.Children.OfType<Folder>())
            {
                foreach (Folder subFolder in folder.Children.OfType<Folder>())
                {
                    if (Path.IsPathRooted(subFolder.Path))
                    {
                        string parent = Path.GetDirectoryName(subFolder.Path);

                        if (!pathsToWatch.Contains(parent))
                        {
                            pathsToWatch.Add(parent);
                        }
                    }
                }
            }

            foreach (string path in pathsToWatch)
            {
                FileSystemWatcher watcher = new FileSystemWatcher(path, "*");

                watcher.IncludeSubdirectories = true;

                watcher.Changed += watcher_Changed;

                // All the others seem to trigger change events on the parent, so let's keep it simple for now.
                //watcher.Created += watcher_Changed;
                //watcher.Deleted += watcher_Changed;
                //watcher.Renamed += watcher_Changed;

                watcher.EnableRaisingEvents = true;
                FileSystemWatchers.Add(watcher);
            }
        }

        void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!affectedPaths.Contains(e.FullPath))
            {
                affectedPaths.Add(e.FullPath);
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

            List<string> paths = affectedPaths;
            affectedPaths = new List<string>();

            await ProcessPathChanges(paths).ConfigureAwait(false);
        }

        private Task ProcessPathChanges(IEnumerable<string> paths)
        {
            List<BaseItem> itemsToRefresh = new List<BaseItem>();

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
            else
            {
                return Task.WhenAll(itemsToRefresh.Select(i => Kernel.Instance.ReloadItem(i)));
            }
        }

        private BaseItem GetAffectedBaseItem(string path)
        {
            BaseItem item = null;

            while (item == null)
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
