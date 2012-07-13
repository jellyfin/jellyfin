using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Events;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Library
{
    public class ItemController
    {
        private List<IBaseItemResolver> Resolvers = new List<IBaseItemResolver>();

        /// <summary>
        /// Registers a new BaseItem resolver.
        /// </summary>
        public void AddResovler<TBaseItemType, TResolverType>()
            where TBaseItemType : BaseItem, new()
            where TResolverType : BaseItemResolver<TBaseItemType>, new()
        {
            Resolvers.Insert(0, new TResolverType());
        }

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

        #region Item Events
        /// <summary>
        /// Called when an item is being created.
        /// This should be used to fill item values, such as metadata
        /// </summary>
        public event EventHandler<GenericItemEventArgs<BaseItem>> ItemCreating;

        /// <summary>
        /// Called when an item has been created.
        /// This should be used to process or modify item values.
        /// </summary>
        public event EventHandler<GenericItemEventArgs<BaseItem>> ItemCreated;
        #endregion

        /// <summary>
        /// Called when an item has been created
        /// </summary>
        private void OnItemCreated(BaseItem item, Folder parent)
        {
            GenericItemEventArgs<BaseItem> args = new GenericItemEventArgs<BaseItem> { Item = item };

            if (ItemCreating != null)
            {
                ItemCreating(this, args);
            }

            if (ItemCreated != null)
            {
                ItemCreated(this, args);
            }
        }

        private void FireCreateEventsRecursive(Folder folder, Folder parent)
        {
            OnItemCreated(folder, parent);

            int count = folder.Children.Length;

            Parallel.For(0, count, i =>
            {
                BaseItem item = folder.Children[i];

                Folder childFolder = item as Folder;

                if (childFolder != null)
                {
                    FireCreateEventsRecursive(childFolder, folder);
                }
                else
                {
                    OnItemCreated(item, folder);
                }
            });
        }

        private BaseItem ResolveItem(ItemResolveEventArgs args)
        {
            // If that didn't pan out, try the slow ones
            foreach (IBaseItemResolver resolver in Resolvers)
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
        public BaseItem GetItem(string path)
        {
            return GetItem(null, path);
        }

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        public BaseItem GetItem(Folder parent, string path)
        {
            BaseItem item = GetItemInternal(parent, path, File.GetAttributes(path));

            if (item != null)
            {
                var folder = item as Folder;

                if (folder != null)
                {
                    FireCreateEventsRecursive(folder, parent);
                }
                else
                {
                    OnItemCreated(item, parent);
                }
            }

            return item;
        }

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        private BaseItem GetItemInternal(Folder parent, string path, FileAttributes attributes)
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

            BaseItem item = ResolveItem(args);

            var folder = item as Folder;

            if (folder != null)
            {
                // If it's a folder look for child entities
                AttachChildren(folder, fileSystemChildren);
            }

            return item;
        }

        /// <summary>
        /// Finds child BaseItems for a given Folder
        /// </summary>
        private void AttachChildren(Folder folder, IEnumerable<KeyValuePair<string, FileAttributes>> fileSystemChildren)
        {
            List<BaseItem> baseItemChildren = new List<BaseItem>();

            int count = fileSystemChildren.Count();

            // Resolve the child folder paths into entities
            Parallel.For(0, count, i =>
            {
                KeyValuePair<string, FileAttributes> child = fileSystemChildren.ElementAt(i);

                BaseItem item = GetItemInternal(folder, child.Key, child.Value);

                if (item != null)
                {
                    lock (baseItemChildren)
                    {
                        baseItemChildren.Add(item);
                    }
                }
            });

            // Sort them
            folder.Children = baseItemChildren.OrderBy(f =>
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
    }
}
