using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    public class MovieDbTrailerProvider : IRemoteMetadataProvider<Trailer, TrailerInfo>, IHasChangeMonitor
    {
        public Task<MetadataResult<Trailer>> GetMetadata(TrailerInfo info, CancellationToken cancellationToken)
        {
            return MovieDbProvider.Current.GetItemMetadata<Trailer>(info, cancellationToken);
        }

        public string Name
        {
            get { return MovieDbProvider.Current.Name; }
        }

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            return MovieDbProvider.Current.HasChanged(item, date);
        }
    }
}
