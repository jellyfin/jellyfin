using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.XbmcMetadata.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Savers
{
    /// <summary>
    /// Nfo saver for episodes.
    /// </summary>
    public class EpisodeNfoSaver : BaseNfoSaver
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeNfoSaver"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="configurationManager">the server configuration manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="userDataManager">The user data manager.</param>
        /// <param name="logger">The logger.</param>
        public EpisodeNfoSaver(
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILogger<EpisodeNfoSaver> logger)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        /// <inheritdoc />
        protected override string GetLocalSavePath(BaseItem item)
            => Path.ChangeExtension(item.Path, ".nfo");

        /// <inheritdoc />
        protected override string GetRootElementName(BaseItem item)
            => "episodedetails";

        /// <inheritdoc />
        public override bool IsEnabledFor(BaseItem item, ItemUpdateType updateType)
            => item.SupportsLocalMetadata && item is Episode && updateType >= MinimumUpdateType;

        /// <inheritdoc />
        protected override void WriteCustomElements(BaseItem item, XmlWriter writer)
        {
            var episode = (Episode)item;

            if (episode.IndexNumber.HasValue)
            {
                writer.WriteElementString("episode", episode.IndexNumber.Value.ToString(_usCulture));
            }

            if (episode.IndexNumberEnd.HasValue)
            {
                writer.WriteElementString("episodenumberend", episode.IndexNumberEnd.Value.ToString(_usCulture));
            }

            if (episode.ParentIndexNumber.HasValue)
            {
                writer.WriteElementString("season", episode.ParentIndexNumber.Value.ToString(_usCulture));
            }

            if (episode.PremiereDate.HasValue)
            {
                var formatString = ConfigurationManager.GetNfoConfiguration().ReleaseDateFormat;

                writer.WriteElementString("aired", episode.PremiereDate.Value.ToLocalTime().ToString(formatString, CultureInfo.InvariantCulture));
            }

            if (!episode.ParentIndexNumber.HasValue || episode.ParentIndexNumber.Value == 0)
            {
                if (episode.AirsAfterSeasonNumber.HasValue && episode.AirsAfterSeasonNumber.Value != -1)
                {
                    writer.WriteElementString("airsafter_season", episode.AirsAfterSeasonNumber.Value.ToString(_usCulture));
                }

                if (episode.AirsBeforeEpisodeNumber.HasValue && episode.AirsBeforeEpisodeNumber.Value != -1)
                {
                    writer.WriteElementString("airsbefore_episode", episode.AirsBeforeEpisodeNumber.Value.ToString(_usCulture));
                }

                if (episode.AirsBeforeSeasonNumber.HasValue && episode.AirsBeforeSeasonNumber.Value != -1)
                {
                    writer.WriteElementString("airsbefore_season", episode.AirsBeforeSeasonNumber.Value.ToString(_usCulture));
                }

                if (episode.AirsBeforeEpisodeNumber.HasValue && episode.AirsBeforeEpisodeNumber.Value != -1)
                {
                    writer.WriteElementString("displayepisode", episode.AirsBeforeEpisodeNumber.Value.ToString(_usCulture));
                }

                var specialSeason = episode.AiredSeasonNumber;
                if (specialSeason.HasValue && specialSeason.Value != -1)
                {
                    writer.WriteElementString("displayseason", specialSeason.Value.ToString(_usCulture));
                }
            }
        }

        /// <inheritdoc />
        protected override List<string> GetTagsUsed(BaseItem item)
        {
            var list = base.GetTagsUsed(item);
            list.AddRange(new string[]
            {
                "aired",
                "season",
                "episode",
                "episodenumberend",
                "airsafter_season",
                "airsbefore_episode",
                "airsbefore_season",
                "displayseason",
                "displayepisode"
            });

            return list;
        }
    }
}
