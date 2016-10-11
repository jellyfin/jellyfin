namespace MediaBrowser.Model.Configuration
{
    public class ChapterOptions
    {
        public bool EnableMovieChapterImageExtraction { get; set; }
        public bool EnableEpisodeChapterImageExtraction { get; set; }
        public bool EnableOtherVideoChapterImageExtraction { get; set; }

        public bool ExtractDuringLibraryScan { get; set; }
    }
}