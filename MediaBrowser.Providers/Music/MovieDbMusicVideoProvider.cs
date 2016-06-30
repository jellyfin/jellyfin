using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class MovieDbMusicVideoProvider : IRemoteMetadataProvider<MusicVideo, MusicVideoInfo>
    {
        public Task<MetadataResult<MusicVideo>> GetMetadata(MusicVideoInfo info, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetItemMetadata<MusicVideo>(info, cancellationToken);
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
        }

        public string Name
        {
            get { return MovieDbProvider.Current.Name; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
