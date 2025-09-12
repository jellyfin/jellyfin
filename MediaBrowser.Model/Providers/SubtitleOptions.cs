#nullable disable

#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Providers
{
    public class SubtitleOptions
    {
        public SubtitleOptions()
        {
            DownloadLanguages = [];

            SkipIfAudioTrackMatches = true;
            RequirePerfectMatch = true;
        }

        public bool SkipIfEmbeddedSubtitlesPresent { get; set; }

        public bool SkipIfAudioTrackMatches { get; set; }

        public string[] DownloadLanguages { get; set; }

        public bool DownloadMovieSubtitles { get; set; }

        public bool DownloadEpisodeSubtitles { get; set; }

        public string OpenSubtitlesUsername { get; set; }

        public string OpenSubtitlesPasswordHash { get; set; }

        public bool IsOpenSubtitleVipAccount { get; set; }

        public bool RequirePerfectMatch { get; set; }
    }
}
