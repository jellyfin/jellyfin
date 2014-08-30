
namespace MediaBrowser.Model.FileOrganization
{
    public class TvFileOrganizationOptions
    {
        public bool IsEnabled { get; set; }
        public int MinFileSizeMb { get; set; }
        public string[] LeftOverFileExtensionsToDelete { get; set; }
        public string[] WatchLocations { get; set; }

        public string SeasonFolderPattern { get; set; }

        public string SeasonZeroFolderName { get; set; }

        public string EpisodeNamePattern { get; set; }
        public string MultiEpisodeNamePattern { get; set; }

        public bool OverwriteExistingEpisodes { get; set; }

        public bool DeleteEmptyFolders { get; set; }

        public bool CopyOriginalFile { get; set; }

        public TvFileOrganizationOptions()
        {
            MinFileSizeMb = 50;

            LeftOverFileExtensionsToDelete = new string[] { };

            WatchLocations = new string[] { };

            EpisodeNamePattern = "%sn - %sx%0e - %en.%ext";
            MultiEpisodeNamePattern = "%sn - %sx%0e-x%0ed - %en.%ext";
            SeasonFolderPattern = "Season %s";
            SeasonZeroFolderName = "Season 0";

            CopyOriginalFile = false;
        }
    }
}
