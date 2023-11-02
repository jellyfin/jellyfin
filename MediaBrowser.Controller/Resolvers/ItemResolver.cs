using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Class ItemResolver.
    /// </summary>
    /// <typeparam name="T">The type of BaseItem.</typeparam>
    public abstract class ItemResolver<T> : IItemResolver
        where T : BaseItem, new()
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public virtual ResolverPriority Priority => ResolverPriority.First;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>`0.</returns>
        protected internal virtual T? Resolve(ItemResolveArgs args)
        {
            return null;
        }

        /// <summary>
        /// Sets initial values on the newly resolved item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected virtual void SetInitialItemValues(T item, ItemResolveArgs args)
        {
        }

        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BaseItem.</returns>
        public BaseItem? ResolvePath(ItemResolveArgs args)
        {
            var item = Resolve(args);

            if (item is not null)
            {
                SetInitialItemValues(item, args);
            }

            return item;
        }
    }
}
