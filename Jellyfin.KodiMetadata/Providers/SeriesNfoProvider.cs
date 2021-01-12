using System;
using System.IO;
using Jellyfin.KodiMetadata.Models;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Providers
{
    /// <summary>
    /// The series nfo metadata provider.
    /// </summary>
    public class SeriesNfoProvider : BaseNfoProvider<Series, SeriesNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public SeriesNfoProvider(ILogger<BaseNfoProvider<Series, SeriesNfo>> logger, IFileSystem fileSystem, IXmlSerializer xmlSerializer)
            : base(logger, fileSystem, xmlSerializer)
        {
        }

        /// <inheritdoc/>
        public override void MapNfoToJellyfinObject(SeriesNfo? nfo, MetadataResult<Series> metadataResult)
        {
            if (nfo == null)
            {
                throw new ArgumentException("Nfo can't be null", nameof(nfo));
            }

            base.MapNfoToJellyfinObject(nfo, metadataResult);

            if (Enum.TryParse(nfo.Status, true, out SeriesStatus seriesStatus))
            {
                metadataResult.Item.Status = seriesStatus;
            }
            else
            {
                Logger.LogInformation("Unrecognized series status: " + nfo.Status);
            }

            metadataResult.Item.AirDays = TVUtils.GetAirDays(nfo.AirDay);
            metadataResult.Item.AirTime = nfo.AirTime;
        }

        internal static string GetSeriesSavePath(ItemInfo item)
            => Path.Combine(item.Path, "tvshow.nfo");

        /// <inheritdoc/>
        protected override FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService)
            => directoryService.GetFile(Path.Combine(info.Path, "tvshow.nfo"));
    }
}
