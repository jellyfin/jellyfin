using System;
using System.Collections.Generic;
using Emby.Naming.Audio;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class MusicAlbumResolver.
    /// </summary>
    public class MusicAlbumResolver : ItemResolver<MusicAlbum>
    {
        private readonly ILogger<MusicAlbumResolver> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicAlbumResolver"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="libraryManager">The library manager.</param>
        public MusicAlbumResolver(ILogger<MusicAlbumResolver> logger, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Second;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>MusicAlbum.</returns>
        protected override MusicAlbum Resolve(ItemResolveArgs args)
        {
            var collectionType = args.GetCollectionType();
            var isMusicMediaFolder = string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase);

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
        public bool IsMusicAlbum(string path, IDirectoryService directoryService)
        {
            return ContainsMusic(directoryService.GetFileSystemEntries(path), true, directoryService, _logger, _fileSystem, _libraryManager);
        }

        /// <summary>
        /// Determine if the supplied resolve args should be considered a music album.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if [is music album] [the specified args]; otherwise, <c>false</c>.</returns>
        private bool IsMusicAlbum(ItemResolveArgs args)
        {
            // Args points to an album if parent is an Artist folder or it directly contains music
            if (args.IsDirectory)
            {
                // if (args.Parent is MusicArtist) return true;  //saves us from testing children twice
                if (ContainsMusic(args.FileSystemChildren, true, args.DirectoryService, _logger, _fileSystem, _libraryManager))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine if the supplied list contains what we should consider music.
        /// </summary>
        private bool ContainsMusic(
            IEnumerable<FileSystemMetadata> list,
            bool allowSubfolders,
            IDirectoryService directoryService,
            ILogger logger,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
        {
            var discSubfolderCount = 0;
            var notMultiDisc = false;

            var namingOptions = ((LibraryManager)_libraryManager).GetNamingOptions();
            var parser = new AlbumParser(namingOptions);
            foreach (var fileSystemInfo in list)
            {
                if (fileSystemInfo.IsDirectory)
                {
                    if (allowSubfolders)
                    {
                        if (notMultiDisc)
                        {
                            continue;
                        }

                        var path = fileSystemInfo.FullName;
                        var hasMusic = ContainsMusic(directoryService.GetFileSystemEntries(path), false, directoryService, logger, fileSystem, libraryManager);

                        if (hasMusic)
                        {
                            if (parser.IsMultiPart(path))
                            {
                                logger.LogDebug("Found multi-disc folder: " + path);
                                discSubfolderCount++;
                            }
                            else
                            {
                                // If there are folders underneath with music that are not multidisc, then this can't be a multi-disc album
                                notMultiDisc = true;
                            }
                        }
                    }
                }
                else
                {
                    var fullName = fileSystemInfo.FullName;

                    if (libraryManager.IsAudioFile(fullName))
                    {
                        return true;
                    }
                }
            }

            if (notMultiDisc)
            {
                return false;
            }

            return discSubfolderCount > 0;
        }
    }
}
