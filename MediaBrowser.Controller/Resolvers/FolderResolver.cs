using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Class FolderResolver
    /// </summary>
    public class FolderResolver : BaseFolderResolver<Folder>
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Last; }
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Folder.</returns>
        protected override Folder Resolve(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                if (args.IsPhysicalRoot)
                {
                    return new AggregateFolder();
                }
                if (args.IsRoot)
                {
                    return new UserRootFolder();  //if we got here and still a root - must be user root
                }
                if (args.IsVf)
                {
                    return new CollectionFolder();
                }

                return new Folder();
            }

            return null;
        }
    }

    /// <summary>
    /// Class BaseFolderResolver
    /// </summary>
    /// <typeparam name="TItemType">The type of the T item type.</typeparam>
    public abstract class BaseFolderResolver<TItemType> : BaseItemResolver<TItemType>
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
            item.IsPhysicalRoot = args.IsPhysicalRoot;
        }
    }
}
