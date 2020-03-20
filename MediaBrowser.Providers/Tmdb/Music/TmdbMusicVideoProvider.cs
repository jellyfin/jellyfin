using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Tmdb.Movies;

namespace MediaBrowser.Providers.Tmdb.Music
{
    public class TmdbMusicVideoProvider : IRemoteMetadataProvider<MusicVideo, MusicVideoInfo>
    {
        public Task<MetadataResult<MusicVideo>> GetMetadata(MusicVideoInfo info, CancellationToken cancellationToken)
        {
            return TmdbMovieProvider.Current.GetItemMetadata<MusicVideo>(info, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<RemoteSearchResult>)new List<RemoteSearchResult>());
        }

        public string Name => TmdbMovieProvider.Current.Name;

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
