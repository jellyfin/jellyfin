namespace MediaBrowser.Model.Configuration
{
    public class ChannelOptions
    {
        public int? PreferredStreamingWidth { get; set; }

        public string DownloadPath { get; set; }
        public int? MaxDownloadAge { get; set; }

        public string[] DownloadingChannels { get; set; }

        public double? DownloadSizeLimit { get; set; }

        public ChannelOptions()
        {
            DownloadingChannels = new string[] { };
            DownloadSizeLimit = .5;
            MaxDownloadAge = 30;
        }
    }
}