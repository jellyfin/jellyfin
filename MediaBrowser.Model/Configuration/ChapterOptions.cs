namespace MediaBrowser.Model.Configuration
{
    public class ChapterOptions
    {
        public bool DownloadMovieChapters { get; set; }
        public bool DownloadEpisodeChapters { get; set; }

        public string[] FetcherOrder { get; set; }
        public string[] DisabledFetchers { get; set; }
      
        public ChapterOptions()
        {
            DownloadMovieChapters = true;

            DisabledFetchers = new string[] { };
            FetcherOrder = new string[] { };
        }
    }
}