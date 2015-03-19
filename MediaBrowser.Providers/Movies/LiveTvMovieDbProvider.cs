using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    public class LiveTvMovieDbProvider : IRemoteMetadataProvider<LiveTvProgram, LiveTvProgramLookupInfo>, IHasOrder
    {
        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(LiveTvProgramLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetMovieSearchResults(searchInfo, cancellationToken);
        }

        public Task<MetadataResult<LiveTvProgram>> GetMetadata(LiveTvProgramLookupInfo info, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetItemMetadata<LiveTvProgram>(info, cancellationToken);
        }

        public string Name
        {
            get { return "LiveTvMovieDbProvider"; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetImageResponse(url, cancellationToken);
        }

        public int Order
        {
            get { return 1; }
        }
    }
}