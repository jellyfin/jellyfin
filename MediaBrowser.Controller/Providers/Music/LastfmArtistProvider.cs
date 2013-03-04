using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Providers.Music
{
    public class LastfmArtistProvider : LastfmBaseArtistProvider
    {
        public LastfmArtistProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager) 
            : base(jsonSerializer, httpClient, logManager)
        {
        }

        protected override async Task<string> FindId(Entities.BaseItem item, System.Threading.CancellationToken cancellationToken)
        {
            //Execute the Artist search against our name and assume first one is the one we want
            var url = RootUrl + string.Format("method=artist.search&artist={0}&api_key={1}&format=json", UrlEncode(item.Name), ApiKey);

            LastfmArtistSearchResults searchResult = null;

            try
            {
                using (var json = await HttpClient.Get(url, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                {
                    searchResult = JsonSerializer.DeserializeFromStream<LastfmArtistSearchResults>(json);
                }
            }
            catch (HttpException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw;
            }

            if (searchResult != null && searchResult.results != null && searchResult.results.artistmatches != null && searchResult.results.artistmatches.artist.Any())
            {
                return searchResult.results.artistmatches.artist.First().mbid;
            }

            return null;
        }

        protected override async Task FetchLastfmData(BaseItem item, string id, CancellationToken cancellationToken)
        {
            // Get artist info with provided id
            var url = RootUrl + string.Format("method=artist.getInfo&mbid={0}&api_key={1}&format=json", UrlEncode(id), ApiKey);

            LastfmGetArtistResult result = null;

            try
            {
                using (var json = await HttpClient.Get(url, Kernel.Instance.ResourcePools.Lastfm, cancellationToken).ConfigureAwait(false))
                {
                    result = JsonSerializer.DeserializeFromStream<LastfmGetArtistResult>(json);
                }
            }
            catch (HttpException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new LastfmProviderException(string.Format("Unable to retrieve artist info for {0} with id {0}", item.Name, id));
                }
                throw;
            }

            if (result != null && result.artist != null)
            {
                ProcessArtistData(item as MusicArtist, result.artist);
            }
        }
    }
}
