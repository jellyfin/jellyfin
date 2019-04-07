using System;

namespace MediaBrowser.Model.Providers
{
    public class SubtitleOptions
    {
        public string[] DownloadLanguages { get; set; }
        public bool SkipIfEmbeddedSubtitlesPresent { get; set; }
        public bool SkipIfAudioTrackMatches { get; set; }
        public bool DownloadMovieSubtitles { get; set; }
        public bool DownloadEpisodeSubtitles { get; set; }
        public bool RequirePerfectMatch { get; set; }

        public SubtitleOptions()
        {
            DownloadLanguages = Array.Empty<string>();
            SkipIfAudioTrackMatches = true;
            RequirePerfectMatch = true;
        }
    }
}
