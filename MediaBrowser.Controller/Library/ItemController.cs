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
        private bool OnPreBeginResolvePath(Folder parent, string path, FileAttributes attributes)
        {
            PreBeginResolveEventArgs args = new PreBeginResolveEventArgs()
            {
                Path = path,
                Parent = parent,
                FileAttributes = attributes,
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

        private async Task<BaseItem> ResolveItem(ItemResolveEventArgs args)
        {
            // If that didn't pan out, try the slow ones
            foreach (IBaseItemResolver resolver in Kernel.Instance.EntityResolvers)
            {
                var item = await resolver.ResolvePath(args);

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
            return await GetItemInternal(parent, path, File.GetAttributes(path));
        }

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        private async Task<BaseItem> GetItemInternal(Folder parent, string path, FileAttributes attributes)
        {
            if (!OnPreBeginResolvePath(parent, path, attributes))
            {
                return null;
            }

            IEnumerable<KeyValuePair<string, FileAttributes>> fileSystemChildren;

            // Gather child folder and files
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                fileSystemChildren = Directory.GetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly).Select(f => new KeyValuePair<string, FileAttributes>(f, File.GetAttributes(f)));

                bool isVirtualFolder = parent != null && parent.IsRoot;
                fileSystemChildren = FilterChildFileSystemEntries(fileSystemChildren, isVirtualFolder);
            }
            else
            {
                fileSystemChildren = new KeyValuePair<string, FileAttributes>[] { };
            }

            ItemResolveEventArgs args = new ItemResolveEventArgs()
            {
                Path = path,
                FileAttributes = attributes,
                FileSystemChildren = fileSystemChildren,
                Parent = parent,
                Cancel = false
            };

            // Fire BeginResolvePath to see if anyone wants to cancel this operation
            if (!OnBeginResolvePath(args))
            {
                return null;
            }

            BaseItem item = await ResolveItem(args);

            var folder = item as Folder;

            if (folder != null)
            {
                // If it's a folder look for child entities
                await AttachChildren(folder, fileSystemChildren);
            }

            return item;
        }

        /// <summary>
        /// Finds child BaseItems for a given Folder
        /// </summary>
        private async Task AttachChildren(Folder folder, IEnumerable<KeyValuePair<string, FileAttributes>> fileSystemChildren)
        {
            KeyValuePair<string, FileAttributes>[] fileSystemChildrenArray = fileSystemChildren.ToArray();

            int count = fileSystemChildrenArray.Length;

            Task<BaseItem>[] tasks = new Task<BaseItem>[count];

            for (int i = 0; i < count; i++)
            {
                var child = fileSystemChildrenArray[i];

                tasks[i] = GetItemInternal(folder, child.Key, child.Value);
            }

            BaseItem[] baseItemChildren = await Task<BaseItem>.WhenAll(tasks);
            
            // Sort them
            folder.Children = baseItemChildren.Where(i => i != null).OrderBy(f =>
            {
                return string.IsNullOrEmpty(f.SortName) ? f.Name : f.SortName;

            }).ToArray();
        }

        /// <summary>
        /// Transforms shortcuts into their actual paths
        /// </summary>
        private List<KeyValuePair<string, FileAttributes>> FilterChildFileSystemEntries(IEnumerable<KeyValuePair<string, FileAttributes>> fileSystemChildren, bool flattenShortcuts)
        {
            List<KeyValuePair<string, FileAttributes>> returnFiles = new List<KeyValuePair<string, FileAttributes>>();

            // Loop through each file
            foreach (KeyValuePair<string, FileAttributes> file in fileSystemChildren)
            {
                // Folders
                if (file.Value.HasFlag(FileAttributes.Directory))
                {
                    returnFiles.Add(file);
                }

                // If it's a shortcut, resolve it
                else if (Shortcut.IsShortcut(file.Key))
                {
                    string newPath = Shortcut.ResolveShortcut(file.Key);
                    FileAttributes newPathAttributes = File.GetAttributes(newPath);

                    // Find out if the shortcut is pointing to a directory or file

                    if (newPathAttributes.HasFlag(FileAttributes.Directory))
                    {
                        // If we're flattening then get the shortcut's children

                        if (flattenShortcuts)
                        {
                            IEnumerable<KeyValuePair<string, FileAttributes>> newChildren = Directory.GetFileSystemEntries(newPath, "*", SearchOption.TopDirectoryOnly).Select(f => new KeyValuePair<string, FileAttributes>(f, File.GetAttributes(f)));

                            returnFiles.AddRange(FilterChildFileSystemEntries(newChildren, false));
                        }
                        else
                        {
                            returnFiles.Add(new KeyValuePair<string, FileAttributes>(newPath, newPathAttributes));
                        }
                    }
                    else
                    {
                        returnFiles.Add(new KeyValuePair<string, FileAttributes>(newPath, newPathAttributes));
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

            return await GetImagesByNameItem<Person>(path, name);
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        public async Task<Studio> GetStudio(string name)
        {
            string path = Path.Combine(Kernel.Instance.ApplicationPaths.StudioPath, name);

            return await GetImagesByNameItem<Studio>(path, name);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        public async Task<Genre> GetGenre(string name)
        {
            string path = Path.Combine(Kernel.Instance.ApplicationPaths.GenrePath, name);

            return await GetImagesByNameItem<Genre>(path, name);
        }

        /// <summary>
        /// Gets a Year
        /// </summary>
        public async Task<Year> GetYear(int value)
        {
            string path = Path.Combine(Kernel.Instance.ApplicationPaths.YearPath, value.ToString());

            return await GetImagesByNameItem<Year>(path, value.ToString());
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
                T obj = await CreateImagesByNameItem<T>(path, name);
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
            args.FileAttributes = File.GetAttributes(path);
            args.FileSystemChildren = Directory.GetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly).Select(f => new KeyValuePair<string, FileAttributes>(f, File.GetAttributes(f)));

            await Kernel.Instance.ExecuteMetadataProviders(item, args);

            return item;
        }
    }
}
