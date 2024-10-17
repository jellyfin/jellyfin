#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;

using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Specialized Folder class that points to a subset of the physical folders in the system.
    /// It is created from the user-specific folders within the system root.
    /// </summary>
    public class CollectionFolder : Folder, ICollectionFolder
    {
        private static readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private static readonly ConcurrentDictionary<string, LibraryOptions> _libraryOptions = new ConcurrentDictionary<string, LibraryOptions>();
        private bool _requiresRefresh;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionFolder"/> class.
        /// </summary>
        public CollectionFolder()
        {
            PhysicalLocationsList = Array.Empty<string>();
            PhysicalFolderIds = Array.Empty<Guid>();
        }

        /// <summary>
        /// Gets the display preferences id.
        /// </summary>
        /// <remarks>
        /// Allow different display preferences for each collection folder.
        /// </remarks>
        /// <value>The display prefs id.</value>
        [JsonIgnore]
        public override Guid DisplayPreferencesId => Id;

        [JsonIgnore]
        public override string[] PhysicalLocations => PhysicalLocationsList;

        public string[] PhysicalLocationsList { get; set; }

        public Guid[] PhysicalFolderIds { get; set; }

        public static IXmlSerializer XmlSerializer { get; set; }

        public static IServerApplicationHost ApplicationHost { get; set; }

        [JsonIgnore]
        public override bool SupportsPlayedStatus => false;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        public CollectionType? CollectionType { get; set; }

        /// <summary>
        /// Gets the item's children.
        /// </summary>
        /// <remarks>
        /// Our children are actually just references to the ones in the physical root...
        /// </remarks>
        /// <value>The actual children.</value>
        [JsonIgnore]
        public override IEnumerable<BaseItem> Children => GetActualChildren();

        [JsonIgnore]
        public override bool SupportsPeople => false;

        public override bool CanDelete()
        {
            return false;
        }

        public LibraryOptions GetLibraryOptions()
        {
            return GetLibraryOptions(Path);
        }

        public override bool IsVisible(User user, bool skipAllowedTagsCheck = false)
        {
            if (GetLibraryOptions().Enabled)
            {
                return base.IsVisible(user, skipAllowedTagsCheck);
            }

            return false;
        }

        private static LibraryOptions LoadLibraryOptions(string path)
        {
            try
            {
                if (XmlSerializer.DeserializeFromFile(typeof(LibraryOptions), GetLibraryOptionsPath(path)) is not LibraryOptions result)
                {
                    return new LibraryOptions();
                }

                foreach (var mediaPath in result.PathInfos)
                {
                    if (!string.IsNullOrEmpty(mediaPath.Path))
                    {
                        mediaPath.Path = ApplicationHost.ExpandVirtualPath(mediaPath.Path);
                    }
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
                Logger.LogError(ex, "Error loading library options");

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
            => _libraryOptions.GetOrAdd(path, LoadLibraryOptions);

        public static void SaveLibraryOptions(string path, LibraryOptions options)
        {
            _libraryOptions[path] = options;

            var clone = JsonSerializer.Deserialize<LibraryOptions>(JsonSerializer.SerializeToUtf8Bytes(options, _jsonOptions), _jsonOptions);
            foreach (var mediaPath in clone.PathInfos)
            {
                if (!string.IsNullOrEmpty(mediaPath.Path))
                {
                    mediaPath.Path = ApplicationHost.ReverseVirtualPath(mediaPath.Path);
                }
            }

            XmlSerializer.SerializeToFile(clone, GetLibraryOptionsPath(path));
        }

        public static void OnCollectionFolderChange()
            => _libraryOptions.Clear();

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        protected override FileSystemMetadata[] GetFileSystemChildren(IDirectoryService directoryService)
        {
            return CreateResolveArgs(directoryService, true).FileSystemChildren;
        }

        public override bool RequiresRefresh()
        {
            var changed = base.RequiresRefresh() || _requiresRefresh;

            if (!changed)
            {
                var locations = PhysicalLocations;

                var newLocations = CreateResolveArgs(new DirectoryService(FileSystem), false).PhysicalLocations;

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

        public override bool BeforeMetadataRefresh(bool replaceAllMetadata)
        {
            var changed = base.BeforeMetadataRefresh(replaceAllMetadata) || _requiresRefresh;
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

            return totalProgresses / foldersWithProgress;
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

            LinkedChildren = linkedChildren.ToArray();

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

            var args = new ItemResolveArgs(ConfigurationManager.ApplicationPaths, LibraryManager)
            {
                FileInfo = FileSystem.GetDirectoryInfo(path),
                Parent = GetParent() as Folder,
                CollectionType = CollectionType
            };

            // Gather child folder and files
            if (args.IsDirectory)
            {
                var flattenFolderDepth = 0;

                var files = FileData.GetFilteredFileSystemEntries(directoryService, args.Path, FileSystem, ApplicationHost, Logger, args, flattenFolderDepth: flattenFolderDepth, resolveShortcuts: true);

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
        /// ***Currently does not contain logic to maintain items that are unavailable in the file system***.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <param name="refreshChildMetadata">if set to <c>true</c> [refresh child metadata].</param>
        /// <param name="allowRemoveRoot">remove item even this folder is root.</param>
        /// <param name="refreshOptions">The refresh options.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        protected override Task ValidateChildrenInternal(IProgress<double> progress, bool recursive, bool refreshChildMetadata, bool allowRemoveRoot, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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

            return PhysicalLocations
                    .Where(i => !FileSystem.AreEqual(i, Path))
                    .SelectMany(i => GetPhysicalParents(i, rootChildren))
                    .DistinctBy(x => x.Id);
        }

        private IEnumerable<Folder> GetPhysicalParents(string path, List<Folder> rootChildren)
        {
            var result = rootChildren
                .Where(i => FileSystem.AreEqual(i.Path, path))
                .ToList();

            if (result.Count == 0)
            {
                if (LibraryManager.FindByPath(path, true) is Folder folder)
                {
                    result.Add(folder);
                }
            }

            return result;
        }
    }
}
