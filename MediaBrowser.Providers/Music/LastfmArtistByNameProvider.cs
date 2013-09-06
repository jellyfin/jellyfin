using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Class LastfmArtistByNameProvider
    /// </summary>
    public class LastfmArtistByNameProvider : LastfmArtistProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LastfmArtistByNameProvider" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        public LastfmArtistByNameProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, ILibraryManager libraryManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager, libraryManager)
        {
        }

        /// <summary>
        /// Gets a value indicating whether [save local meta].
        /// </summary>
        /// <value><c>true</c> if [save local meta]; otherwise, <c>false</c>.</value>
        protected override bool SaveLocalMeta
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Artist;
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "7";
            }
        }

        /// <summary>
        /// Fetches the lastfm data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="musicBrainzId">The music brainz id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        protected override async Task FetchLastfmData(BaseItem item, string musicBrainzId, bool force, CancellationToken cancellationToken)
        {
            var artist = (Artist)item;

            // See if we can avoid an http request by finding the matching MusicArtist entity
            var musicArtist = FindMusicArtist(artist, LibraryManager);

            if (musicArtist != null && !force)
            {
                LastfmHelper.ProcessArtistData(musicArtist, artist);
            }
            else
            {
                await base.FetchLastfmData(item, musicBrainzId, force, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Finds the music artist.
        /// </summary>
        /// <param name="artist">The artist.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <returns>MusicArtist.</returns>
        private static MusicArtist FindMusicArtist(Artist artist, ILibraryManager libraryManager)
        {
            var musicBrainzId = artist.GetProviderId(MetadataProviders.Musicbrainz);

            return libraryManager.RootFolder.RecursiveChildren
                .OfType<MusicArtist>()
                .FirstOrDefault(i =>
                {
                    if (!string.IsNullOrWhiteSpace(musicBrainzId) && string.Equals(musicBrainzId, i.GetProviderId(MetadataProviders.Musicbrainz), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return false;
                });
        }
    }
}
