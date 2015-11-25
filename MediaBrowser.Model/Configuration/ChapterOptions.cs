namespace MediaBrowser.Model.Configuration
{
    public class ChapterOptions
    {
        public bool EnableMovieChapterImageExtraction { get; set; }
        public bool EnableEpisodeChapterImageExtraction { get; set; }
        public bool EnableOtherVideoChapterImageExtraction { get; set; }

        public bool DownloadMovieChapters { get; set; }
        public bool DownloadEpisodeChapters { get; set; }

        public string[] FetcherOrder { get; set; }
        public string[] DisabledFetchers { get; set; }

        public bool ExtractDuringLibraryScan { get; set; }
      
        public ChapterOptions()
        {
            DownloadMovieChapters = true;

            DisabledFetchers = new string[] { };
            FetcherOrder = new string[] { };
        }
    }
}