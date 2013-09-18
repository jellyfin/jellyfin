using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class MusicArtistResolver
    /// </summary>
    public class MusicArtistResolver : ItemResolver<MusicArtist>
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Third; } // we need to be ahead of the generic folder resolver but behind the movie one
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>MusicArtist.</returns>
        protected override MusicArtist Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory) return null;

            //Avoid mis-identifying top folders
            if (args.Parent == null) return null;
            if (args.Parent.IsRoot) return null;

            // Don't allow nested artists
            if (args.Parent is MusicArtist)
            {
                return null;
            }

            // Optimization
            if (args.Parent is BoxSet || args.Parent is Series || args.Parent is Season)
            {
                return null;
            }

            var collectionType = args.GetCollectionType();

            // If there's a collection type and it's not music, it can't be a series
            if (!string.IsNullOrEmpty(collectionType) &&
                !string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            
            // If we contain an album assume we are an artist folder
            return args.FileSystemChildren.Where(i => (i.Attributes & FileAttributes.Directory) == FileAttributes.Directory).Any(i => MusicAlbumResolver.IsMusicAlbum(i.FullName)) ? new MusicArtist() : null;
        }

    }
}
