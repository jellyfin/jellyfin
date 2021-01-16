using System;
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
    public class EpisodeNfoSaver : BaseNfoSaver<Episode, EpisodeNfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeNfoSaver"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        public EpisodeNfoSaver(
            ILogger<BaseNfoSaver<Episode, EpisodeNfo>> logger,
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager)
            : base(logger, xmlSerializer, fileSystem, configurationManager, libraryManager, userManager, userDataManager)
        {
        }

        /// <inheritdoc />
        public override string GetSavePath(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            return EpisodeNfoProvider.GetEpisodeSavePath(new ItemInfo(item));
        }

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
            => item.SupportsLocalMetadata && item is Episode && updateType >= MinimumUpdateType;

        /// <inheritdoc />
        protected override void MapJellyfinToNfoObject(Episode? item, EpisodeNfo nfo)
        {
            if (item == null)
            {
                throw new ArgumentException("Item can't be null", nameof(item));
            }

            if (nfo == null)
            {
                throw new ArgumentException("Nfo object can't be null", nameof(nfo));
            }

            var episode = item;

            nfo.Episode = episode.IndexNumber;
            nfo.EpisodeNumberEnd = episode.IndexNumberEnd;
            nfo.Season = episode.ParentIndexNumber;

            nfo.Aired = episode.PremiereDate;

            if (!episode.ParentIndexNumber.HasValue || episode.ParentIndexNumber.Value == 0)
            {
                nfo.AirsAfterSeason = episode.AirsAfterSeasonNumber;
                nfo.AirsBeforeEpisode = episode.AirsBeforeEpisodeNumber;
                nfo.AirsBeforeSeason = episode.AirsBeforeSeasonNumber;
                if (episode.AiredSeasonNumber != 1)
                {
                    nfo.DisplaySeason = episode.AiredSeasonNumber;
                }
            }

            base.MapJellyfinToNfoObject(item, nfo);
        }
    }
}
