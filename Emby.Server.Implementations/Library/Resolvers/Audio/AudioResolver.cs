#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emby.Naming.Audio;
using Emby.Naming.AudioBook;
using Emby.Naming.Common;
using Emby.Naming.Video;
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
        private readonly NamingOptions _namingOptions;

        public AudioResolver(NamingOptions namingOptions)
        {
            _namingOptions = namingOptions;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Fifth;

        public MultiItemResolverResult ResolveMultiple(
            Folder parent,
            List<FileSystemMetadata> files,
            string collectionType,
            IDirectoryService directoryService)
        {
            var result = ResolveMultipleInternal(parent, files, collectionType);

            if (result is not null)
            {
                foreach (var item in result.Items)
                {
                    SetInitialItemValues((MediaBrowser.Controller.Entities.Audio.Audio)item, null);
                }
            }

            return result;
        }

        private MultiItemResolverResult ResolveMultipleInternal(
            Folder parent,
            List<FileSystemMetadata> files,
            string collectionType)
        {
            if (string.Equals(collectionType, CollectionType.Books, StringComparison.OrdinalIgnoreCase))
            {
                return ResolveMultipleAudio(parent, files, true);
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

                return FindAudioBook(args, false);
            }

            if (AudioFileParser.IsAudioFile(args.Path, _namingOptions))
            {
                var extension = Path.GetExtension(args.Path.AsSpan());

                if (extension.Equals(".cue", StringComparison.OrdinalIgnoreCase))
                {
                    // if audio file exists of same name, return null
                    return null;
                }

                var isMixedCollectionType = string.IsNullOrEmpty(collectionType);

                // For conflicting extensions, give priority to videos
                if (isMixedCollectionType && VideoResolver.IsVideoFile(args.Path, _namingOptions))
                {
                    return null;
                }

                MediaBrowser.Controller.Entities.Audio.Audio item = null;

                var isMusicCollectionType = string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase);

                // Use regular audio type for mixed libraries, owned items and music
                if (isMixedCollectionType ||
                    args.Parent is null ||
                    isMusicCollectionType)
                {
                    item = new MediaBrowser.Controller.Entities.Audio.Audio();
                }
                else if (isBooksCollectionType)
                {
                    item = new AudioBook();
                }

                if (item is not null)
                {
                    item.IsShortcut = extension.Equals(".strm", StringComparison.OrdinalIgnoreCase);

                    item.IsInMixedFolder = true;
                }

                return item;
            }

            return null;
        }

        private AudioBook FindAudioBook(ItemResolveArgs args, bool parseName)
        {
            // TODO: Allow GetMultiDiscMovie in here
            var result = ResolveMultipleAudio(args.Parent, args.GetActualFileSystemChildren(), parseName);

            if (result is null || result.Items.Count != 1 || result.Items[0] is not AudioBook item)
            {
                return null;
            }

            // If we were supporting this we'd be checking filesFromOtherItems
            item.IsInMixedFolder = false;
            item.Name = Path.GetFileName(item.ContainingFolderPath);
            return item;
        }

        private MultiItemResolverResult ResolveMultipleAudio(Folder parent, IEnumerable<FileSystemMetadata> fileSystemEntries, bool parseName)
        {
            var files = new List<FileSystemMetadata>();
            var leftOver = new List<FileSystemMetadata>();

            // Loop through each child file/folder and see if we find a video
            foreach (var child in fileSystemEntries)
            {
                if (child.IsDirectory)
                {
                    leftOver.Add(child);
                }
                else
                {
                    files.Add(child);
                }
            }

            var resolver = new AudioBookListResolver(_namingOptions);
            var resolverResult = resolver.Resolve(files).ToList();

            var result = new MultiItemResolverResult
            {
                ExtraFiles = leftOver,
                Items = new List<BaseItem>()
            };

            var isInMixedFolder = resolverResult.Count > 1 || (parent is not null && parent.IsTopParent);

            foreach (var resolvedItem in resolverResult)
            {
                if (resolvedItem.Files.Count > 1)
                {
                    // For now, until we sort out naming for multi-part books
                    continue;
                }

                // Until multi-part books are handled letting files stack hides them from browsing in the client
                if (resolvedItem.Files.Count == 0 || resolvedItem.Extras.Count > 0 || resolvedItem.AlternateVersions.Count > 0)
                {
                    continue;
                }

                var firstMedia = resolvedItem.Files[0];

                var libraryItem = new AudioBook
                {
                    Path = firstMedia.Path,
                    IsInMixedFolder = isInMixedFolder,
                    ProductionYear = resolvedItem.Year,
                    Name = parseName ?
                        resolvedItem.Name :
                        Path.GetFileNameWithoutExtension(firstMedia.Path),
                    // AdditionalParts = resolvedItem.Files.Skip(1).Select(i => i.Path).ToArray(),
                    // LocalAlternateVersions = resolvedItem.AlternateVersions.Select(i => i.Path).ToArray()
                };

                result.Items.Add(libraryItem);
            }

            result.ExtraFiles.AddRange(files.Where(i => !ContainsFile(resolverResult, i)));

            return result;
        }

        private static bool ContainsFile(IEnumerable<AudioBookInfo> result, FileSystemMetadata file)
        {
            return result.Any(i => ContainsFile(i, file));
        }

        private static bool ContainsFile(AudioBookInfo result, FileSystemMetadata file)
        {
            return result.Files.Any(i => ContainsFile(i, file)) ||
                result.AlternateVersions.Any(i => ContainsFile(i, file)) ||
                result.Extras.Any(i => ContainsFile(i, file));
        }

        private static bool ContainsFile(AudioBookFileInfo result, FileSystemMetadata file)
        {
            return string.Equals(result.Path, file.FullName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
