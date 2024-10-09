#pragma warning disable CA1002, CS1591

using System.Collections.Generic;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;

namespace MediaBrowser.Controller.Library
{
    public interface IMusicManager
    {
        /// <summary>
        /// Gets the instant mix from song.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <param name="user">The user to use.</param>
        /// <param name="dtoOptions">The options to use.</param>
        /// <returns>List of items.</returns>
        IReadOnlyList<BaseItem> GetInstantMixFromItem(BaseItem item, User? user, DtoOptions dtoOptions);

        /// <summary>
        /// Gets the instant mix from artist.
        /// </summary>
        /// <param name="artist">The artist to use.</param>
        /// <param name="user">The user to use.</param>
        /// <param name="dtoOptions">The options to use.</param>
        /// <returns>List of items.</returns>
        IReadOnlyList<BaseItem> GetInstantMixFromArtist(MusicArtist artist, User? user, DtoOptions dtoOptions);

        /// <summary>
        /// Gets the instant mix from genre.
        /// </summary>
        /// <param name="genres">The genres to use.</param>
        /// <param name="user">The user to use.</param>
        /// <param name="dtoOptions">The options to use.</param>
        /// <returns>List of items.</returns>
        IReadOnlyList<BaseItem> GetInstantMixFromGenres(IEnumerable<string> genres, User? user, DtoOptions dtoOptions);
    }
}
