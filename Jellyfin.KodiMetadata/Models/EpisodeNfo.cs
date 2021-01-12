using System.Xml.Serialization;
using MediaBrowser.Controller.Serialization;

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
        /// Gets or sets the end episode number for multiple episodes in one file.
        /// </summary>
        [XmlElement("episodenumberend")]
        public int? EpisodeNumberEnd { get; set; }

        /// <summary>
        /// Gets or sets the episode this episode airs before.
        /// </summary>
        [XmlElement("displayepisode")]
        [XmlSynonyms("airsbefore_episode")]
        public int? AirsBeforeEpisode { get; set; }

        /// <summary>
        /// Gets or sets the season this episode airs after.
        /// </summary>
        [XmlElement("airsafter_season")]
        public int? AirsAfterSeason { get; set; }

        /// <summary>
        /// Gets or sets the season this episode airs before.
        /// </summary>
        [XmlElement("displayseason")]
        [XmlSynonyms("airsbefore_season")]
        public int? AirsBeforeSeason { get; set; }

        /// <summary>
        /// Gets or sets the display episode (used for special episodes).
        /// </summary>
        [XmlElement("displayepisode")]
        public int? DisplayEpisode { get; set; }

        /// <summary>
        /// Gets or sets the display season (used for special episodes).
        /// </summary>
        [XmlElement("displayseason")]
        public int? DisplaySeason { get; set; }
    }
}
