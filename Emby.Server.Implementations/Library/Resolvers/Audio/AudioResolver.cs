#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emby.Naming.Audio;
using Emby.Naming.AudioBook;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
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
            CollectionType? collectionType,
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
            CollectionType? collectionType)
        {
            if (collectionType == CollectionType.books)
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

            var isBooksCollectionType = collectionType == CollectionType.books;

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

                var isMixedCollectionType = collectionType is null;

                // For conflicting extensions, give priority to videos
                if (isMixedCollectionType && VideoResolver.IsVideoFile(args.Path, _namingOptions))
                {
                    return null;
                }

                MediaBrowser.Controller.Entities.Audio.Audio item = null;

                var isMusicCollectionType = collectionType == CollectionType.music;

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
                result.Items.AddRange(CreateAudioBookItems(resolvedItem, isInMixedFolder, parseName));
            }

            result.ExtraFiles.AddRange(files.Where(i => !ContainsFile(resolverResult, i)));

            return result;
        }

        private IEnumerable<BaseItem> CreateAudioBookItems(AudioBookInfo resolvedItem, bool isInMixedFolder, bool parseName)
        {
            if (resolvedItem.Files.Count == 0)
            {
                return Array.Empty<BaseItem>();
            }

            // Descriptive names that contain their own numbers (e.g. "04 Chapter 1") collide on the
            // parsed chapter number and get split into alternate versions. A distinct order number on
            // every file means they are really separate parts, so keep them together as one book.
            if (resolvedItem.Extras.Count == 0)
            {
                var allParts = resolvedItem.Files.Concat(resolvedItem.AlternateVersions).ToList();
                if (allParts.Count > 1 && HasDistinctTrackNumbers(allParts))
                {
                    return new[] { BuildStackedAudioBook(allParts, isInMixedFolder, parseName, resolvedItem) };
                }
            }

            if (resolvedItem.Extras.Count > 0 || resolvedItem.AlternateVersions.Count > 0)
            {
                return Array.Empty<BaseItem>();
            }

            if (resolvedItem.Files.Count > 1 && !IsCoherentMultiPartBook(resolvedItem))
            {
                return resolvedItem.Files.Select(file => new AudioBook
                {
                    Path = file.Path,
                    IsInMixedFolder = isInMixedFolder,
                    ProductionYear = resolvedItem.Year,
                    Name = Path.GetFileNameWithoutExtension(file.Path)
                });
            }

            return new[] { BuildStackedAudioBook(resolvedItem.Files, isInMixedFolder, parseName, resolvedItem) };
        }

        private static AudioBook BuildStackedAudioBook(IReadOnlyList<AudioBookFileInfo> files, bool isInMixedFolder, bool parseName, AudioBookInfo resolvedItem)
        {
            var orderedFiles = files
                .OrderBy(f => GetTrackNumber(f.Path) ?? int.MaxValue)
                .ToList();
            var firstMedia = orderedFiles[0];

            return new AudioBook
            {
                Path = firstMedia.Path,
                IsInMixedFolder = isInMixedFolder,
                ProductionYear = resolvedItem.Year,
                Name = parseName ?
                    resolvedItem.Name :
                    Path.GetFileNameWithoutExtension(firstMedia.Path),
                AdditionalParts = orderedFiles.Skip(1).Select(i => i.Path).ToArray()
            };
        }

        private static bool IsCoherentMultiPartBook(AudioBookInfo info)
        {
            var files = info.Files;
            if (files.Count <= 1)
            {
                return true;
            }

            return HasSharedBaseName(files) || HasDistinctTrackNumbers(files);
        }

        private static bool HasSharedBaseName(IReadOnlyList<AudioBookFileInfo> files)
        {
            if (!files.All(f => f.ChapterNumber is not null || f.PartNumber is not null))
            {
                return false;
            }

            var baseName = GetComparableBaseName(files[0].Path);
            if (baseName.Length == 0)
            {
                return false;
            }

            return files.All(f => string.Equals(GetComparableBaseName(f.Path), baseName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasDistinctTrackNumbers(IReadOnlyList<AudioBookFileInfo> files)
        {
            var seen = new HashSet<int>();
            foreach (var file in files)
            {
                var number = GetTrackNumber(file.Path);
                if (number is null || !seen.Add(number.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetComparableBaseName(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path.AsSpan()).Trim();
            var end = name.Length;
            while (end > 0)
            {
                var c = name[end - 1];
                if (char.IsDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.' || c == '#')
                {
                    end--;
                }
                else
                {
                    break;
                }
            }

            return name[..end].ToString();
        }

        private static int? GetTrackNumber(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path.AsSpan()).Trim();
            if (name.Length == 0)
            {
                return null;
            }

            var leading = 0;
            while (leading < name.Length && char.IsDigit(name[leading]))
            {
                leading++;
            }

            if (leading > 0 && int.TryParse(name[..leading], NumberStyles.Integer, CultureInfo.InvariantCulture, out var leadingNumber))
            {
                return leadingNumber;
            }

            var trailing = name.Length;
            while (trailing > 0 && char.IsDigit(name[trailing - 1]))
            {
                trailing--;
            }

            if (trailing < name.Length && int.TryParse(name[trailing..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var trailingNumber))
            {
                return trailingNumber;
            }

            return null;
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
