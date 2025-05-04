#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// The music album resolver.
    /// </summary>
    public class MusicAlbumResolver : ItemResolver<MusicAlbum>
    {
        private readonly ILogger<MusicAlbumResolver> _logger;
        private readonly NamingOptions _namingOptions;
        private readonly IDirectoryService _directoryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicAlbumResolver"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="directoryService">The directory service.</param>
        public MusicAlbumResolver(ILogger<MusicAlbumResolver> logger, NamingOptions namingOptions, IDirectoryService directoryService)
        {
            _logger = logger;
            _namingOptions = namingOptions;
            _directoryService = directoryService;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Third;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>MusicAlbum.</returns>
        protected override MusicAlbum Resolve(ItemResolveArgs args)
        {
            var collectionType = args.GetCollectionType();
            var isMusicMediaFolder = collectionType == CollectionType.music;

            // If there's a collection type and it's not music, don't allow it.
            if (!isMusicMediaFolder)
            {
                return null;
            }

            if (!args.IsDirectory)
            {
                return null;
            }

            // Avoid mis-identifying top folders
            if (args.HasParent<MusicAlbum>())
            {
                return null;
            }

            if (args.Parent.IsRoot)
            {
                return null;
            }

            return IsMusicAlbum(args) ? new MusicAlbum() : null;
        }

        /// <summary>
        /// Determine if the supplied file data points to a music album.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns><c>true</c> if the provided path points to a music album; otherwise, <c>false</c>.</returns>
        public bool IsMusicAlbum(string path, IDirectoryService directoryService)
        {
            return ContainsMusic(directoryService.GetFileSystemEntries(path), true, directoryService);
        }

        /// <summary>
        /// Determine if the supplied resolve args should be considered a music album.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if [is music album] [the specified args]; otherwise, <c>false</c>.</returns>
        private bool IsMusicAlbum(ItemResolveArgs args)
        {
            if (args.IsDirectory)
            {
                // If args is a artist subfolder it's not a music album
                foreach (var subfolder in _namingOptions.ArtistSubfolders)
                {
                    if (Path.GetDirectoryName(args.Path.AsSpan()).Equals(subfolder, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Found release folder: {Path}", args.Path);
                        return false;
                    }
                }

                // If args contains music it's a music album
                if (ContainsMusic(args.FileSystemChildren, true, _directoryService))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine if the supplied list contains what we should consider music.
        /// </summary>
        /// <returns><c>true</c> if the provided path list contains music; otherwise, <c>false</c>.</returns>
        private bool ContainsMusic(
            ICollection<FileSystemMetadata> list,
            bool allowSubfolders,
            IDirectoryService directoryService)
        {
            // Check for audio files before digging down into directories
            var foundAudioFile = list.Any(fileSystemInfo => !fileSystemInfo.IsDirectory && AudioFileParser.IsAudioFile(fileSystemInfo.FullName, _namingOptions));
            if (foundAudioFile)
            {
                // At least one audio file exists
                return true;
            }

            if (!allowSubfolders)
            {
                // Not music since no audio file exists and we're not looking into subfolders
                return false;
            }

            var discSubfolderCount = 0;

            var parser = new AlbumParser(_namingOptions);

            var directories = list.Where(fileSystemInfo => fileSystemInfo.IsDirectory);

            var result = Parallel.ForEach(directories, (fileSystemInfo, state) =>
            {
                var path = fileSystemInfo.FullName;
                var hasMusic = ContainsMusic(directoryService.GetFileSystemEntries(path), false, directoryService);

                if (hasMusic)
                {
                    if (parser.IsMultiPart(path))
                    {
                        _logger.LogDebug("Found multi-disc folder: {Path}", path);
                        Interlocked.Increment(ref discSubfolderCount);
                    }
                    else
                    {
                        // If there are folders underneath with music that are not multidisc, then this can't be a multi-disc album
                        state.Stop();
                    }
                }
            });

            if (!result.IsCompleted)
            {
                return false;
            }

            return discSubfolderCount > 0;
        }
    }
}
