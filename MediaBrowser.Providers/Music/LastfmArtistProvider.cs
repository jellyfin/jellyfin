using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Class LastfmArtistProvider
    /// </summary>
    public class LastfmArtistProvider : LastfmBaseProvider
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        protected readonly ILibraryManager LibraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LastfmArtistProvider" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        public LastfmArtistProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, ILibraryManager libraryManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager)
        {
            LibraryManager = libraryManager;
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (HasAltMeta(item))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override string ProviderVersion
        {
            get
            {
                return "9";
            }
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }

        private bool HasAltMeta(BaseItem item)
        {
            return item.LocationType == LocationType.FileSystem && item.ResolveArgs.ContainsMetaFileByName("artist.xml");
        }

        /// <summary>
        /// Finds the id.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> FindId(BaseItem item, CancellationToken cancellationToken)
        {
            try
            {
                // If we don't get anything, go directly to music brainz
                return await FindIdFromMusicBrainz(item, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpException e)
            {
                if (e.StatusCode.HasValue && e.StatusCode.Value == HttpStatusCode.BadRequest)
                {
                    // They didn't like a character in the name. Handle the exception so that the provider doesn't keep retrying over and over
                    return null;
                }

                throw;
            }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = item.GetProviderId(MetadataProviders.Musicbrainz) ?? await FindId(item, cancellationToken).ConfigureAwait(false);
            
            if (!string.IsNullOrWhiteSpace(id))
            {
                cancellationToken.ThrowIfCancellationRequested();

                item.SetProviderId(MetadataProviders.Musicbrainz, id);

                await FetchLastfmData(item, id, force, cancellationToken).ConfigureAwait(false);
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Finds the id from music artist entity.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string FindIdFromMusicArtistEntity(BaseItem item)
        {
            var artist = LibraryManager.RootFolder.RecursiveChildren.OfType<MusicArtist>()
                .FirstOrDefault(i => string.Compare(i.Name, item.Name, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols) == 0);

            return artist != null ? artist.GetProviderId(MetadataProviders.Musicbrainz) : null;
        }

        /// <summary>
        /// Finds the id from music brainz.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> FindIdFromMusicBrainz(BaseItem item, CancellationToken cancellationToken)
        {
            // They seem to throw bad request failures on any term with a slash
            var nameToSearch = item.Name.Replace('/', ' ');

            var url = string.Format("http://www.musicbrainz.org/ws/2/artist/?query=artist:\"{0}\"", UrlEncode(nameToSearch));

            var doc = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
            var node = doc.SelectSingleNode("//mb:artist-list/mb:artist/@id", ns);

            if (node != null && node.Value != null)
            {
                return node.Value;
            }

            if (HasDiacritics(item.Name))
            {
                // Try again using the search with accent characters url
                url = string.Format("http://www.musicbrainz.org/ws/2/artist/?query=artistaccent:\"{0}\"", UrlEncode(nameToSearch));

                doc = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

                ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
                node = doc.SelectSingleNode("//mb:artist-list/mb:artist/@id", ns);

                if (node != null && node.Value != null)
                {
                    return node.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified text has diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the specified text has diacritics; otherwise, <c>false</c>.</returns>
        private bool HasDiacritics(string text)
        {
            return !string.Equals(text, RemoveDiacritics(text), StringComparison.Ordinal);
        }

        /// <summary>
        /// Removes the diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        private string RemoveDiacritics(string text)
        {
            return string.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Fetches the lastfm data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="musicBrainzId">The music brainz id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        protected virtual async Task FetchLastfmData(BaseItem item, string musicBrainzId, bool force, CancellationToken cancellationToken)
        {
            // Get artist info with provided id
            var url = RootUrl + string.Format("method=artist.getInfo&mbid={0}&api_key={1}&format=json", UrlEncode(musicBrainzId), ApiKey);

            LastfmGetArtistResult result;

            using (var json = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = LastfmResourcePool,
                CancellationToken = cancellationToken,
                EnableHttpCompression = false

            }).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(json))
                {
                    var jsonText = await reader.ReadToEndAsync().ConfigureAwait(false);

                    // Fix their bad json
                    jsonText = jsonText.Replace("\"#text\"", "\"url\"");

                    result = JsonSerializer.DeserializeFromString<LastfmGetArtistResult>(jsonText);
                }
            }

            if (result != null && result.artist != null)
            {
                LastfmHelper.ProcessArtistData(item, result.artist);
            }
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is MusicArtist;
        }
    }
}
