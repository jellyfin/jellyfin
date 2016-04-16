using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class MusicArtistResolver
    /// </summary>
    public class MusicArtistResolver : ItemResolver<MusicArtist>
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;

        public MusicArtistResolver(ILogger logger, IFileSystem fileSystem, ILibraryManager libraryManager)
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
        /// <returns>MusicArtist.</returns>
        protected override MusicArtist Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory) return null;

            // Avoid mis-identifying top folders
            if (args.Parent.IsRoot) return null;

            // Don't allow nested artists
            if (args.HasParent<MusicArtist>() || args.HasParent<MusicAlbum>())
            {
                return null;
            }

            var collectionType = args.GetCollectionType();

            var isMusicMediaFolder = string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase);

            // If there's a collection type and it's not music, it can't be a series
            if (!isMusicMediaFolder)
            {
                return null;
            }

            var directoryService = args.DirectoryService;

            var albumResolver = new MusicAlbumResolver(_logger, _fileSystem, _libraryManager);

            // If we contain an album assume we are an artist folder
            return args.FileSystemChildren.Where(i => (i.Attributes & FileAttributes.Directory) == FileAttributes.Directory).Any(i => albumResolver.IsMusicAlbum(i.FullName, directoryService)) ? new MusicArtist() : null;
        }

    }
}
