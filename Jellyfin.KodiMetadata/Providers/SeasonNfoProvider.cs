using System.IO;
using Jellyfin.KodiMetadata.Models;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Providers
{
    /// <summary>
    /// Season nfo metadata provider.
    /// </summary>
    public class SeasonNfoProvider : BaseNfoProvider<Season, SeasonNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeasonNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public SeasonNfoProvider(ILogger<BaseNfoProvider<Season, SeasonNfo>> logger, IFileSystem fileSystem, IXmlSerializer xmlSerializer)
            : base(logger, fileSystem, xmlSerializer)
        {
        }

        /// <inheritdoc/>
        public override void MapNfoToJellyfinObject(SeasonNfo? nfo, MetadataResult<Season> metadataResult)
        {
            if (nfo == null)
            {
                return;
            }

            base.MapNfoToJellyfinObject(nfo, metadataResult);
            metadataResult.Item.IndexNumber = nfo.SeasonNumber;
        }

        /// <inheritdoc/>
        protected override FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService)
            => directoryService.GetFile(Path.Combine(info.Path, "season.nfo"));
    }
}
