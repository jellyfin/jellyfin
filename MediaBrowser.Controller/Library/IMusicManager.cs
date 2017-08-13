using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using System.Collections.Generic;
using MediaBrowser.Controller.Dto;

namespace MediaBrowser.Controller.Library
{
    public interface IMusicManager
    {
        /// <summary>
        /// Gets the instant mix from song.
        /// </summary>
        List<BaseItem> GetInstantMixFromItem(BaseItem item, User user, DtoOptions dtoOptions);

        /// <summary>
        /// Gets the instant mix from artist.
        /// </summary>
        List<BaseItem> GetInstantMixFromArtist(MusicArtist artist, User user, DtoOptions dtoOptions);

        /// <summary>
        /// Gets the instant mix from genre.
        /// </summary>
        List<BaseItem> GetInstantMixFromGenres(IEnumerable<string> genres, User user, DtoOptions dtoOptions);
    }
}
