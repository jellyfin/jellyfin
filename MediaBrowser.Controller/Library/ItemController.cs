using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Library
{
    public class ItemController
    {
        #region PreBeginResolvePath Event
        /// <summary>
        /// Fires when a path is about to be resolved, but before child folders and files 
        /// have been collected from the file system.
        /// This gives listeners a chance to cancel the operation and cause the path to be ignored.
        /// </summary>
        public event EventHandler<PreBeginResolveEventArgs> PreBeginResolvePath;
        private bool OnPreBeginResolvePath(Folder parent, string path, WIN32_FIND_DATA fileData)
        {
            PreBeginResolveEventArgs args = new PreBeginResolveEventArgs()
            {
                Path = path,
                Parent = parent,
                FileData = fileData,
                Cancel = false
            };

            if (PreBeginResolvePath != null)
            {
                PreBeginResolvePath(this, args);
            }

            return !args.Cancel;
        }
        #endregion

        #region BeginResolvePath Event
        /// <summary>
        /// Fires when a path is about to be resolved, but after child folders and files 
        /// have been collected from the file system.
        /// This gives listeners a chance to cancel the operation and cause the path to be ignored.
        /// </summary>
        public event EventHandler<ItemResolveEventArgs> BeginResolvePath;
        private bool OnBeginResolvePath(ItemResolveEventArgs args)
        {
            if (BeginResolvePath != null)
            {
                BeginResolvePath(this, args);
            }

            return !args.Cancel;
        }
        #endregion

        private BaseItem ResolveItem(ItemResolveEventArgs args)
        {
            // Try first priority resolvers
            foreach (IBaseItemResolver resolver in Kernel.Instance.EntityResolvers.Where(p => p.Priority == ResolverPriority.First))
            {
                var item = resolver.ResolvePath(args);

                if (item != null)
                {
                    return item;
                }
            }

            // Try second priority resolvers
            foreach (IBaseItemResolver resolver in Kernel.Instance.EntityResolvers.Where(p => p.Priority == ResolverPriority.Second))
            {
                var item = resolver.ResolvePath(args);

                if (item != null)
                {
                    return item;
                }
            }

            // Try third priority resolvers
            foreach (IBaseItemResolver resolver in Kernel.Instance.EntityResolvers.Where(p => p.Priority == ResolverPriority.Third))
            {
                var item = resolver.ResolvePath(args);

                if (item != null)
                {
                    return item;
                }
            }

            // Try last priority resolvers
            foreach (IBaseItemResolver resolver in Kernel.Instance.EntityResolvers.Where(p => p.Priority == ResolverPriority.Last))
            {
                var item = resolver.ResolvePath(args);

                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        public async Task<BaseItem> GetItem(Folder parent, string path)
        {
            WIN32_FIND_DATA fileData = FileData.GetFileData(path);

            return await GetItemInternal(parent, path, fileData).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        private async Task<BaseItem> GetItemInternal(Folder parent, string path, WIN32_FIND_DATA fileData)
        {
            if (!OnPreBeginResolvePath(parent, path, fileData))
            {
                return null;
            }

            IEnumerable<KeyValuePair<string, WIN32_FIND_DATA>> fileSystemChildren;

            // Gather child folder and files
            if (fileData.IsDirectory)
            {
                fileSystemChildren = Directory.GetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly).Select(f => new KeyValuePair<string, WIN32_FIND_DATA>(f, FileData.GetFileData(f)));

                bool isVirtualFolder = parent != null && parent.IsRoot;
                fileSystemChildren = FilterChildFileSystemEntries(fileSystemChildren, isVirtualFolder);
            }
            else
            {
                fileSystemChildren = new KeyValuePair<string, WIN32_FIND_DATA>[] { };
            }

            ItemResolveEventArgs args = new ItemResolveEventArgs()
            {
                Path = path,
                FileSystemChildren = fileSystemChildren,
                FileData = fileData,
                Parent = parent,
                Cancel = false
            };

            // Fire BeginResolvePath to see if anyone wants to cancel this operation
            if (!OnBeginResolvePath(args))
            {
                return null;
            }

            BaseItem item = ResolveItem(args);

            if (item != null)
            {
                await Kernel.Instance.ExecuteMetadataProviders(item, args);

                var folder = item as Folder;

                if (folder != null)
                {
                    // If it's a folder look for child entities
                    await AttachChildren(folder, fileSystemChildren).ConfigureAwait(false);
                }
            }

            return item;
        }

        /// <summary>
        /// Finds child BaseItems for a given Folder
        /// </summary>
        private async Task AttachChildren(Folder folder, IEnumerable<KeyValuePair<string, WIN32_FIND_DATA>> fileSystemChildren)
        {
            KeyValuePair<string, WIN32_FIND_DATA>[] fileSystemChildrenArray = fileSystemChildren.ToArray();

            int count = fileSystemChildrenArray.Length;

            Task<BaseItem>[] tasks = new Task<BaseItem>[count];

            for (int i = 0; i < count; i++)
            {
                var child = fileSystemChildrenArray[i];

                tasks[i] = GetItemInternal(folder, child.Key, child.Value);
            }

            BaseItem[] baseItemChildren = await Task<BaseItem>.WhenAll(tasks).ConfigureAwait(false);

            // Sort them
            folder.Children = baseItemChildren.Where(i => i != null).OrderBy(f =>
            {
                return string.IsNullOrEmpty(f.SortName) ? f.Name : f.SortName;

            }).ToArray();
        }

        /// <summary>
        /// Transforms shortcuts into their actual paths
        /// </summary>
        private List<KeyValuePair<string, WIN32_FIND_DATA>> FilterChildFileSystemEntries(IEnumerable<KeyValuePair<string, WIN32_FIND_DATA>> fileSystemChildren, bool flattenShortcuts)
        {
            List<KeyValuePair<string, WIN32_FIND_DATA>> returnFiles = new List<KeyValuePair<string, WIN32_FIND_DATA>>();

            // Loop through each file
            foreach (KeyValuePair<string, WIN32_FIND_DATA> file in fileSystemChildren)
            {
                // Folders
                if (file.Value.IsDirectory)
                {
                    returnFiles.Add(file);
                }

                // If it's a shortcut, resolve it
                else if (Shortcut.IsShortcut(file.Key))
                {
                    string newPath = Shortcut.ResolveShortcut(file.Key);
                    WIN32_FIND_DATA newPathData = FileData.GetFileData(newPath);

                    // Find out if the shortcut is pointing to a directory or file

                    if (newPathData.IsDirectory)
                    {
                        // If we're flattening then get the shortcut's children

                        if (flattenShortcuts)
                        {
                            IEnumerable<KeyValuePair<string, WIN32_FIND_DATA>> newChildren = Directory.GetFileSystemEntries(newPath, "*", SearchOption.TopDirectoryOnly).Select(f => new KeyValuePair<string, WIN32_FIND_DATA>(f, FileData.GetFileData(f)));

                            returnFiles.AddRange(FilterChildFileSystemEntries(newChildren, false));
                        }
                        else
                        {
                            returnFiles.Add(new KeyValuePair<string, WIN32_FIND_DATA>(newPath, newPathData));
                        }
                    }
                    else
                    {
                        returnFiles.Add(new KeyValuePair<string, WIN32_FIND_DATA>(newPath, newPathData));
                    }
                }
                else
                {
                    returnFiles.Add(file);
                }
            }

            return returnFiles;
        }

        /// <summary>
        /// Gets a Person
        /// </summary>
        public async Task<Person> GetPerson(string name)
        {
            string path = Path.Combine(Kernel.Instance.ApplicationPaths.PeoplePath, name);

            return await GetImagesByNameItem<Person>(path, name).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        public async Task<Studio> GetStudio(string name)
        {
            string path = Path.Combine(Kernel.Instance.ApplicationPaths.StudioPath, name);

            return await GetImagesByNameItem<Studio>(path, name).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        public async Task<Genre> GetGenre(string name)
        {
            string path = Path.Combine(Kernel.Instance.ApplicationPaths.GenrePath, name);

            return await GetImagesByNameItem<Genre>(path, name).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a Year
        /// </summary>
        public async Task<Year> GetYear(int value)
        {
            string path = Path.Combine(Kernel.Instance.ApplicationPaths.YearPath, value.ToString());

            return await GetImagesByNameItem<Year>(path, value.ToString()).ConfigureAwait(false);
        }

        private ConcurrentDictionary<string, object> ImagesByNameItemCache = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Generically retrieves an IBN item
        /// </summary>
        private async Task<T> GetImagesByNameItem<T>(string path, string name)
            where T : BaseEntity, new()
        {
            string key = path.ToLower();

            // Look for it in the cache, if it's not there, create it
            if (!ImagesByNameItemCache.ContainsKey(key))
            {
                T obj = await CreateImagesByNameItem<T>(path, name).ConfigureAwait(false);
                ImagesByNameItemCache[key] = obj;
                return obj;
            }

            return ImagesByNameItemCache[key] as T;
        }

        /// <summary>
        /// Creates an IBN item based on a given path
        /// </summary>
        private async Task<T> CreateImagesByNameItem<T>(string path, string name)
            where T : BaseEntity, new()
        {
            T item = new T();

            item.Name = name;
            item.Id = Kernel.GetMD5(path);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            item.DateCreated = Directory.GetCreationTime(path);
            item.DateModified = Directory.GetLastAccessTime(path);

            ItemResolveEventArgs args = new ItemResolveEventArgs();
            args.Path = path;
            args.FileData = FileData.GetFileData(path);
            args.FileSystemChildren = Directory.GetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly).Select(f => new KeyValuePair<string, WIN32_FIND_DATA>(f, FileData.GetFileData(f)));

            await Kernel.Instance.ExecuteMetadataProviders(item, args).ConfigureAwait(false);

            return item;
        }
    }
}
