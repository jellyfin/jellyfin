using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Class FolderResolver
    /// </summary>
    public class FolderResolver : FolderResolver<Folder>
    {
        private readonly IFileSystem _fileSystem;

        public FolderResolver(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

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
                    return new CollectionFolder
                    {
                        CollectionType = GetCollectionType(args)
                    };
                }

                return new Folder();
            }

            return null;
        }

        private string GetCollectionType(ItemResolveArgs args)
        {
            return args.FileSystemChildren
                .Where(i =>
                {

                    try
                    {
                        return (i.Attributes & FileAttributes.Directory) != FileAttributes.Directory &&
                               string.Equals(".collection", i.Extension, StringComparison.OrdinalIgnoreCase);
                    }
                    catch (IOException)
                    {
                        return false;
                    }

                })
                .Select(i => _fileSystem.GetFileNameWithoutExtension(i))
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Class FolderResolver
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
            item.IsPhysicalRoot = args.IsPhysicalRoot;
        }
    }
}
