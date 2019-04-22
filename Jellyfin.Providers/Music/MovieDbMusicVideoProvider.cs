using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Common.Net;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Providers;
using Jellyfin.Providers.Movies;

namespace Jellyfin.Providers.Music
{
    public class MovieDbMusicVideoProvider : IRemoteMetadataProvider<MusicVideo, MusicVideoInfo>
    {
        public Task<MetadataResult<MusicVideo>> GetMetadata(MusicVideoInfo info, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetItemMetadata<MusicVideo>(info, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<RemoteSearchResult>)new List<RemoteSearchResult>());
        }

        public string Name => MovieDbProvider.Current.Name;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
