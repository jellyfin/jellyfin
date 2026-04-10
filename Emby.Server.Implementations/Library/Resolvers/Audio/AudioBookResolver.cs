#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Resolves directories containing audio files in a books library as <see cref="AudioBook"/> folders.
    /// Follows the same pattern as <see cref="MusicAlbumResolver"/> for music albums.
    /// </summary>
    public class AudioBookResolver : ItemResolver<AudioBook>
    {
        private readonly NamingOptions _namingOptions;
        private readonly IDirectoryService _directoryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBookResolver"/> class.
        /// </summary>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="directoryService">The directory service.</param>
        public AudioBookResolver(NamingOptions namingOptions, IDirectoryService directoryService)
        {
            _namingOptions = namingOptions;
            _directoryService = directoryService;
        }

        /// <inheritdoc />
        public override ResolverPriority Priority => ResolverPriority.Third;

        /// <inheritdoc />
        protected override AudioBook Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory)
            {
                return null;
            }

            if (args.GetCollectionType() != CollectionType.books)
            {
                return null;
            }

            // Avoid nested audiobooks
            if (args.HasParent<AudioBook>())
            {
                return null;
            }

            if (args.Parent is null || args.Parent.IsRoot)
            {
                return null;
            }

            return ContainsAudioBook(args.FileSystemChildren) ? new AudioBook() : null;
        }

        /// <summary>
        /// Determines whether the file list represents an audiobook, either as a flat collection
        /// of audio files or as part subfolders (e.g. "Part 1/", "Part 2/") each containing audio files.
        /// Mirrors the multi-disc subfolder detection in <see cref="MusicAlbumResolver"/>.
        /// </summary>
        private bool ContainsAudioBook(ICollection<FileSystemMetadata> list)
        {
            // A flat directory with at least one audio file is an audiobook.
            if (list.Any(f => !f.IsDirectory && AudioFileParser.IsAudioFile(f.FullName, _namingOptions)))
            {
                return true;
            }

            // Check for part subfolders (e.g. "Part 1/", "Part 2/") that each contain audio files.
            var partSubfolderCount = 0;
            var parser = new AlbumParser(_namingOptions);

            var directories = list.Where(f => f.IsDirectory);

            var result = Parallel.ForEach(directories, (dir, state) =>
            {
                var children = _directoryService.GetFileSystemEntries(dir.FullName);
                var hasAudio = children.Any(f => !f.IsDirectory && AudioFileParser.IsAudioFile(f.FullName, _namingOptions));

                if (hasAudio)
                {
                    if (parser.IsMultiPart(dir.FullName))
                    {
                        Interlocked.Increment(ref partSubfolderCount);
                    }
                    else
                    {
                        // A subfolder with audio that is not a part folder means this is not a
                        // multi-part audiobook; it would be a nested audiobook, which we don't support.
                        state.Stop();
                    }
                }
            });

            return result.IsCompleted && partSubfolderCount > 0;
        }
    }
}
