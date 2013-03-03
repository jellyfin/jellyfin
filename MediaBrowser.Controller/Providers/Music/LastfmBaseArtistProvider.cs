using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Providers.Music
{
    public abstract class LastfmBaseArtistProvider : LastfmBaseProvider
    {
        protected LastfmBaseArtistProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager) 
            : base(jsonSerializer, httpClient, logManager)
        {
            LocalMetaFileName = "MBArtist.json";
        }

        public override bool Supports(Entities.BaseItem item)
        {
            return item is MusicArtist;
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
    }

    #region Result Objects

    public class LastfmStats
    {
        public string listeners { get; set; }
        public string playcount { get; set; }
    }

    public class LastfmTag
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    public class LastfmFormationInfo
    {
        public string yearfrom { get; set; }
        public string yearto { get; set; }
    }

    public class LastFmBio
    {
        public string published { get; set; }
        public string summary { get; set; }
        public string content { get; set; }
        public string placeformed { get; set; }
        public string yearformed { get; set; }
        public List<LastfmFormationInfo> formationlist { get; set; }
    }

    public class LastfmArtist
    {
        public string name { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
        public string streamable { get; set; }
        public string ontour { get; set; }
        public LastfmStats stats { get; set; }
        public List<LastfmArtist> similar { get; set; }
        public List<LastfmTag> tags { get; set; }
        public LastFmBio bio { get; set; }
    }

    public class LastfmGetArtistResult
    {
        public LastfmArtist artist { get; set; }
    }

    public class Artistmatches
    {
        public List<LastfmArtist> artist { get; set; }
    }

    public class LastfmArtistSearchResult
    {
        public Artistmatches artistmatches { get; set; }
    }

    public class LastfmArtistSearchResults
    {
        public LastfmArtistSearchResult results { get; set; }
    }

    #endregion
}
