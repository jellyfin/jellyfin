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
using CommonIO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using MoreLinq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Specialized Folder class that points to a subset of the physical folders in the system.
    /// It is created from the user-specific folders within the system root
    /// </summary>
    public class CollectionFolder : Folder, ICollectionFolder
    {
        public static IXmlSerializer XmlSerializer { get; set; }

        public CollectionFolder()
        {
            PhysicalLocationsList = new List<string>();
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

        private static readonly Dictionary<string, LibraryOptions> LibraryOptions = new Dictionary<string, LibraryOptions>();
        public LibraryOptions GetLibraryOptions()
        {
            lock (LibraryOptions)
            {
                LibraryOptions options;
                if (!LibraryOptions.TryGetValue(Path, out options))
                {
                    options = LoadLibraryOptions();
                    LibraryOptions[Path] = options;
                }

                return options;
            }
        }

        private LibraryOptions LoadLibraryOptions()
        {
            try
            {
                var result = XmlSerializer.DeserializeFromFile(typeof(LibraryOptions), GetLibraryOptionsPath(Path)) as LibraryOptions;

                if (result == null)
                {
                    return new LibraryOptions();
                }

                return result;
            }
            catch (FileNotFoundException)
            {
                return new LibraryOptions();
            }
            catch (DirectoryNotFoundException)
            {
                return new LibraryOptions();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error loading library options", ex);

                return new LibraryOptions();
            }
        }

        private static string GetLibraryOptionsPath(string path)
        {
            return System.IO.Path.Combine(path, "options.xml");
        }

        public void UpdateLibraryOptions(LibraryOptions options)
        {
            SaveLibraryOptions(Path, options);
        }

        public static void SaveLibraryOptions(string path, LibraryOptions options)
        {
            lock (LibraryOptions)
            {
                LibraryOptions[path] = options;

                options.SchemaVersion = 1;
                XmlSerializer.SerializeToFile(options, GetLibraryOptionsPath(path));
            }
        }

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
            return CreateResolveArgs(directoryService, true).FileSystemChildren;
        }

        private bool _requiresRefresh;
        public override bool RequiresRefresh()
        {
            var changed = base.RequiresRefresh() || _requiresRefresh;

            if (!changed)
            {
                var locations = PhysicalLocations.ToList();

                var newLocations = CreateResolveArgs(new DirectoryService(Logger, FileSystem), false).PhysicalLocations.ToList();

                if (!locations.SequenceEqual(newLocations))
                {
                    changed = true;
                }
            }

            return changed;
        }

        public override bool BeforeMetadataRefresh()
        {
            var changed = base.BeforeMetadataRefresh() || _requiresRefresh;
            _requiresRefresh = false;
            return changed;
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

        private ItemResolveArgs CreateResolveArgs(IDirectoryService directoryService, bool setPhysicalLocations)
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

            _requiresRefresh = _requiresRefresh || !args.PhysicalLocations.SequenceEqual(PhysicalLocations);
            if (setPhysicalLocations)
            {
                PhysicalLocationsList = args.PhysicalLocations.ToList();
            }

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
        [IgnoreDataMember]
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
            var rootChildren = LibraryManager.RootFolder.Children
                .OfType<Folder>()
                .ToList();

            return PhysicalLocations.Where(i => !string.Equals(i, Path, StringComparison.OrdinalIgnoreCase)).SelectMany(i => GetPhysicalParents(i, rootChildren)).DistinctBy(i => i.Id);
        }

        private IEnumerable<Folder> GetPhysicalParents(string path, List<Folder> rootChildren)
        {
            var result = rootChildren
                .Where(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (result.Count == 0)
            {
                var folder = LibraryManager.FindByPath(path, true) as Folder;

                if (folder != null)
                {
                    result.Add(folder);
                }
            }

            return result;
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
