#pragma warning disable CS1591

using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Interface IItemResolver.
    /// </summary>
    public interface IItemResolver
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        ResolverPriority Priority { get; }

        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BaseItem.</returns>
        BaseItem? ResolvePath(ItemResolveArgs args);
    }

    public interface IMultiItemResolver
    {
        MultiItemResolverResult ResolveMultiple(
            Folder parent,
            List<FileSystemMetadata> files,
            CollectionType? collectionType,
            IDirectoryService directoryService);
    }

    public class MultiItemResolverResult
    {
        public MultiItemResolverResult()
        {
            Items = new List<BaseItem>();
            ExtraFiles = new List<FileSystemMetadata>();
        }

        public List<BaseItem> Items { get; set; }

        public List<FileSystemMetadata> ExtraFiles { get; set; }
    }
}
