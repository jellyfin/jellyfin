using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.Music
{
    /// <summary>
    /// Class MovieDbProvider
    /// </summary>
    public abstract class LastfmBaseProvider : BaseMetadataProvider
    {
        protected static readonly SemaphoreSlim LastfmResourcePool = new SemaphoreSlim(5, 5);

        /// <summary>
        /// Initializes a new instance of the <see cref="LastfmBaseProvider" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        protected LastfmBaseProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
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

        protected override string ProviderVersion
        {
            get
            {
                return "04-24-2013";
            }
        }

        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

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

        protected virtual bool SaveLocalMeta
        {
            get
            {
                return ConfigurationManager.Configuration.SaveLocalMeta;
            }
        }

        /// <summary>
        /// If we save locally, refresh if they delete something
        /// </summary>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return SaveLocalMeta;
            }
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

        protected const string RootUrl = @"http://ws.audioscrobbler.com/2.0/?";
        protected static string ApiKey = "7b76553c3eb1d341d642755aecc40a33";

        /// <summary>
        /// Determines whether [has local meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local meta] [the specified item]; otherwise, <c>false</c>.</returns>
        protected bool HasLocalMeta(BaseItem item)
        {
            return item.ResolveArgs.ContainsMetaFileByName(LocalMetaFileName);
        }

        /// <summary>
        /// Fetches the items data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Task.</returns>
        protected virtual async Task FetchData(BaseItem item, CancellationToken cancellationToken)
        {
            var id = item.GetProviderId(MetadataProviders.Musicbrainz) ?? await FindId(item, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(id))
            {
                Logger.Debug("LastfmProvider - getting info for {0}", item.Name);

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

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.DontFetchMeta) return false;

            if (RefreshOnFileSystemStampChange && HasFileSystemStampChanged(item, providerInfo))
            {
                //If they deleted something from file system, chances are, this item was mis-identified the first time
                item.SetProviderId(MetadataProviders.Musicbrainz, null);
                Logger.Debug("LastfmProvider reports file system stamp change...");
                return true;
            }

            if (providerInfo.LastRefreshStatus != ProviderRefreshStatus.Success)
            {
                Logger.Debug("LastfmProvider for {0} - last attempt had errors.  Will try again.", item.Path);
                return true;
            }

            if (RefreshOnVersionChange && ProviderVersion != providerInfo.ProviderVersion)
            {
                Logger.Debug("LastfmProvider version change re-running for {0}", item.Path);
                return true;
            }

            if (DateTime.UtcNow.Subtract(providerInfo.LastRefreshed).TotalDays > ConfigurationManager.Configuration.MetadataRefreshDays) // only refresh every n days
                return true;

            return false;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await FetchData(item, cancellationToken).ConfigureAwait(false);
            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }
    }

    #region Result Objects

    public class LastfmStats
    {
        public string listeners { get; set; }
        public string playcount { get; set; }
    }

    public class LastfmTag
    {
        public string name { get; set; }
        public string url { get; set; }
    }


    public class LastfmTags
    {
        public List<LastfmTag> tag { get; set; }
    }

    public class LastfmFormationInfo
    {
        public string yearfrom { get; set; }
        public string yearto { get; set; }
    }

    public class LastFmBio
    {
        public string published { get; set; }
        public string summary { get; set; }
        public string content { get; set; }
        public string placeformed { get; set; }
        public string yearformed { get; set; }
        public List<LastfmFormationInfo> formationlist { get; set; }
    }

    public class LastfmArtist
    {
        public string name { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
        public string streamable { get; set; }
        public string ontour { get; set; }
        public LastfmStats stats { get; set; }
        public List<LastfmArtist> similar { get; set; }
        public LastfmTags tags { get; set; }
        public LastFmBio bio { get; set; }
    }


    public class LastfmAlbum
    {
        public string name { get; set; }
        public string artist { get; set; }
        public string id { get; set; }
        public string mbid { get; set; }
        public string releasedate { get; set; }
        public int listeners { get; set; }
        public int playcount { get; set; }
        public LastfmTags toptags { get; set; }
        public LastFmBio wiki { get; set; }
    }

    public class LastfmGetAlbumResult
    {
        public LastfmAlbum album { get; set; }
    }

    public class LastfmGetArtistResult
    {
        public LastfmArtist artist { get; set; }
    }

    public class Artistmatches
    {
        public List<LastfmArtist> artist { get; set; }
    }

    public class LastfmArtistSearchResult
    {
        public Artistmatches artistmatches { get; set; }
    }

    public class LastfmArtistSearchResults
    {
        public LastfmArtistSearchResult results { get; set; }
    }

    #endregion
}
