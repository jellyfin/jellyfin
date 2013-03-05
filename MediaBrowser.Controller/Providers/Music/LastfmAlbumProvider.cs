using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Providers.Music
{
    public class LastfmAlbumProvider : LastfmBaseProvider
    {
        private static readonly Task<string> BlankId = Task.FromResult("0000");

        public LastfmAlbumProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager)
        {
            LocalMetaFileName = LastfmHelper.LocalAlbumMetaFileName;
        }

        protected override Task<string> FindId(BaseItem item, CancellationToken cancellationToken)
        {
            // We don't fetch by id
            return BlankId;
        }

        protected override async Task FetchLastfmData(BaseItem item, string id, CancellationToken cancellationToken)
        {
            // Get albu info using artist and album name
            var url = RootUrl + string.Format("method=album.getInfo&artist={0}&album={1}&api_key={2}&format=json", UrlEncode(item.Parent.Name), UrlEncode(item.Name), ApiKey);

            LastfmGetAlbumResult result = null;

            try
            {
                using (var json = await HttpClient.Get(url, LastfmResourcePool, cancellationToken).ConfigureAwait(false))
                {
                    result = JsonSerializer.DeserializeFromStream<LastfmGetAlbumResult>(json);
                }
            }
            catch (HttpException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new LastfmProviderException(string.Format("Unable to retrieve album info for {0} with artist {1}", item.Name, item.Parent.Name));
                }
                throw;
            }

            if (result != null && result.album != null)
            {
                LastfmHelper.ProcessAlbumData(item, result.album);
                //And save locally if indicated
                if (ConfigurationManager.Configuration.SaveLocalMeta)
                {
                    var ms = new MemoryStream();
                    JsonSerializer.SerializeToStream(result.album, ms);

                    cancellationToken.ThrowIfCancellationRequested();

                    await Kernel.Instance.FileSystemManager.SaveToLibraryFilesystem(item, Path.Combine(item.MetaLocation, LocalMetaFileName), ms, cancellationToken).ConfigureAwait(false);
                    
                }
            }
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
        }
    }
}
