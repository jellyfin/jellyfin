using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class MusicAlbumDiscResolver
    /// </summary>
    public class MusicAlbumDiscResolver : ItemResolver<MusicAlbumDisc>
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Second; } // we need to be ahead of the generic folder resolver but behind the movie one
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>MusicAlbum.</returns>
        protected override MusicAlbumDisc Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory) return null;

            return args.Parent is MusicAlbum ? new MusicAlbumDisc
            {
                DisplayMediaType = "Disc"

            } : null;
        }
    }
}
