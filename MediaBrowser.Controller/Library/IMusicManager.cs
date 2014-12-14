using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Playlists;
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
        IEnumerable<Audio> GetInstantMixFromSong(Audio item, User user);
        /// <summary>
        /// Gets the instant mix from artist.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Audio}.</returns>
        IEnumerable<Audio> GetInstantMixFromArtist(string name, User user);
        /// <summary>
        /// Gets the instant mix from album.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Audio}.</returns>
        IEnumerable<Audio> GetInstantMixFromAlbum(MusicAlbum item, User user);
        /// <summary>
        /// Gets the instant mix from playlist.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable&lt;Audio&gt;.</returns>
        IEnumerable<Audio> GetInstantMixFromPlaylist(Playlist item, User user);
        /// <summary>
        /// Gets the instant mix from genre.
        /// </summary>
        /// <param name="genres">The genres.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Audio}.</returns>
        IEnumerable<Audio> GetInstantMixFromGenres(IEnumerable<string> genres, User user);
    }
}
