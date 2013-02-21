using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.IO;
using System.Text.RegularExpressions;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Class BaseItemResolver
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseItemResolver<T> : IBaseItemResolver
        where T : BaseItem, new()
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>`0.</returns>
        protected virtual T Resolve(ItemResolveArgs args)
        {
            return null;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
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
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected virtual void SetInitialItemValues(T item, ItemResolveArgs args)
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

            item.Id = item.Path.GetMBId(item.GetType());
            item.DisplayMediaType = item.GetType().Name;
        }

        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BaseItem.</returns>
        public BaseItem ResolvePath(ItemResolveArgs args)
        {
            T item = Resolve(args);

            if (item != null)
            {
                // Set the args on the item
                item.ResolveArgs = args;

                // Set initial values on the newly resolved item
                SetInitialItemValues(item, args);

                // Make sure the item has a name
                EnsureName(item);

                // Make sure DateCreated and DateModified have values
                EntityResolutionHelper.EnsureDates(item, args);
            }

            return item;
        }

        /// <summary>
        /// Ensures the name.
        /// </summary>
        /// <param name="item">The item.</param>
        private void EnsureName(T item)
        {
            // If the subclass didn't supply a name, add it here
            if (string.IsNullOrEmpty(item.Name) && !string.IsNullOrEmpty(item.Path))
            {
                //we use our resolve args name here to get the name of the containg folder, not actual video file
                item.Name = GetMBName(item.ResolveArgs.FileInfo.cFileName, item.ResolveArgs.FileInfo.IsDirectory);
            }
        }

        /// <summary>
        /// The MB name regex
        /// </summary>
        private static readonly Regex MBNameRegex = new Regex("(\\[.*\\])", RegexOptions.Compiled);

        /// <summary>
        /// Strip out attribute items and return just the name we will use for items
        /// </summary>
        /// <param name="path">Assumed to be a file or directory path</param>
        /// <param name="isDirectory">if set to <c>true</c> [is directory].</param>
        /// <returns>The cleaned name</returns>
        private static string GetMBName(string path, bool isDirectory)
        {
            //first just get the file or directory name
            var fn = isDirectory ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);

            //now - strip out anything inside brackets
            fn = MBNameRegex.Replace(fn, string.Empty);

            return fn;
        }
    }

    /// <summary>
    /// Weed this to keep a list of resolvers, since Resolvers are built with generics
    /// </summary>
    public interface IBaseItemResolver
    {
        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BaseItem.</returns>
        BaseItem ResolvePath(ItemResolveArgs args);
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        ResolverPriority Priority { get; }
    }

    /// <summary>
    /// Enum ResolverPriority
    /// </summary>
    public enum ResolverPriority
    {
        /// <summary>
        /// The first
        /// </summary>
        First = 1,
        /// <summary>
        /// The second
        /// </summary>
        Second = 2,
        /// <summary>
        /// The third
        /// </summary>
        Third = 3,
        /// <summary>
        /// The last
        /// </summary>
        Last = 4
    }
}
