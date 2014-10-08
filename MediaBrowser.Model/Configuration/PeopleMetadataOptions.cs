namespace MediaBrowser.Model.Configuration
{
    public class PeopleMetadataOptions
    {
        public bool DownloadActorMetadata { get; set; }
        public bool DownloadDirectorMetadata { get; set; }
        public bool DownloadProducerMetadata { get; set; }
        public bool DownloadWriterMetadata { get; set; }
        public bool DownloadComposerMetadata { get; set; }
        public bool DownloadOtherPeopleMetadata { get; set; }
        public bool DownloadGuestStarMetadata { get; set; }

        public PeopleMetadataOptions()
        {
            DownloadActorMetadata = true;
            DownloadDirectorMetadata = true;
        }
    }
}