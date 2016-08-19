namespace MediaBrowser.Model.Configuration
{
    public class LibraryOptions
    {
        public bool EnableArchiveMediaFiles { get; set; }
        public bool EnablePhotos { get; set; }

        public LibraryOptions()
        {
            EnablePhotos = true;
        }
    }
}
