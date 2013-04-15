using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
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
    /// Class PersonProviderFromJson
    /// </summary>
    class PersonProviderFromJson : TmdbPersonProvider
    {
        public PersonProviderFromJson(IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager) 
            : base(httpClient, jsonSerializer, logManager, configurationManager, providerManager)
        {
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Person;
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return false;
            }
        }

        // Need to re-override to jump over intermediate implementation
        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (!item.ResolveArgs.ContainsMetaFileByName(MetaFileName))
            {
                return false;
            }

            return CompareDate(item) > providerInfo.LastRefreshed;
        }

        /// <summary>
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation,MetaFileName));
            return entry != null ? entry.Value.LastWriteTimeUtc : DateTime.MinValue;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get
            {
                return MetadataProviderPriority.Third;
            }
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
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var personInfo = JsonSerializer.DeserializeFromFile<PersonResult>(Path.Combine(item.MetaLocation, MetaFileName));

                cancellationToken.ThrowIfCancellationRequested();

                ProcessInfo((Person)item, personInfo);

                SetLastRefreshed(item, DateTime.UtcNow);
                return TrueTaskResult;
            }
            catch (FileNotFoundException)
            {
                // This is okay - just means we force refreshed and there isn't a json file
                return FalseTaskResult;
            }
        }
    }
}
