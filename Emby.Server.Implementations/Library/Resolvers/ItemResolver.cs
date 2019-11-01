using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;

namespace Emby.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Class ItemResolver.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ItemResolver<T> : IItemResolver
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
        public virtual ResolverPriority Priority => ResolverPriority.First;

        /// <summary>
        /// Sets initial values on the newly resolved item
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
        BaseItem IItemResolver.ResolvePath(ItemResolveArgs args)
        {
            var item = Resolve(args);

            if (item != null)
            {
                SetInitialItemValues(item, args);
            }

            return item;
        }
    }
}
