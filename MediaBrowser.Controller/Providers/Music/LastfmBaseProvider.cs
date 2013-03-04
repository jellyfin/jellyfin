using System.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Providers.Music
{
    class LastfmProviderException : ApplicationException
    {
        public LastfmProviderException(string msg)
            : base(msg)
        {
        }
     
    }
    /// <summary>
    /// Class MovieDbProvider
    /// </summary>
    public abstract class LastfmBaseProvider : BaseMetadataProvider
    {
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
        /// The name of the local json meta file for this item type
        /// </summary>
        protected string LocalMetaFileName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LastfmBaseProvider" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The Log manager</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public LastfmBaseProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager)
            : base(logManager)
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
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
        /// If we save locally, refresh if they delete something
        /// </summary>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return Kernel.Instance.Configuration.SaveLocalMeta;
            }
        }

        protected const string RootUrl = @"http://ws.audioscrobbler.com/2.0/?";
        protected static string ApiKey = "7b76553c3eb1d341d642755aecc40a33";

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.DontFetchMeta) return false;

            if (Kernel.Instance.Configuration.SaveLocalMeta && HasFileSystemStampChanged(item, providerInfo))
            {
                //If they deleted something from file system, chances are, this item was mis-identified the first time
                item.SetProviderId(MetadataProviders.Musicbrainz, null);
                Logger.Debug("LastfmProvider reports file system stamp change...");
                return true;

            }

            if (providerInfo.LastRefreshStatus == ProviderRefreshStatus.CompletedWithErrors)
            {
                Logger.Debug("LastfmProvider for {0} - last attempt had errors.  Will try again.", item.Path);
                return true;
            }

            var downloadDate = providerInfo.LastRefreshed;

            if (Kernel.Instance.Configuration.MetadataRefreshDays == -1 && downloadDate != DateTime.MinValue)
            {
                return false;
            }

            if (DateTime.Today.Subtract(item.DateCreated).TotalDays > 180 && downloadDate != DateTime.MinValue)
                return false; // don't trigger a refresh data for item that are more than 6 months old and have been refreshed before

            if (DateTime.Today.Subtract(downloadDate).TotalDays < Kernel.Instance.Configuration.MetadataRefreshDays) // only refresh every n days
                return false;


            Logger.Debug("LastfmProvider - " + item.Name + " needs refresh.  Download date: " + downloadDate + " item created date: " + item.DateCreated + " Check for Update age: " + Kernel.Instance.Configuration.MetadataRefreshDays);
            return true;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            if (item.DontFetchMeta)
            {
                Logger.Info("LastfmProvider - Not fetching because requested to ignore " + item.Name);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!Kernel.Instance.Configuration.SaveLocalMeta || !HasLocalMeta(item) || (force && !HasLocalMeta(item)))
            {
                try
                {
                    await FetchData(item, cancellationToken).ConfigureAwait(false);
                    SetLastRefreshed(item, DateTime.UtcNow);
                }
                catch (LastfmProviderException)
                {
                    SetLastRefreshed(item, DateTime.UtcNow, ProviderRefreshStatus.CompletedWithErrors);
                }

                return true;
            }
            Logger.Debug("LastfmProvider not fetching because local meta exists for " + item.Name);
            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Determines whether [has local meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local meta] [the specified item]; otherwise, <c>false</c>.</returns>
        private bool HasLocalMeta(BaseItem item)
        {
            return item.ResolveArgs.ContainsMetaFileByName(LocalMetaFileName);
        }

        /// <summary>
        /// Fetches the items data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Task.</returns>
        protected async Task FetchData(BaseItem item, CancellationToken cancellationToken)
        {
            var id = item.GetProviderId(MetadataProviders.Musicbrainz) ?? await FindId(item, cancellationToken).ConfigureAwait(false);
            if (id != null)
            {
                Logger.Debug("LastfmProvider - getting info with id: " + id);

                cancellationToken.ThrowIfCancellationRequested();

                item.SetProviderId(MetadataProviders.Musicbrainz, id);

                await FetchLastfmData(item, id, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Logger.Info("LastfmProvider could not find " + item.Name + ". Check name on Last.fm.");
            }
            
        }

        protected abstract Task<string> FindId(BaseItem item, CancellationToken cancellationToken);

        protected abstract Task FetchLastfmData(BaseItem item, string id, CancellationToken cancellationToken);

        /// <summary>
        /// Encodes an URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        protected static string UrlEncode(string name)
        {
            return WebUtility.UrlEncode(name);
        }

    }
}
