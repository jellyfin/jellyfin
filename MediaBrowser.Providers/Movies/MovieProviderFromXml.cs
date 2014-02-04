using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Savers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Class MovieProviderFromXml
    /// </summary>
    public class MovieProviderFromXml : BaseMetadataProvider
    {
        private readonly IItemRepository _itemRepo;
        private readonly IFileSystem _fileSystem;

        public MovieProviderFromXml(ILogManager logManager, IServerConfigurationManager configurationManager, IItemRepository itemRepo, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            _itemRepo = itemRepo;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
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

            // Check parent for null to avoid running this against things like video backdrops
            return item is Video && !(item is Episode) && item.Parent != null;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var savePath = MovieXmlSaver.GetMovieSavePath((Video)item);

            var xml = item.ResolveArgs.GetMetaFileByPath(savePath) ?? new FileInfo(savePath);

            if (!xml.Exists)
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

            var video = (Video)item;

            var path = MovieXmlSaver.GetMovieSavePath(video);

            if (File.Exists(path))
            {
                await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    new MovieXmlParser(Logger).FetchAsync(video, path, cancellationToken);
                }
                finally
                {
                    XmlParsingResourcePool.Release();
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);

            return true;
        }
    }
}
