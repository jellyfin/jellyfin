using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// The result of an <see cref="IMultiItemResolver"/>.
    /// </summary>
    public class MultiItemResolverResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiItemResolverResult"/> class.
        /// </summary>
        public MultiItemResolverResult()
        {
            Items = new List<BaseItem>();
            ExtraFiles = new List<FileSystemMetadata>();
        }

        /// <summary>
        /// Gets or sets the resolved items.
        /// </summary>
        public List<BaseItem> Items { get; set; }

        /// <summary>
        /// Gets or sets the files that were not resolved to an item.
        /// </summary>
        public List<FileSystemMetadata> ExtraFiles { get; set; }
    }
}
