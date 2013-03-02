using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.Movies
{
    /// <summary>
    /// Class MovieProviderFromJson
    /// </summary>
    public class MovieProviderFromJson : MovieDbProvider
    {
        public MovieProviderFromJson(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
            : base(jsonSerializer, httpClient, logManager)
        {
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
            var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, LOCAL_META_FILE_NAME));
            return entry != null ? entry.Value.LastWriteTimeUtc : DateTime.MinValue;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.ResolveArgs.ContainsMetaFileByName(ALT_META_FILE_NAME))
            {
                return false; // don't read our file if 3rd party data exists
            }

            if (!item.ResolveArgs.ContainsMetaFileByName(LOCAL_META_FILE_NAME))
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
        protected override Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            // Since we don't have anything truly async, and since deserializing can be expensive, create a task to force parallelism
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, LOCAL_META_FILE_NAME));
                if (entry.HasValue)
                {
                    // read in our saved meta and pass to processing function
                    var movieData = JsonSerializer.DeserializeFromFile<CompleteMovieData>(entry.Value.Path);

                    cancellationToken.ThrowIfCancellationRequested();

                    ProcessMainInfo(item, movieData);

                    SetLastRefreshed(item, DateTime.UtcNow);
                    return true;
                }
                return false;
            });
        }
    }
}
