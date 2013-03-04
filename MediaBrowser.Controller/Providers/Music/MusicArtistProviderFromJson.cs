using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

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

        protected override Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entry = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, LastfmHelper.LocalArtistMetaFileName));
                if (entry.HasValue)
                {
                    // read in our saved meta and pass to processing function
                    var data = JsonSerializer.DeserializeFromFile<LastfmArtist>(entry.Value.Path);

                    cancellationToken.ThrowIfCancellationRequested();

                    LastfmHelper.ProcessArtistData(item, data);

                    SetLastRefreshed(item, DateTime.UtcNow);
                    return true;
                }
                return false;
            });
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
            return entry != null ? entry.Value.LastWriteTimeUtc : DateTime.MinValue;
        }

    }
}
