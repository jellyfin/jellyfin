using Jellyfin.KodiMetadata.Models;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.KodiMetadata.Providers
{
    /// <summary>
    /// Movie nfo metadata provider.
    /// </summary>
    public class MovieNfoProvider : BaseNfoProvider<Movie, MovieNfo>
    {
        /// <inheritdoc/>
        public override void MapNfoToJellyfinObject(MovieNfo nfo)
        {
            base.MapNfoToJellyfinObject(nfo);
            /*
             * Map properties special to movies
             */
            throw new System.NotImplementedException();
        }
    }
}
