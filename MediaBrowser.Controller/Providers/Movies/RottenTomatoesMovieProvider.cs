using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.Movies
{
    /// <summary>
    /// Class RottenTomatoesMovieProvider
    /// </summary>
    public class RottenTomatoesMovieProvider : BaseMetadataProvider
    {
        // http://developer.rottentomatoes.com/iodocs
        /// <summary>
        /// The API key
        /// </summary>
        private const string ApiKey = "x9wjnvv39ntjmt9zs95nm7bg";

        /// <summary>
        /// The _rotten tomatoes resource pool
        /// </summary>
        private SemaphoreSlim _rottenTomatoesResourcePool = new SemaphoreSlim(3, 3);

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        protected IJsonSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RottenTomatoesMovieProvider"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public RottenTomatoesMovieProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
            : base(logManager, configurationManager)
        {
            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "1";
            }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Movie;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (providerInfo.LastRefreshStatus != ProviderRefreshStatus.Success)
            {
                Logger.Debug("RottenTomatoesMovieProvider for {0} - last attempt had errors.  Will try again.", item.Path);
                return true;
            }

            // Refresh if imdb id has changed
            if (!string.Equals(item.GetProviderId(MetadataProviders.Imdb), providerInfo.CustomData))
            {
                return true;
            }

            if (DateTime.Today.Subtract(providerInfo.LastRefreshed).TotalDays < ConfigurationManager.Configuration.MetadataRefreshDays)
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            // Do work here


            // Lastly, record the Imdb id here
            BaseProviderInfo data;

            if (item.ProviderData.TryGetValue(Id, out data))
            {
                data.CustomData = item.GetProviderId(MetadataProviders.Imdb);
            }

            SetLastRefreshed(item, DateTime.UtcNow);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get
            {
                // Run after moviedb and xml providers
                return MetadataProviderPriority.Last;
            }
        }
    }
}
