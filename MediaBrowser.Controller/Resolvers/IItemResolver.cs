using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using CommonIO;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Interface IItemResolver
    /// </summary>
    public interface IItemResolver
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

    public interface IMultiItemResolver
    {
        MultiItemResolverResult ResolveMultiple(Folder parent,
            List<FileSystemMetadata> files, 
            string collectionType,
            IDirectoryService directoryService);
    }

    public class MultiItemResolverResult
    {
        public List<BaseItem> Items { get; set; }
        public List<FileSystemMetadata> ExtraFiles { get; set; }

        public MultiItemResolverResult()
        {
            Items = new List<BaseItem>();
            ExtraFiles = new List<FileSystemMetadata>();
        }
    }
}
