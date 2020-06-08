#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    public class XbmcMetadataOptions
    {
        public string UserId { get; set; }

        public string ReleaseDateFormat { get; set; }

        public bool SaveImagePathsInNfo { get; set; }
        public bool EnablePathSubstitution { get; set; }

        public bool EnableExtraThumbsDuplication { get; set; }

        public XbmcMetadataOptions()
        {
            ReleaseDateFormat = "yyyy-MM-dd";

            SaveImagePathsInNfo = true;
            EnablePathSubstitution = true;
        }
    }
}
