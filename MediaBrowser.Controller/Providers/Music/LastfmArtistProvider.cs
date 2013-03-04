using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Providers.Music
{
    public class LastfmArtistProvider : LastfmBaseProvider
    {

        public LastfmArtistProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager)
        {
            LocalMetaFileName = LastfmHelper.LocalArtistMetaFileName;
        }

        protected override async Task<string> FindId(Entities.BaseItem item, System.Threading.CancellationToken cancellationToken)
        {
            //Execute the Artist search against our name and assume first one is the one we want
            var url = RootUrl + string.Format("method=artist.search&artist={0}&api_key={1}&format=json", UrlEncode(item.Name), ApiKey);

            LastfmArtistSearchResults searchResult = null;

            try
            {
                using (var json = await HttpClient.Get(url, LastfmResourcePool, cancellationToken).ConfigureAwait(false))
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
                using (var json = await HttpClient.Get(url, LastfmResourcePool, cancellationToken).ConfigureAwait(false))
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
                LastfmHelper.ProcessArtistData(item, result.artist);
                //And save locally if indicated
                if (ConfigurationManager.Configuration.SaveLocalMeta)
                {
                    var ms = new MemoryStream();
                    JsonSerializer.SerializeToStream(result.artist, ms);

                    cancellationToken.ThrowIfCancellationRequested();

                    await Kernel.Instance.FileSystemManager.SaveToLibraryFilesystem(item, Path.Combine(item.MetaLocation, LocalMetaFileName), ms, cancellationToken).ConfigureAwait(false);
                    
                }
            }
        }

        public override bool Supports(Entities.BaseItem item)
        {
            return item is MusicArtist;
        }
    }
}
