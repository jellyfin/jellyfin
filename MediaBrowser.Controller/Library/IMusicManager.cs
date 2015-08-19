using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    public interface IMusicManager
    {
        /// <summary>
        /// Gets the instant mix from song.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Audio}.</returns>
        IEnumerable<Audio> GetInstantMixFromItem(BaseItem item, User user);
        /// <summary>
        /// Gets the instant mix from artist.
        /// </summary>
        /// <param name="artist">The artist.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Audio}.</returns>
        IEnumerable<Audio> GetInstantMixFromArtist(MusicArtist artist, User user);
        /// <summary>
        /// Gets the instant mix from genre.
        /// </summary>
        /// <param name="genres">The genres.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Audio}.</returns>
        IEnumerable<Audio> GetInstantMixFromGenres(IEnumerable<string> genres, User user);
    }
}
