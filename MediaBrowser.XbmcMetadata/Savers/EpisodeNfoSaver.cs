using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using CommonIO;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class EpisodeNfoSaver : BaseNfoSaver
    {
        public EpisodeNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        protected override string GetLocalSavePath(IHasMetadata item)
        {
            return Path.ChangeExtension(item.Path, ".nfo");
        }

        protected override string GetRootElementName(IHasMetadata item)
        {
            return "episodedetails";
        }

        public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is Episode && updateType >= MinimumUpdateType;
        }

        protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
        {
            var episode = (Episode)item;

            if (episode.IndexNumber.HasValue)
            {
                writer.WriteElementString("episode", episode.IndexNumber.Value.ToString(UsCulture));
            }

            if (episode.IndexNumberEnd.HasValue)
            {
                writer.WriteElementString("episodenumberend", episode.IndexNumberEnd.Value.ToString(UsCulture));
            }

            if (episode.ParentIndexNumber.HasValue)
            {
                writer.WriteElementString("season", episode.ParentIndexNumber.Value.ToString(UsCulture));
            }

            if (episode.PremiereDate.HasValue)
            {
                var formatString = ConfigurationManager.GetNfoConfiguration().ReleaseDateFormat;

                writer.WriteElementString("aired", episode.PremiereDate.Value.ToLocalTime().ToString(formatString));
            }

            if (episode.AirsAfterSeasonNumber.HasValue && episode.AirsAfterSeasonNumber.Value != -1)
            {
                writer.WriteElementString("airsafter_season", episode.AirsAfterSeasonNumber.Value.ToString(UsCulture));
            }
            if (episode.AirsBeforeEpisodeNumber.HasValue && episode.AirsBeforeEpisodeNumber.Value != -1)
            {
                writer.WriteElementString("airsbefore_episode", episode.AirsBeforeEpisodeNumber.Value.ToString(UsCulture));
            }
            if (episode.AirsBeforeSeasonNumber.HasValue && episode.AirsBeforeSeasonNumber.Value != -1)
            {
                writer.WriteElementString("airsbefore_season", episode.AirsBeforeSeasonNumber.Value.ToString(UsCulture));
            }

            if (episode.ParentIndexNumber.HasValue && episode.ParentIndexNumber.Value == 0)
            {
                if (episode.AirsBeforeEpisodeNumber.HasValue && episode.AirsBeforeEpisodeNumber.Value != -1)
                {
                    writer.WriteElementString("displayepisode", episode.AirsBeforeEpisodeNumber.Value.ToString(UsCulture));
                }

                var specialSeason = episode.AiredSeasonNumber;
                if (specialSeason.HasValue && specialSeason.Value != -1)
                {
                    writer.WriteElementString("displayseason", specialSeason.Value.ToString(UsCulture));
                }
            }

            if (episode.DvdEpisodeNumber.HasValue)
            {
                writer.WriteElementString("DVD_episodenumber", episode.DvdEpisodeNumber.Value.ToString(UsCulture));
            }

            if (episode.DvdSeasonNumber.HasValue)
            {
                writer.WriteElementString("DVD_season", episode.DvdSeasonNumber.Value.ToString(UsCulture));
            }

            if (episode.AbsoluteEpisodeNumber.HasValue)
            {
                writer.WriteElementString("absolute_number", episode.AbsoluteEpisodeNumber.Value.ToString(UsCulture));
            }
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        protected override List<string> GetTagsUsed()
        {
            var list = new List<string>
            {
                    "aired",
                    "season",
                    "episode",
                    "episodenumberend",
                    "airsafter_season",
                    "airsbefore_episode",
                    "airsbefore_season",
                    "DVD_episodenumber",
                    "DVD_season",
                    "absolute_number",
                    "displayseason",
                    "displayepisode"
            };

            return list;
        }
    }
}
