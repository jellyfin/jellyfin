using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    class ArtistProviderFromXml : BaseMetadataProvider
    {
        private readonly IFileSystem _fileSystem;

        public ArtistProviderFromXml(ILogManager logManager, IServerConfigurationManager configurationManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return (item is MusicArtist) && item.LocationType == LocationType.FileSystem;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        private const string XmlFileName = "artist.xml";
        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var xml = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, XmlFileName));

            if (xml == null)
            {
                return false;
            }

            return _fileSystem.GetLastWriteTimeUtc(xml) > item.DateLastSaved;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadataFile = item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.MetaLocation, XmlFileName));

            if (metadataFile != null)
            {
                var path = metadataFile.FullName;

                await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    new BaseItemXmlParser<MusicArtist>(Logger).Fetch((MusicArtist)item, path, cancellationToken);
                }
                finally
                {
                    XmlParsingResourcePool.Release();
                }

                SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
                return true;
            }

            return false;
        }
    }
}
