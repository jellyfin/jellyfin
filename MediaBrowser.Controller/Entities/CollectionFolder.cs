using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Specialized Folder class that points to a subset of the physical folders in the system.
    /// It is created from the user-specific folders within the system root
    /// </summary>
    public class CollectionFolder : Folder, ICollectionFolder
    {
        public CollectionFolder()
        {
            PhysicalLocationsList = new List<string>();
        }

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

        [IgnoreDataMember]
        protected override bool SupportsShortcutChildren
        {
            get
            {
                return true;
            }
        }

        public override bool CanDelete()
        {
            return false;
        }

        public string CollectionType { get; set; }

        /// <summary>
        /// Allow different display preferences for each collection folder
        /// </summary>
        /// <value>The display prefs id.</value>
        [IgnoreDataMember]
        public override Guid DisplayPreferencesId
        {
            get
            {
                return Id;
            }
        }

        [IgnoreDataMember]
        public override IEnumerable<string> PhysicalLocations
        {
            get
            {
                return PhysicalLocationsList;
            }
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        public List<string> PhysicalLocationsList { get; set; }

        protected override IEnumerable<FileSystemMetadata> GetFileSystemChildren(IDirectoryService directoryService)
        {
            return CreateResolveArgs(directoryService).FileSystemChildren;
        }

        internal override bool IsValidFromResolver(BaseItem newItem)
        {
            var newCollectionFolder = newItem as CollectionFolder;

            if (newCollectionFolder != null)
            {
                if (!string.Equals(CollectionType, newCollectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }


            return base.IsValidFromResolver(newItem);
        }

        private ItemResolveArgs CreateResolveArgs(IDirectoryService directoryService)
        {
            var path = ContainingFolderPath;

            var args = new ItemResolveArgs(ConfigurationManager.ApplicationPaths, directoryService)
            {
                FileInfo = FileSystem.GetDirectoryInfo(path),
                Path = path,
                Parent = Parent,
                CollectionType = CollectionType
            };

            // Gather child folder and files
            if (args.IsDirectory)
            {
                var isPhysicalRoot = args.IsPhysicalRoot;

                // When resolving the root, we need it's grandchildren (children of user views)
                var flattenFolderDepth = isPhysicalRoot ? 2 : 0;

                var fileSystemDictionary = FileData.GetFilteredFileSystemEntries(directoryService, args.Path, FileSystem, Logger, args, flattenFolderDepth: flattenFolderDepth, resolveShortcuts: isPhysicalRoot || args.IsVf);

                // Need to remove subpaths that may have been resolved from shortcuts
                // Example: if \\server\movies exists, then strip out \\server\movies\action
                if (isPhysicalRoot)
                {
                    var paths = LibraryManager.NormalizeRootPathList(fileSystemDictionary.Values);

                    fileSystemDictionary = paths.ToDictionary(i => i.FullName);
                }

                args.FileSystemDictionary = fileSystemDictionary;
            }

            PhysicalLocationsList = args.PhysicalLocations.ToList();

            return args;
        }

        /// <summary>
        /// Compare our current children (presumably just read from the repo) with the current state of the file system and adjust for any changes
        /// ***Currently does not contain logic to maintain items that are unavailable in the file system***
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <param name="refreshChildMetadata">if set to <c>true</c> [refresh child metadata].</param>
        /// <param name="refreshOptions">The refresh options.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns>Task.</returns>
        protected override Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            var list = PhysicalLocationsList.ToList();

            CreateResolveArgs(directoryService);

            if (!list.SequenceEqual(PhysicalLocationsList))
            {
                return UpdateToRepository(ItemUpdateType.MetadataImport, cancellationToken);
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Our children are actually just references to the ones in the physical root...
        /// </summary>
        /// <value>The linked children.</value>
        public override List<LinkedChild> LinkedChildren
        {
            get { return GetLinkedChildrenInternal(); }
            set
            {
                base.LinkedChildren = value;
            }
        }
        private List<LinkedChild> GetLinkedChildrenInternal()
        {
            return GetPhysicalParents()
                .SelectMany(c => c.LinkedChildren)
                .ToList();
        }

        /// <summary>
        /// Our children are actually just references to the ones in the physical root...
        /// </summary>
        /// <value>The actual children.</value>
        protected override IEnumerable<BaseItem> ActualChildren
        {
            get { return GetActualChildren(); }
        }

        private IEnumerable<BaseItem> GetActualChildren()
        {
            return GetPhysicalParents().SelectMany(c => c.Children);
        }

        public IEnumerable<Folder> GetPhysicalParents()
        {
            return LibraryManager.RootFolder.Children
                .OfType<Folder>()
                .Where(i => i.Path != null && PhysicalLocations.Contains(i.Path, StringComparer.OrdinalIgnoreCase));
        }

        [IgnoreDataMember]
        public override bool SupportsPeople
        {
            get
            {
                return false;
            }
        }
    }
}
