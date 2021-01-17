using System.IO;
using Jellyfin.NfoMetadata.Models;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.NfoMetadata.Providers
{
    /// <summary>
    /// Episode nfo metadata provider.
    /// </summary>
    public class EpisodeNfoProvider : BaseNfoProvider<Episode, EpisodeNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeNfoProvider"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public EpisodeNfoProvider(ILogger<BaseNfoProvider<Episode, EpisodeNfo>> logger, IFileSystem fileSystem, IXmlSerializer xmlSerializer)
            : base(logger, fileSystem, xmlSerializer)
        {
        }

        /// <inheritdoc/>
        public override void MapNfoToJellyfinObject(EpisodeNfo? nfo, MetadataResult<Episode> metadataResult)
        {
            if (nfo == null)
            {
                return;
            }

            base.MapNfoToJellyfinObject(nfo, metadataResult);
            var item = metadataResult.Item;

            item.ParentIndexNumber = nfo.Season;
            item.IndexNumber = nfo.Episode;
            item.AirsBeforeEpisodeNumber = nfo.AirsBeforeEpisode;
            item.AirsAfterSeasonNumber = nfo.AirsAfterSeason;
            item.AirsBeforeEpisodeNumber = nfo.AirsBeforeEpisode;
            item.AirsBeforeSeasonNumber = nfo.AirsBeforeSeason;
        }

        internal static string GetEpisodeSavePath(ItemInfo info)
            => Path.ChangeExtension(info.Path, ".nfo");

        /// <inheritdoc/>
        protected override FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            var path = GetEpisodeSavePath(info);

            return directoryService.GetFile(path);
        }
    }
}
