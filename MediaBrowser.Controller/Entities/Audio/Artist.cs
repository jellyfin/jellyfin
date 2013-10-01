using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class Artist
    /// </summary>
    public class Artist : BaseItem, IItemByName, IHasMusicGenres
    {
        public Artist()
        {
            UserItemCounts = new Dictionary<Guid, ItemByNameCounts>();
        }

        public string LastFmImageUrl { get; set; }
        
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return GetUserDataKey(this);
        }

        [IgnoreDataMember]
        public Dictionary<Guid, ItemByNameCounts> UserItemCounts { get; set; }

        /// <summary>
        /// Finds the music artist.
        /// </summary>
        /// <param name="artist">The artist.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <returns>MusicArtist.</returns>
        public static MusicArtist FindMusicArtist(Artist artist, ILibraryManager libraryManager)
        {
            return FindMusicArtist(artist, libraryManager.RootFolder.RecursiveChildren.OfType<MusicArtist>());
        }

        /// <summary>
        /// Finds the music artist.
        /// </summary>
        /// <param name="artist">The artist.</param>
        /// <param name="allMusicArtists">All music artists.</param>
        /// <returns>MusicArtist.</returns>
        public static MusicArtist FindMusicArtist(Artist artist, IEnumerable<MusicArtist> allMusicArtists)
        {
            var musicBrainzId = artist.GetProviderId(MetadataProviders.Musicbrainz);

            return allMusicArtists.FirstOrDefault(i =>
            {
                if (!string.IsNullOrWhiteSpace(musicBrainzId) && string.Equals(musicBrainzId, i.GetProviderId(MetadataProviders.Musicbrainz), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return string.Compare(i.Name, artist.Name, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols) == 0;
            });
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public static string GetUserDataKey(BaseItem item)
        {
            var id = item.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }
            
            return "Artist-" + item.Name;
        }
    }
}
