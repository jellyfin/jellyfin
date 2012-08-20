using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Resolvers
{
    public abstract class BaseItemResolver<T> : IBaseItemResolver
        where T : BaseItem, new()
    {
        protected virtual T Resolve(ItemResolveEventArgs args)
        {
            return null;
        }

        public virtual ResolverPriority Priority
        {
            get
            {
                return ResolverPriority.First;
            }
        }

        /// <summary>
        /// Sets initial values on the newly resolved item
        /// </summary>
        protected virtual void SetInitialItemValues(T item, ItemResolveEventArgs args)
        {
            // If the subclass didn't specify this
            if (string.IsNullOrEmpty(item.Path))
            {
                item.Path = args.Path;
            }

            // If the subclass didn't specify this
            if (args.Parent != null)
            {
                item.Parent = args.Parent;
            }

            item.Id = Kernel.GetMD5(item.Path);
        }

        public async Task<BaseItem> ResolvePath(ItemResolveEventArgs args)
        {
            T item = Resolve(args);

            if (item != null)
            {
                // Set initial values on the newly resolved item
                SetInitialItemValues(item, args);

                // Make sure the item has a name
                EnsureName(item);

                // Make sure DateCreated and DateModified have values
                EnsureDates(item);

                await Kernel.Instance.ExecuteMetadataProviders(item, args);
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

        /// <summary>
        /// Ensures DateCreated and DateModified have values
        /// </summary>
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
    }

    /// <summary>
    /// Weed this to keep a list of resolvers, since Resolvers are built with generics
    /// </summary>
    public interface IBaseItemResolver
    {
        Task<BaseItem> ResolvePath(ItemResolveEventArgs args);
        ResolverPriority Priority { get; }
    }

    public enum ResolverPriority
    {
        First,
        Second,
        Third,
        Last
    }
}
