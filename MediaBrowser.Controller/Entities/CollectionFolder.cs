using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System;
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

        public List<string> PhysicalLocationsList { get; set; }

        protected override IEnumerable<FileSystemInfo> GetFileSystemChildren(IDirectoryService directoryService)
        {
            return CreateResolveArgs(directoryService).FileSystemChildren;
        }

        private ItemResolveArgs CreateResolveArgs(IDirectoryService directoryService)
        {
            var path = ContainingFolderPath;

            var args = new ItemResolveArgs(ConfigurationManager.ApplicationPaths, LibraryManager, directoryService)
            {
                FileInfo = new DirectoryInfo(path),
                Path = path,
                Parent = Parent
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
                    var paths = LibraryManager.NormalizeRootPathList(fileSystemDictionary.Keys);

                    fileSystemDictionary = paths.Select(i => (FileSystemInfo)new DirectoryInfo(i)).ToDictionary(i => i.FullName);
                }

                args.FileSystemDictionary = fileSystemDictionary;
            }

            PhysicalLocationsList = args.PhysicalLocations.ToList();

            return args;
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
        /// <param name="refreshChildMetadata">if set to <c>true</c> [refresh child metadata].</param>
        /// <param name="refreshOptions">The refresh options.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns>Task.</returns>
        protected override Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            CreateResolveArgs(directoryService);
            ResetDynamicChildren();

            return NullTaskResult;
        }

        private List<LinkedChild> _linkedChildren;

        /// <summary>
        /// Our children are actually just references to the ones in the physical root...
        /// </summary>
        /// <value>The linked children.</value>
        public override List<LinkedChild> LinkedChildren
        {
            get { return _linkedChildren ?? (_linkedChildren = GetLinkedChildrenInternal()); }
            set
            {
                base.LinkedChildren = value;
            }
        }
        private List<LinkedChild> GetLinkedChildrenInternal()
        {
            Dictionary<string, string> locationsDicionary;

            try
            {
                locationsDicionary = PhysicalLocations.Distinct().ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
                return new List<LinkedChild>();
            }

            return LibraryManager.RootFolder.Children
                .OfType<Folder>()
                .Where(i => i.Path != null && locationsDicionary.ContainsKey(i.Path))
                .SelectMany(c => c.LinkedChildren)
                .ToList();
        }

        private IEnumerable<BaseItem> _actualChildren;

        /// <summary>
        /// Our children are actually just references to the ones in the physical root...
        /// </summary>
        /// <value>The actual children.</value>
        protected override IEnumerable<BaseItem> ActualChildren
        {
            get { return _actualChildren ?? (_actualChildren = GetActualChildren()); }
        }

        private IEnumerable<BaseItem> GetActualChildren()
        {
            Dictionary<string, string> locationsDicionary;

            try
            {
                locationsDicionary = PhysicalLocations.Distinct().ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
                return new BaseItem[] { };
            }

            return
                LibraryManager.RootFolder.Children
                .OfType<Folder>()
                .Where(i => i.Path != null && locationsDicionary.ContainsKey(i.Path))
                .SelectMany(c => c.Children)
                .ToList();
        }

        public void ResetDynamicChildren()
        {
            _actualChildren = null;
            _linkedChildren = null;
        }
    }
}
