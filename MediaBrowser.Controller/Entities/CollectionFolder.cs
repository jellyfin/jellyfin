using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Specialized Folder class that points to a subset of the physical folders in the system.
    /// It is created from the user-specific folders within the system root
    /// </summary>
    public class CollectionFolder : Folder, ICollectionFolder, IByReferenceItem
    {
        /// <summary>
        /// Gets a value indicating whether this instance is virtual folder.
        /// </summary>
        /// <value><c>true</c> if this instance is virtual folder; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IsVirtualFolder
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Allow different display preferences for each collection folder
        /// </summary>
        /// <value>The display prefs id.</value>
        public override Guid DisplayPreferencesId
        {
            get
            {
                return Id;
            }
        }

        // Cache this since it will be used a lot
        /// <summary>
        /// The null task result
        /// </summary>
        private static readonly Task NullTaskResult = Task.FromResult<object>(null);

        /// <summary>
        /// Compare our current children (presumably just read from the repo) with the current state of the file system and adjust for any changes
        /// ***Currently does not contain logic to maintain items that are unavailable in the file system***
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>Task.</returns>
        protected override Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool? recursive = null)
        {
            //we don't directly validate our children
            //but we do need to clear out the index cache...
            IndexCache = new ConcurrentDictionary<string, List<BaseItem>>(StringComparer.OrdinalIgnoreCase);

            return NullTaskResult;
        }

        /// <summary>
        /// Our children are actually just references to the ones in the physical root...
        /// </summary>
        /// <value>The actual children.</value>
        protected override ConcurrentBag<BaseItem> ActualChildren
        {
            get
            {
                IEnumerable<Guid> folderIds;

                try
                {
                    // Accessing ResolveArgs could involve file system access
                    folderIds = ResolveArgs.PhysicalLocations.Select(f => (f.GetMBId(typeof(Folder))));
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error creating FolderIds for {0}", ex, Path);

                    folderIds = new Guid[] {};
                }

                var ourChildren =
                    LibraryManager.RootFolder.Children.OfType<Folder>()
                          .Where(i => folderIds.Contains(i.Id))
                          .SelectMany(c => c.Children);

                return new ConcurrentBag<BaseItem>(ourChildren);
            }
        }
    }
}
