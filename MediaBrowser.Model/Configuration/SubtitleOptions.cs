namespace MediaBrowser.Model.Configuration
{
    public class SubtitleOptions
    {
        public bool SkipIfGraphicalSubtitlesPresent { get; set; }
        public bool SkipIfAudioTrackMatches { get; set; }
        public string[] DownloadLanguages { get; set; }
        public bool DownloadMovieSubtitles { get; set; }
        public bool DownloadEpisodeSubtitles { get; set; }

        public string OpenSubtitlesUsername { get; set; }
        public string OpenSubtitlesPasswordHash { get; set; }
        public bool IsOpenSubtitleVipAccount { get; set; }

        public SubtitleOptions()
        {
            DownloadLanguages = new string[] { };

            SkipIfAudioTrackMatches = true;
        }
    }
}