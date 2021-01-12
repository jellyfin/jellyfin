using System;
using Jellyfin.KodiMetadata.Models;
using Jellyfin.KodiMetadata.Providers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Savers
{
    /// <summary>
    /// The tv series nfo metadata saver.
    /// </summary>
    public class SeriesNfoSaver : BaseNfoSaver<Series, SeriesNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesNfoSaver"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public SeriesNfoSaver(
            ILogger<BaseNfoSaver<Series, SeriesNfo>> logger,
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

            return SeriesNfoProvider.GetSeriesSavePath(new ItemInfo(item));
        }

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
            => item.SupportsLocalMetadata && item is Series && updateType >= MinimumUpdateType;

        /// <inheritdoc />
        protected override void MapJellyfinToNfoObject(Series? item, SeriesNfo nfo)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            if (nfo == null)
            {
                throw new ArgumentException("Nfo object can't be null", nameof(nfo));
            }

            // todo change ids to not use the method from the kodi wiki
            nfo.Ids = new[]
            {
                new IdNfo()
                {
                    TvdbId = item.GetProviderId(MetadataProvider.Tvdb)
                }
            };
            nfo.Season = -1;
            nfo.Episode = -1;
            if (item.Status.HasValue)
            {
                nfo.Status = item.Status.Value.ToString();
            }

            // todo episodeguide

            base.MapJellyfinToNfoObject(item, nfo);
        }
    }
}
