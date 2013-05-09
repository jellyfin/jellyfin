using System;
using System.Globalization;
using System.Net;
using System.Text;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Mediabrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers.Music
{
    public class LastfmArtistProvider : LastfmBaseProvider
    {
        private readonly IProviderManager _providerManager;
        private readonly ILibraryManager _libraryManager;

        public LastfmArtistProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, ILibraryManager libraryManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager)
        {
            _providerManager = providerManager;
            _libraryManager = libraryManager;
            LocalMetaFileName = LastfmHelper.LocalArtistMetaFileName;
        }

        /// <summary>
        /// Finds the id.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        protected override async Task<string> FindId(BaseItem item, CancellationToken cancellationToken)
        {
            if (item is Artist)
            {
                // Since MusicArtists are refreshed first, try to find it from one of them
                var id = FindIdFromMusicArtistEntity(item);

                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }

            // Try to find the id using last fm
            var result = await FindIdFromLastFm(item, cancellationToken).ConfigureAwait(false);

            if (result != null)
            {
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }

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

        private string FindIdFromMusicArtistEntity(BaseItem item)
        {
            var artist = _libraryManager.RootFolder.RecursiveChildren.OfType<MusicArtist>()
                .FirstOrDefault(i => string.Equals(i.Name, item.Name, StringComparison.OrdinalIgnoreCase));

            return artist != null ? artist.GetProviderId(MetadataProviders.Musicbrainz) : null;
        }

        private async Task<string> FindIdFromLastFm(BaseItem item, CancellationToken cancellationToken)
        {
            //Execute the Artist search against our name and assume first one is the one we want
            var url = RootUrl + string.Format("method=artist.search&artist={0}&api_key={1}&format=json", UrlEncode(item.Name), ApiKey);

            LastfmArtistSearchResults searchResult;

            try
            {
                using (var json = await HttpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    ResourcePool = LastfmResourcePool,
                    CancellationToken = cancellationToken,
                    EnableResponseCache = true

                }).ConfigureAwait(false))
                {
                    searchResult = JsonSerializer.DeserializeFromStream<LastfmArtistSearchResults>(json);
                }
            }
            catch (HttpException e)
            {
                return null;
            }

            if (searchResult != null && searchResult.results != null && searchResult.results.artistmatches != null && searchResult.results.artistmatches.artist.Count > 0)
            {
                var artist = searchResult.results.artistmatches.artist.FirstOrDefault(i => i.name != null && string.Compare(i.name, item.Name, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace) == 0) ??
                    searchResult.results.artistmatches.artist.First();

                return artist.mbid;
            }

            return null;
        }

        private async Task<string> FindIdFromMusicBrainz(BaseItem item, CancellationToken cancellationToken)
        {
            // They seem to throw bad request failures on any term with a slash
            var nameToSearch = item.Name.Replace('/', ' ');

            var url = string.Format("http://www.musicbrainz.org/ws/2/artist/?query=artist:{0}", UrlEncode(nameToSearch));

            var doc = await FanArtAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
            var node = doc.SelectSingleNode("//mb:artist-list/mb:artist[@type='Group']/@id", ns);

            if (node != null && node.Value != null)
            {
                return node.Value;
            }

            if (HasDiacritics(item.Name))
            {
                // Try again using the search with accent characters url
                url = string.Format("http://www.musicbrainz.org/ws/2/artist/?query=artistaccent:{0}", UrlEncode(nameToSearch));

                doc = await FanArtAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

                ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
                node = doc.SelectSingleNode("//mb:artist-list/mb:artist[@type='Group']/@id", ns);

                if (node != null && node.Value != null)
                {
                    return node.Value;
                }
            }

            return null;
        }

        private bool HasDiacritics(string text)
        {
            return !string.Equals(text, RemoveDiacritics(text), StringComparison.Ordinal);
        }
        
        private string RemoveDiacritics(string text)
        {
            return string.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
        }
        
        protected override async Task FetchLastfmData(BaseItem item, string id, CancellationToken cancellationToken)
        {
            // Get artist info with provided id
            var url = RootUrl + string.Format("method=artist.getInfo&mbid={0}&api_key={1}&format=json", UrlEncode(id), ApiKey);

            LastfmGetArtistResult result;

            using (var json = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = LastfmResourcePool,
                CancellationToken = cancellationToken,
                EnableResponseCache = true

            }).ConfigureAwait(false))
            {
                result = JsonSerializer.DeserializeFromStream<LastfmGetArtistResult>(json);
            }

            if (result != null && result.artist != null)
            {
                LastfmHelper.ProcessArtistData(item, result.artist);
                //And save locally if indicated
                if (SaveLocalMeta)
                {
                    var ms = new MemoryStream();
                    JsonSerializer.SerializeToStream(result.artist, ms);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        ms.Dispose();
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await _providerManager.SaveToLibraryFilesystem(item, Path.Combine(item.MetaLocation, LocalMetaFileName), ms, cancellationToken).ConfigureAwait(false);
                    
                }
            }
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicArtist;
        }
    }
}
