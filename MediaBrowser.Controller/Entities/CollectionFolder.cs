using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;

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
            PhysicalLocationsList = EmptyStringArray;
            PhysicalFolderIds = EmptyGuidArray;
        }

        //public override double? GetDefaultPrimaryImageAspectRatio()
        //{
        //    double value = 16;
        //    value /= 9;

        //    return value;
        //}

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsInheritedParentImages
        {
            get
            {
                return false;
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
            return GetLibraryOptions(Path);
        }

        private static LibraryOptions LoadLibraryOptions(string path)
        {
            try
            {
                var result = XmlSerializer.DeserializeFromFile(typeof(LibraryOptions), GetLibraryOptionsPath(path)) as LibraryOptions;

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
            catch (IOException)
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

        public static LibraryOptions GetLibraryOptions(string path)
        {
            lock (LibraryOptions)
            {
                LibraryOptions options;
                if (!LibraryOptions.TryGetValue(path, out options))
                {
                    options = LoadLibraryOptions(path);
                    LibraryOptions[path] = options;
                }

                return options;
            }
        }

        public static void SaveLibraryOptions(string path, LibraryOptions options)
        {
            lock (LibraryOptions)
            {
                LibraryOptions[path] = options;

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
        public override string[] PhysicalLocations
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

        public string[] PhysicalLocationsList { get; set; }
        public Guid[] PhysicalFolderIds { get; set; }

        protected override FileSystemMetadata[] GetFileSystemChildren(IDirectoryService directoryService)
        {
            return CreateResolveArgs(directoryService, true).FileSystemChildren;
        }

        private bool _requiresRefresh;
        public override bool RequiresRefresh()
        {
            var changed = base.RequiresRefresh() || _requiresRefresh;

            if (!changed)
            {
                var locations = PhysicalLocations;

                var newLocations = CreateResolveArgs(new DirectoryService(Logger, FileSystem), false).PhysicalLocations;

                if (!locations.SequenceEqual(newLocations))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                var folderIds = PhysicalFolderIds;

                var newFolderIds = GetPhysicalFolders(false).Select(i => i.Id).ToList();

                if (!folderIds.SequenceEqual(newFolderIds))
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

        public override double? GetRefreshProgress()
        {
            var folders = GetPhysicalFolders(true).ToList();
            double totalProgresses = 0;
            var foldersWithProgress = 0;

            foreach (var folder in folders)
            {
                var progress = ProviderManager.GetRefreshProgress(folder.Id);
                if (progress.HasValue)
                {
                    totalProgresses += progress.Value;
                    foldersWithProgress++;
                }
            }

            if (foldersWithProgress == 0)
            {
                return null;
            }

            return (totalProgresses / foldersWithProgress);
        }

        protected override bool RefreshLinkedChildren(IEnumerable<FileSystemMetadata> fileSystemChildren)
        {
            return RefreshLinkedChildrenInternal(true);
        }

        private bool RefreshLinkedChildrenInternal(bool setFolders)
        {
            var physicalFolders = GetPhysicalFolders(false)
                .ToList();

            var linkedChildren = physicalFolders
                .SelectMany(c => c.LinkedChildren)
                .ToList();

            var changed = !linkedChildren.SequenceEqual(LinkedChildren, new LinkedChildComparer(FileSystem));

            LinkedChildren = linkedChildren.ToArray(linkedChildren.Count);

            var folderIds = PhysicalFolderIds;
            var newFolderIds = physicalFolders.Select(i => i.Id).ToArray();

            if (!folderIds.SequenceEqual(newFolderIds))
            {
                changed = true;
                if (setFolders)
                {
                    PhysicalFolderIds = newFolderIds;
                }
            }

            return changed;
        }

        private ItemResolveArgs CreateResolveArgs(IDirectoryService directoryService, bool setPhysicalLocations)
        {
            var path = ContainingFolderPath;

            var args = new ItemResolveArgs(ConfigurationManager.ApplicationPaths, directoryService)
            {
                FileInfo = FileSystem.GetDirectoryInfo(path),
                Path = path,
                Parent = GetParent() as Folder,
                CollectionType = CollectionType
            };

            // Gather child folder and files
            if (args.IsDirectory)
            {
                var isPhysicalRoot = args.IsPhysicalRoot;

                // When resolving the root, we need it's grandchildren (children of user views)
                var flattenFolderDepth = isPhysicalRoot ? 2 : 0;

                var files = FileData.GetFilteredFileSystemEntries(directoryService, args.Path, FileSystem, Logger, args, flattenFolderDepth: flattenFolderDepth, resolveShortcuts: isPhysicalRoot || args.IsVf);

                // Need to remove subpaths that may have been resolved from shortcuts
                // Example: if \\server\movies exists, then strip out \\server\movies\action
                if (isPhysicalRoot)
                {
                    files = LibraryManager.NormalizeRootPathList(files).ToArray();
                }

                args.FileSystemChildren = files;
            }

            _requiresRefresh = _requiresRefresh || !args.PhysicalLocations.SequenceEqual(PhysicalLocations);
            if (setPhysicalLocations)
            {
                PhysicalLocationsList = args.PhysicalLocations;
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
        /// <value>The actual children.</value>
        [IgnoreDataMember]
        public override IEnumerable<BaseItem> Children
        {
            get { return GetActualChildren(); }
        }

        public IEnumerable<BaseItem> GetActualChildren()
        {
            return GetPhysicalFolders(true).SelectMany(c => c.Children);
        }

        public IEnumerable<Folder> GetPhysicalFolders()
        {
            return GetPhysicalFolders(true);
        }

        private IEnumerable<Folder> GetPhysicalFolders(bool enableCache)
        {
            if (enableCache)
            {
                return PhysicalFolderIds.Select(i => LibraryManager.GetItemById(i)).OfType<Folder>();
            }

            var rootChildren = LibraryManager.RootFolder.Children
                .OfType<Folder>()
                .ToList();

            return PhysicalLocations.Where(i => !FileSystem.AreEqual(i, Path)).SelectMany(i => GetPhysicalParents(i, rootChildren)).DistinctBy(i => i.Id);
        }

        private IEnumerable<Folder> GetPhysicalParents(string path, List<Folder> rootChildren)
        {
            var result = rootChildren
                .Where(i => FileSystem.AreEqual(i.Path, path))
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
