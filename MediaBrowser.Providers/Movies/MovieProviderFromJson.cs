using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Class MovieProviderFromJson
    /// </summary>
    public class MovieProviderFromJson : MovieDbProvider
    {
        public MovieProviderFromJson(ILogManager logManager, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, IProviderManager providerManager)
            : base(logManager, configurationManager, jsonSerializer, httpClient, providerManager)
        {
        }

        public override bool Supports(BaseItem item)
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return false;
            }

            var trailer = item as Trailer;

            if (trailer != null)
            {
                return !trailer.IsLocalTrailer;
            }

            return item is Movie || item is BoxSet || item is MusicVideo;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get { return false; }
        }

        /// <summary>
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, LocalMetaFileName));
            return entry != null ? entry.LastWriteTimeUtc : DateTime.MinValue;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.ResolveArgs.ContainsMetaFileByName(AltMetaFileName))
            {
                return false; // don't read our file if 3rd party data exists
            }

            if (!item.ResolveArgs.ContainsMetaFileByName(LocalMetaFileName))
            {
                return false; // nothing to read
            }

            // Need to re-override to jump over intermediate implementation
            return CompareDate(item) > providerInfo.LastRefreshed;
        }

        /// <summary>
        /// Fetches the async.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, LocalMetaFileName));
            if (entry != null)
            {
                // read in our saved meta and pass to processing function
                var movieData = JsonSerializer.DeserializeFromFile<CompleteMovieData>(entry.FullName);

                cancellationToken.ThrowIfCancellationRequested();

                ProcessMainInfo(item, movieData);

                SetLastRefreshed(item, DateTime.UtcNow);
                return TrueTaskResult;
            }
            return FalseTaskResult;
        }
    }
}
