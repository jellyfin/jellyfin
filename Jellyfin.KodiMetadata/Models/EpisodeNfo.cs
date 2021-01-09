using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The episode specific nfo tags.
    /// </summary>
    [XmlRoot("episodedetails")]
    public class EpisodeNfo : BaseNfo
    {
        /// <summary>
        /// Gets or sets the season.
        /// </summary>
        [XmlElement("season")]
        public int? Season { get; set; }

        /// <summary>
        /// Gets or sets the episode.
        /// </summary>
        [XmlElement("episode")]
        public int? Episode { get; set; }

        /// <summary>
        /// Gets or sets the episode number end.
        /// </summary>
        [XmlElement("episodenumberend")]
        public int? EpisodeNumberEnd { get; set; }

        /// <summary>
        /// Gets or sets the episode this episode airs before.
        /// </summary>
        [XmlElement("airsbefore_episode")]
        [XmlElement("displayepisode")]
        public int? AirsBeforeEpisode { get; set; }

        /// <summary>
        /// Gets or sets the season this episode airs after.
        /// </summary>
        [XmlElement("airsafter_season")]
        public int? AirsAfterSeason { get; set; }

        /// <summary>
        /// Gets or sets the season this episode airs before.
        /// </summary>
        [XmlElement("airsbefore_season")]
        [XmlElement("displayseason")]
        public int? AirsBeforeSeason { get; set; }
    }
}
