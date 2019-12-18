using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;

namespace Emby.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Class FolderResolver.
    /// </summary>
    public class FolderResolver : FolderResolver<Folder>
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Last;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Folder.</returns>
        protected override Folder Resolve(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                return new Folder();
            }

            return null;
        }
    }

    /// <summary>
    /// Class FolderResolver.
    /// </summary>
    /// <typeparam name="TItemType">The type of the T item type.</typeparam>
    public abstract class FolderResolver<TItemType> : ItemResolver<TItemType>
        where TItemType : Folder, new()
    {
        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(TItemType item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            item.IsRoot = args.Parent == null;
        }
    }
}
