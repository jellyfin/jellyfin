using System;
using System.IO;
using Jellyfin.KodiMetadata.Models;
using Jellyfin.KodiMetadata.Providers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Savers
{
    /// <summary>
    /// The tv season nfo metadata saver.
    /// </summary>
    public class SeasonNfoSaver : BaseNfoSaver<Season, SeasonNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonNfoSaver"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public SeasonNfoSaver(
            ILogger<BaseNfoSaver<Season, SeasonNfo>> logger,
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager)
            : base(logger, xmlSerializer, fileSystem, configurationManager)
        {
        }

        /// <inheritdoc />
        public override string GetSavePath(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            return SeasonNfoProvider.GetSeasonSavePath(new ItemInfo(item));
        }

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            if (!(item is Season))
            {
                return false;
            }

            return updateType >= MinimumUpdateType || (updateType >= ItemUpdateType.MetadataImport && File.Exists(GetSavePath(item)));
        }

        /// <inheritdoc />
        protected override void MapJellyfinToNfoObject(Season? item, SeasonNfo nfo)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            if (nfo == null)
            {
                throw new ArgumentException("Nfo object can't be null", nameof(nfo));
            }

            nfo.SeasonNumber = item.IndexNumber;

            base.MapJellyfinToNfoObject(item, nfo);
        }
    }
}
