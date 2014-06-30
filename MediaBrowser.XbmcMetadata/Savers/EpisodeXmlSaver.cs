using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.XbmcMetadata.Configuration;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class EpisodeXmlSaver : IMetadataFileSaver
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepo;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;

        public EpisodeXmlSaver(ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataRepo = userDataRepo;
            _fileSystem = fileSystem;
            _config = config;
        }

        public string Name
        {
            get
            {
                return "Xbmc Nfo";
            }
        }

        public string GetSavePath(IHasMetadata item)
        {
            return Path.ChangeExtension(item.Path, ".nfo");
        }

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var episode = (Episode)item;

            var builder = new StringBuilder();

            builder.Append("<episodedetails>");

            XmlSaverHelpers.AddCommonNodes(episode, builder, _libraryManager, _userManager, _userDataRepo, _fileSystem, _config);

            if (episode.IndexNumber.HasValue)
            {
                builder.Append("<episode>" + episode.IndexNumber.Value.ToString(_usCulture) + "</episode>");
            }

            if (episode.IndexNumberEnd.HasValue)
            {
                builder.Append("<episodenumberend>" + SecurityElement.Escape(episode.IndexNumberEnd.Value.ToString(_usCulture)) + "</episodenumberend>");
            }
            
            if (episode.ParentIndexNumber.HasValue)
            {
                builder.Append("<season>" + episode.ParentIndexNumber.Value.ToString(_usCulture) + "</season>");
            }

            if (episode.PremiereDate.HasValue)
            {
                var formatString = _config.GetNfoConfiguration().ReleaseDateFormat;

                builder.Append("<aired>" + SecurityElement.Escape(episode.PremiereDate.Value.ToString(formatString)) + "</aired>");
            }

            if (episode.AirsAfterSeasonNumber.HasValue)
            {
                builder.Append("<airsafter_season>" + SecurityElement.Escape(episode.AirsAfterSeasonNumber.Value.ToString(_usCulture)) + "</airsafter_season>");
            }
            if (episode.AirsBeforeEpisodeNumber.HasValue)
            {
                builder.Append("<airsbefore_episode>" + SecurityElement.Escape(episode.AirsBeforeEpisodeNumber.Value.ToString(_usCulture)) + "</airsbefore_episode>");
            }
            if (episode.AirsBeforeSeasonNumber.HasValue)
            {
                builder.Append("<airsbefore_season>" + SecurityElement.Escape(episode.AirsBeforeSeasonNumber.Value.ToString(_usCulture)) + "</airsbefore_season>");
            }

            if (episode.DvdEpisodeNumber.HasValue)
            {
                builder.Append("<DVD_episodenumber>" + SecurityElement.Escape(episode.DvdEpisodeNumber.Value.ToString(_usCulture)) + "</DVD_episodenumber>");
            }

            if (episode.DvdSeasonNumber.HasValue)
            {
                builder.Append("<DVD_season>" + SecurityElement.Escape(episode.DvdSeasonNumber.Value.ToString(_usCulture)) + "</DVD_season>");
            }

            if (episode.AbsoluteEpisodeNumber.HasValue)
            {
                builder.Append("<absolute_number>" + SecurityElement.Escape(episode.AbsoluteEpisodeNumber.Value.ToString(_usCulture)) + "</absolute_number>");
            }
            
            XmlSaverHelpers.AddMediaInfo((Episode)item, builder);

            builder.Append("</episodedetails>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
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
                    "absolute_number"
                });
        }

        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            var locationType = item.LocationType;
            if (locationType == LocationType.Remote || locationType == LocationType.Virtual)
            {
                return false;
            }

            // If new metadata has been downloaded or metadata was manually edited, proceed
            if ((updateType & ItemUpdateType.MetadataDownload) == ItemUpdateType.MetadataDownload
                || (updateType & ItemUpdateType.MetadataEdit) == ItemUpdateType.MetadataEdit)
            {
                return item is Episode;
            }

            return false;
        }
    }
}
