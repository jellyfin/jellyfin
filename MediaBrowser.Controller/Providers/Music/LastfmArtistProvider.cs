using System;
using System.Globalization;
using System.Net;
using System.Text;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.Music
{
    public class LastfmArtistProvider : LastfmBaseProvider
    {
        private readonly IProviderManager _providerManager;
        
        public LastfmArtistProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager)
        {
            _providerManager = providerManager;
            LocalMetaFileName = LastfmHelper.LocalArtistMetaFileName;
        }

        protected override async Task<string> FindId(BaseItem item, CancellationToken cancellationToken)
        {
            // Try to find the id using last fm
            var result = await FindIdFromLastFm(item, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(result.Item1))
            {
                return result.Item1;
            }

            // If there were no artists returned at all, then don't bother with musicbrainz
            if (!result.Item2)
            {
                return null;
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

        private async Task<Tuple<string,bool>> FindIdFromLastFm(BaseItem item, CancellationToken cancellationToken)
        {
            //Execute the Artist search against our name and assume first one is the one we want
            var url = RootUrl + string.Format("method=artist.search&artist={0}&api_key={1}&format=json", UrlEncode(item.Name), ApiKey);

            LastfmArtistSearchResults searchResult;

            try
            {
                using (var json = await HttpClient.Get(url, LastfmResourcePool, cancellationToken).ConfigureAwait(false))
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

                return new Tuple<string, bool>(artist.mbid, true);
            }

            return new Tuple<string,bool>(null, false);
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

            using (var json = await HttpClient.Get(url, LastfmResourcePool, cancellationToken).ConfigureAwait(false))
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
