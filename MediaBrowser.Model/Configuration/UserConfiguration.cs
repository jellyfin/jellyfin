#pragma warning disable CS1591

using System;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Class UserConfiguration.
    /// </summary>
    public class UserConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserConfiguration" /> class.
        /// </summary>
        public UserConfiguration()
        {
            EnableNextEpisodeAutoPlay = true;
            RememberAudioSelections = true;
            RememberSubtitleSelections = true;

            HidePlayedInLatest = true;
            PlayDefaultAudioTrack = true;

            LatestItemsExcludes = Array.Empty<Guid>();
            OrderedViews = Array.Empty<Guid>();
            MyMediaExcludes = Array.Empty<Guid>();
            GroupedFolders = Array.Empty<Guid>();
        }

        /// <summary>
        /// Gets or sets the audio language preference.
        /// </summary>
        /// <value>The audio language preference.</value>
        public string? AudioLanguagePreference { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [play default audio track].
        /// </summary>
        /// <value><c>true</c> if [play default audio track]; otherwise, <c>false</c>.</value>
        public bool PlayDefaultAudioTrack { get; set; }

        /// <summary>
        /// Gets or sets the subtitle language preference.
        /// </summary>
        /// <value>The subtitle language preference.</value>
        public string? SubtitleLanguagePreference { get; set; }

        /// <summary>
        /// Gets or sets the subtitle codec preference.
        /// </summary>
        /// <value>The subtitle codec preference.</value>
        public string? SubtitleCodecPreference { get; set; }

        public bool DisplayMissingEpisodes { get; set; }

        public Guid[] GroupedFolders { get; set; }

        public SubtitlePlaybackMode SubtitleMode { get; set; }

        public bool DisplayCollectionsView { get; set; }

        public bool EnableLocalPassword { get; set; }

        public Guid[] OrderedViews { get; set; }

        public Guid[] LatestItemsExcludes { get; set; }

        public Guid[] MyMediaExcludes { get; set; }

        public bool HidePlayedInLatest { get; set; }

        public bool RememberAudioSelections { get; set; }

        public bool RememberSubtitleSelections { get; set; }

        public bool EnableNextEpisodeAutoPlay { get; set; }

        /// <summary>
        /// Gets or sets the id of the selected cast receiver.
        /// </summary>
        public string? CastReceiverId { get; set; }
    }
}
