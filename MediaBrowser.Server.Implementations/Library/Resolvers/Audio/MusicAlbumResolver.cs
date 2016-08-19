using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Naming.Audio;
using MediaBrowser.Server.Implementations.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using CommonIO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class MusicAlbumResolver
    /// </summary>
    public class MusicAlbumResolver : ItemResolver<MusicAlbum>
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;

        public MusicAlbumResolver(ILogger logger, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get
            {
                // Behind special folder resolver
                return ResolverPriority.Second;
            } 
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>MusicAlbum.</returns>
        protected override MusicAlbum Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory) return null;

            // Avoid mis-identifying top folders
            if (args.Parent.IsRoot) return null;
            if (args.HasParent<MusicAlbum>()) return null;

            var collectionType = args.GetCollectionType();

            var isMusicMediaFolder = string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase);

            // If there's a collection type and it's not music, don't allow it.
            if (!isMusicMediaFolder)
            {
                return null;
            }

            return IsMusicAlbum(args) ? new MusicAlbum() : null;
        }


        /// <summary>
        /// Determine if the supplied file data points to a music album
        /// </summary>
        public bool IsMusicAlbum(string path, IDirectoryService directoryService, LibraryOptions libraryOptions)
        {
            return ContainsMusic(directoryService.GetFileSystemEntries(path), true, directoryService, _logger, _fileSystem, libraryOptions, _libraryManager);
        }

        /// <summary>
        /// Determine if the supplied resolve args should be considered a music album
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if [is music album] [the specified args]; otherwise, <c>false</c>.</returns>
        private bool IsMusicAlbum(ItemResolveArgs args)
        {
            // Args points to an album if parent is an Artist folder or it directly contains music
            if (args.IsDirectory)
            {
                //if (args.Parent is MusicArtist) return true;  //saves us from testing children twice
                if (ContainsMusic(args.FileSystemChildren, true, args.DirectoryService, _logger, _fileSystem, args.GetLibraryOptions(), _libraryManager)) return true;
            }

            return false;
        }

        /// <summary>
        /// Determine if the supplied list contains what we should consider music
        /// </summary>
        private bool ContainsMusic(IEnumerable<FileSystemMetadata> list,
            bool allowSubfolders,
            IDirectoryService directoryService,
            ILogger logger,
            IFileSystem fileSystem,
            LibraryOptions libraryOptions,
            ILibraryManager libraryManager)
        {
            var discSubfolderCount = 0;
            var notMultiDisc = false;

            foreach (var fileSystemInfo in list)
            {
                if ((fileSystemInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (allowSubfolders)
                    {
                        var path = fileSystemInfo.FullName;
                        var isMultiDisc = IsMultiDiscFolder(path, libraryOptions);

                        if (isMultiDisc)
                        {
                            var hasMusic = ContainsMusic(directoryService.GetFileSystemEntries(path), false, directoryService, logger, fileSystem, libraryOptions, libraryManager);

                            if (hasMusic)
                            {
                                logger.Debug("Found multi-disc folder: " + path);
                                discSubfolderCount++;
                            }
                        }
                        else
                        {
                            var hasMusic = ContainsMusic(directoryService.GetFileSystemEntries(path), false, directoryService, logger, fileSystem, libraryOptions, libraryManager);

                            if (hasMusic)
                            {
                                // If there are folders underneath with music that are not multidisc, then this can't be a multi-disc album
                                notMultiDisc = true;
                            }
                        }
                    }
                }

                var fullName = fileSystemInfo.FullName;

                if (libraryManager.IsAudioFile(fullName, libraryOptions))
                {
                    return true;
                }
            }

            if (notMultiDisc)
            {
                return false;
            }

            return discSubfolderCount > 0;
        }

        private bool IsMultiDiscFolder(string path, LibraryOptions libraryOptions)
        {
            var namingOptions = ((LibraryManager)_libraryManager).GetNamingOptions(libraryOptions);

            var parser = new AlbumParser(namingOptions, new PatternsLogger());
            var result = parser.ParseMultiPart(path);

            return result.IsMultiPart;
        }
    }
}
