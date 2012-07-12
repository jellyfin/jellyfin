using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Resolvers
{
    public abstract class BaseItemResolver<T> : IBaseItemResolver
        where T : BaseItem, new ()
    {
        protected virtual T Resolve(ItemResolveEventArgs args)
        {
            return null;
        }

        protected virtual void SetItemValues(T item, ItemResolveEventArgs args)
        {
            // If the subclass didn't specify this
            if (string.IsNullOrEmpty(item.Path))
            {
                item.Path = args.Path;
            }

            Folder parentFolder = args.Parent as Folder;

            if (parentFolder != null)
            {
                item.Parent = parentFolder;
            }

            item.Id = Kernel.GetMD5(item.Path);
            
            PopulateImages(item, args);
            PopulateLocalTrailers(item, args);
        }

        public BaseItem ResolvePath(ItemResolveEventArgs args)
        {
            T item = Resolve(args);
            
            if (item != null)
            {
                SetItemValues(item, args);

                EnsureName(item);
                EnsureDates(item);
            }

            return item;
        }

        private void EnsureName(T item)
        {
            // If the subclass didn't supply a name, add it here
            if (string.IsNullOrEmpty(item.Name))
            {
                item.Name = Path.GetFileNameWithoutExtension(item.Path);
            }

        }

        private void EnsureDates(T item)
        {
            // If the subclass didn't supply dates, add them here
            if (item.DateCreated == DateTime.MinValue)
            {
                item.DateCreated = Path.IsPathRooted(item.Path) ? File.GetCreationTime(item.Path) : DateTime.Now;
            }

            if (item.DateModified == DateTime.MinValue)
            {
                item.DateModified = Path.IsPathRooted(item.Path) ? File.GetLastWriteTime(item.Path) : DateTime.Now;
            }
        }

        protected virtual void PopulateImages(T item, ItemResolveEventArgs args)
        {
            List<string> backdropFiles = new List<string>();

            foreach (KeyValuePair<string,FileAttributes> file in args.FileSystemChildren)
            {
                if (file.Value.HasFlag(FileAttributes.Directory))
                {
                    continue;
                }

                string filePath = file.Key;

                string ext = Path.GetExtension(filePath);

                if (!ext.EndsWith("png", StringComparison.OrdinalIgnoreCase) && !ext.EndsWith("jpg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(filePath);

                if (name.Equals("folder", StringComparison.OrdinalIgnoreCase))
                {
                    item.PrimaryImagePath = filePath;
                }
                else if (name.StartsWith("backdrop", StringComparison.OrdinalIgnoreCase))
                {
                    backdropFiles.Add(filePath);
                }
                if (name.Equals("logo", StringComparison.OrdinalIgnoreCase))
                {
                    item.LogoImagePath = filePath;
                }
                if (name.Equals("banner", StringComparison.OrdinalIgnoreCase))
                {
                    item.BannerImagePath = filePath;
                }
                if (name.Equals("art", StringComparison.OrdinalIgnoreCase))
                {
                    item.ArtImagePath = filePath;
                }
                if (name.Equals("thumb", StringComparison.OrdinalIgnoreCase))
                {
                    item.ThumbnailImagePath = filePath;
                }
            }

            item.BackdropImagePaths = backdropFiles;
        }

        protected virtual void PopulateLocalTrailers(T item, ItemResolveEventArgs args)
        {
            var trailerPath = args.GetFolderByName("trailers");

            if (trailerPath.HasValue)
            {
                string[] allFiles = Directory.GetFileSystemEntries(trailerPath.Value.Key, "*", SearchOption.TopDirectoryOnly);

                item.LocalTrailers = allFiles.Select(f => Kernel.Instance.ItemController.GetItem(f)).OfType<Video>();
            }
        }
    }

    public interface IBaseItemResolver
    {
        BaseItem ResolvePath(ItemResolveEventArgs args);
    }
}
