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
        IEnumerable<Audio> GetInstantMixFromItem(BaseItem item, User user, DtoOptions dtoOptions);
       
        /// <summary>
        /// Gets the instant mix from artist.
        /// </summary>
        IEnumerable<Audio> GetInstantMixFromArtist(MusicArtist artist, User user, DtoOptions dtoOptions);
      
        /// <summary>
        /// Gets the instant mix from genre.
        /// </summary>
        IEnumerable<Audio> GetInstantMixFromGenres(IEnumerable<string> genres, User user, DtoOptions dtoOptions);
    }
}
