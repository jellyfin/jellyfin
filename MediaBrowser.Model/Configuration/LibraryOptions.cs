namespace MediaBrowser.Model.Configuration
{
    public class LibraryOptions
    {
        public bool EnableArchiveMediaFiles { get; set; }
        public bool EnablePhotos { get; set; }
        public bool EnableRealtimeMonitor { get; set; }
        public int SchemaVersion { get; set; }
        public bool EnableChapterImageExtraction { get; set; }
        public bool ExtractChapterImagesDuringLibraryScan { get; set; }

        public LibraryOptions()
        {
            EnablePhotos = true;
            EnableRealtimeMonitor = true;
        }
    }
}
