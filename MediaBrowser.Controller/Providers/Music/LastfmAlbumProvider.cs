using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MoreLinq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.Music
{
    public class LastfmAlbumProvider : LastfmBaseProvider
    {
        private static readonly Task<string> BlankId = Task.FromResult("");

        private readonly IProviderManager _providerManager;
        
        public LastfmAlbumProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager)
        {
            _providerManager = providerManager;
            LocalMetaFileName = LastfmHelper.LocalAlbumMetaFileName;
        }

        protected override Task<string> FindId(BaseItem item, CancellationToken cancellationToken)
        {
            // We don't fetch by id
            return BlankId;
        }

        protected override async Task FetchLastfmData(BaseItem item, string id, CancellationToken cancellationToken)
        {
            var result = await GetAlbumResult(item, cancellationToken).ConfigureAwait(false);

            if (result != null && result.album != null)
            {
                LastfmHelper.ProcessAlbumData(item, result.album);
                //And save locally if indicated
                if (ConfigurationManager.Configuration.SaveLocalMeta)
                {
                    var ms = new MemoryStream();
                    JsonSerializer.SerializeToStream(result.album, ms);

                    cancellationToken.ThrowIfCancellationRequested();

                    await _providerManager.SaveToLibraryFilesystem(item, Path.Combine(item.MetaLocation, LocalMetaFileName), ms, cancellationToken).ConfigureAwait(false);
                    
                }
            }
        }

        private async Task<LastfmGetAlbumResult> GetAlbumResult(BaseItem item, CancellationToken cancellationToken)
        {
            var result = await GetAlbumResult(item.Parent.Name, item.Name, cancellationToken);

            if (result != null && result.album != null)
            {
                return result;
            }

            var folder = (Folder)item;

            // Get each song, distinct by the combination of AlbumArtist and Album
            var songs = folder.RecursiveChildren.OfType<Audio>().DistinctBy(i => (i.AlbumArtist ?? string.Empty) + (i.Album ?? string.Empty), StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var song in songs.Where(song => !string.IsNullOrEmpty(song.Album) && !string.IsNullOrEmpty(song.AlbumArtist)))
            {
                result = await GetAlbumResult(song.AlbumArtist, song.Album, cancellationToken).ConfigureAwait(false);

                if (result != null && result.album != null)
                {
                    return result;
                }
            }

            return null;
        }

        private async Task<LastfmGetAlbumResult> GetAlbumResult(string artist, string album, CancellationToken cancellationToken)
        {
            // Get albu info using artist and album name
            var url = RootUrl + string.Format("method=album.getInfo&artist={0}&album={1}&api_key={2}&format=json", UrlEncode(artist), UrlEncode(album), ApiKey);

            using (var json = await HttpClient.Get(url, LastfmResourcePool, cancellationToken).ConfigureAwait(false))
            {
                return JsonSerializer.DeserializeFromStream<LastfmGetAlbumResult>(json);
            }
        }
        
        protected override Task FetchData(BaseItem item, CancellationToken cancellationToken)
        {
            return FetchLastfmData(item, string.Empty, cancellationToken);
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
        }

        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return true;
            }
        }
    }
}
