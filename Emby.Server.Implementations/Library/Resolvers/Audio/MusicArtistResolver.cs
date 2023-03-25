#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// The music artist resolver.
    /// </summary>
    public class MusicArtistResolver : ItemResolver<MusicArtist>
    {
        private readonly ILogger<MusicAlbumResolver> _logger;
        private readonly NamingOptions _namingOptions;
        private readonly IDirectoryService _directoryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicArtistResolver"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="MusicAlbumResolver"/> interface.</param>
        /// <param name="namingOptions">The <see cref="NamingOptions"/>.</param>
        /// <param name="directoryService">The directory service.</param>
        public MusicArtistResolver(
            ILogger<MusicAlbumResolver> logger,
            NamingOptions namingOptions,
            IDirectoryService directoryService)
        {
            _logger = logger;
            _namingOptions = namingOptions;
            _directoryService = directoryService;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Second;

        /// <summary>
        /// Resolves the specified resolver arguments.
        /// </summary>
        /// <param name="args">The resolver arguments.</param>
        /// <returns>A <see cref="MusicArtist"/>.</returns>
        protected override MusicArtist Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory)
            {
                return null;
            }

            // Don't allow nested artists
            if (args.HasParent<MusicArtist>() || args.HasParent<MusicAlbum>())
            {
                return null;
            }

            var collectionType = args.GetCollectionType();

            var isMusicMediaFolder = string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase);

            // If there's a collection type and it's not music, it can't be a music artist
            if (!isMusicMediaFolder)
            {
                return null;
            }

            if (args.ContainsFileSystemEntryByName("artist.nfo"))
            {
                return new MusicArtist();
            }

            // Avoid mis-identifying top folders
            if (args.Parent.IsRoot)
            {
                return null;
            }

            var albumResolver = new MusicAlbumResolver(_logger, _namingOptions, _directoryService);

            var directories = args.FileSystemChildren.Where(i => i.IsDirectory);

            var result = Parallel.ForEach(directories, (fileSystemInfo, state) =>
            {
                // If we contain a artist subfolder assume we are an artist folder
                foreach (var subfolder in _namingOptions.ArtistSubfolders)
                {
                    if (fileSystemInfo.Name.Equals(subfolder, StringComparison.OrdinalIgnoreCase))
                    {
                        // Stop once we see an artist subfolder
                        state.Stop();
                    }
                }

                // If we contain a music album assume we are an artist folder
                if (albumResolver.IsMusicAlbum(fileSystemInfo.FullName, _directoryService))
                {
                    // Stop once we see a music album
                    state.Stop();
                }
            });

            return !result.IsCompleted ? new MusicArtist() : null;
        }
    }
}
