#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emby.Naming.AudioBook;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class AudioResolver.
    /// </summary>
    public class AudioResolver : ItemResolver<MediaBrowser.Controller.Entities.Audio.Audio>, IMultiItemResolver
    {
        private readonly ILibraryManager LibraryManager;

        public AudioResolver(ILibraryManager libraryManager)
        {
            LibraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Fourth;

        public MultiItemResolverResult ResolveMultiple(Folder parent,
            List<FileSystemMetadata> files,
            string collectionType,
            IDirectoryService directoryService)
        {
            var result = ResolveMultipleInternal(parent, files, collectionType, directoryService);

            if (result != null)
            {
                foreach (var item in result.Items)
                {
                    SetInitialItemValues((MediaBrowser.Controller.Entities.Audio.Audio)item, null);
                }
            }

            return result;
        }

        private MultiItemResolverResult ResolveMultipleInternal(Folder parent,
            List<FileSystemMetadata> files,
            string collectionType,
            IDirectoryService directoryService)
        {
            if (string.Equals(collectionType, CollectionType.Books, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveMultipleAudio<AudioBook>(parent, files, directoryService, false, collectionType, true);
            }

            return null;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Entities.Audio.Audio.</returns>
        protected override MediaBrowser.Controller.Entities.Audio.Audio Resolve(ItemResolveArgs args)
        {
            // Return audio if the path is a file and has a matching extension

            var collectionType = args.GetCollectionType();

            var isBooksCollectionType = string.Equals(collectionType, CollectionType.Books, StringComparison.OrdinalIgnoreCase);

            if (args.IsDirectory)
            {
                if (!isBooksCollectionType)
                {
                    return null;
                }

                var files = args.FileSystemChildren
                    .Where(i => !LibraryManager.IgnoreFile(i, args.Parent))
                    .ToList();

                return FindAudio<AudioBook>(args, args.Path, args.Parent, files, args.DirectoryService, collectionType, false);
            }

            if (LibraryManager.IsAudioFile(args.Path))
            {
                var extension = Path.GetExtension(args.Path);

                if (string.Equals(extension, ".cue", StringComparison.OrdinalIgnoreCase))
                {
                    // if audio file exists of same name, return null
                    return null;
                }

                var isMixedCollectionType = string.IsNullOrEmpty(collectionType);

                // For conflicting extensions, give priority to videos
                if (isMixedCollectionType && LibraryManager.IsVideoFile(args.Path))
                {
                    return null;
                }

                MediaBrowser.Controller.Entities.Audio.Audio item = null;

                var isMusicCollectionType = string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase);

                // Use regular audio type for mixed libraries, owned items and music
                if (isMixedCollectionType ||
                    args.Parent == null ||
                    isMusicCollectionType)
                {
                    item = new MediaBrowser.Controller.Entities.Audio.Audio();
                }
                else if (isBooksCollectionType)
                {
                    item = new AudioBook();
                }

                if (item != null)
                {
                    item.IsShortcut = string.Equals(extension, ".strm", StringComparison.OrdinalIgnoreCase);

                    item.IsInMixedFolder = true;
                }

                return item;
            }

            return null;
        }

        private T FindAudio<T>(ItemResolveArgs args, string path, Folder parent, List<FileSystemMetadata> fileSystemEntries, IDirectoryService directoryService, string collectionType, bool parseName)
            where T : MediaBrowser.Controller.Entities.Audio.Audio, new()
        {
            // TODO: Allow GetMultiDiscMovie in here
            const bool supportsMultiVersion = false;

            var result = ResolveMultipleAudio<T>(parent, fileSystemEntries, directoryService, supportsMultiVersion, collectionType, parseName) ??
                new MultiItemResolverResult();

            if (result.Items.Count == 1)
            {
                // If we were supporting this we'd be checking filesFromOtherItems
                var item = (T)result.Items[0];
                item.IsInMixedFolder = false;
                item.Name = Path.GetFileName(item.ContainingFolderPath);
                return item;
            }

            return null;
        }

        private MultiItemResolverResult ResolveMultipleAudio<T>(Folder parent, IEnumerable<FileSystemMetadata> fileSystemEntries, IDirectoryService directoryService, bool suppportMultiEditions, string collectionType, bool parseName)
            where T : MediaBrowser.Controller.Entities.Audio.Audio, new()
        {
            var files = new List<FileSystemMetadata>();
            var items = new List<BaseItem>();
            var leftOver = new List<FileSystemMetadata>();

            // Loop through each child file/folder and see if we find a video
            foreach (var child in fileSystemEntries)
            {
                if (child.IsDirectory)
                {
                    leftOver.Add(child);
                }
                else if (!IsIgnored(child.Name))
                {
                    files.Add(child);
                }
            }

            var namingOptions = ((LibraryManager)LibraryManager).GetNamingOptions();

            var resolver = new AudioBookListResolver(namingOptions);
            var resolverResult = resolver.Resolve(files).ToList();

            var result = new MultiItemResolverResult
            {
                ExtraFiles = leftOver,
                Items = items
            };

            var isInMixedFolder = resolverResult.Count > 1 || (parent != null && parent.IsTopParent);

            foreach (var resolvedItem in resolverResult)
            {
                if (resolvedItem.Files.Count > 1)
                {
                    // For now, until we sort out naming for multi-part books
                    continue;
                }

                var firstMedia = resolvedItem.Files.First();

                var libraryItem = new T
                {
                    Path = firstMedia.Path,
                    IsInMixedFolder = isInMixedFolder,
                    ProductionYear = resolvedItem.Year,
                    Name = parseName ?
                        resolvedItem.Name :
                        Path.GetFileNameWithoutExtension(firstMedia.Path),
                    //AdditionalParts = resolvedItem.Files.Skip(1).Select(i => i.Path).ToArray(),
                    //LocalAlternateVersions = resolvedItem.AlternateVersions.Select(i => i.Path).ToArray()
                };

                result.Items.Add(libraryItem);
            }

            result.ExtraFiles.AddRange(files.Where(i => !ContainsFile(resolverResult, i)));

            return result;
        }

        private bool ContainsFile(List<AudioBookInfo> result, FileSystemMetadata file)
        {
            return result.Any(i => ContainsFile(i, file));
        }

        private bool ContainsFile(AudioBookInfo result, FileSystemMetadata file)
        {
            return result.Files.Any(i => ContainsFile(i, file)) ||
                result.AlternateVersions.Any(i => ContainsFile(i, file)) ||
                result.Extras.Any(i => ContainsFile(i, file));
        }

        private static bool ContainsFile(AudioBookFileInfo result, FileSystemMetadata file)
        {
            return string.Equals(result.Path, file.FullName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsIgnored(string filename)
        {
            return false;
        }
    }
}
