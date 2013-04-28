using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.Music
{
    public class MusicArtistProviderFromJson : BaseMetadataProvider
    {
        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        protected IJsonSerializer JsonSerializer { get; private set; }

        public MusicArtistProviderFromJson(IJsonSerializer jsonSerializer, ILogManager logManager, IServerConfigurationManager configurationManager) 
            : base(logManager, configurationManager)
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            JsonSerializer = jsonSerializer;

        }

        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, LastfmHelper.LocalArtistMetaFileName));
            if (entry != null)
            {
                // read in our saved meta and pass to processing function
                var data = JsonSerializer.DeserializeFromFile<LastfmArtist>(entry.FullName);

                cancellationToken.ThrowIfCancellationRequested();

                LastfmHelper.ProcessArtistData(item, data);

                item.SetProviderId(MetadataProviders.Musicbrainz, data.mbid);

                SetLastRefreshed(item, DateTime.UtcNow);
                return TrueTaskResult;
            }
            return FalseTaskResult;
        }

        public override MetadataProviderPriority Priority
        {
            get
            {
                return MetadataProviderPriority.First;
            }
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicArtist;
        }

        public override bool RequiresInternet
        {
            get
            {
                return false;
            }
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (!item.ResolveArgs.ContainsMetaFileByName(LastfmHelper.LocalArtistMetaFileName))
            {
                return false; // nothing to read
            }

            // Need to re-override to jump over intermediate implementation
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
            var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, LastfmHelper.LocalArtistMetaFileName));
            return entry != null ? entry.LastWriteTimeUtc : DateTime.MinValue;
        }

    }
}
