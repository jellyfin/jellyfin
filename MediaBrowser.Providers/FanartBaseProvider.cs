using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Threading;

namespace MediaBrowser.Providers
{
    /// <summary>
    /// Class FanartBaseProvider
    /// </summary>
    public abstract class FanartBaseProvider : BaseMetadataProvider
    {
        internal static readonly SemaphoreSlim FanArtResourcePool = new SemaphoreSlim(3, 3);

        /// <summary>
        /// The LOG o_ FILE
        /// </summary>
        protected const string LogoFile = "logo.png";

        /// <summary>
        /// The AR t_ FILE
        /// </summary>
        protected const string ArtFile = "clearart.png";

        /// <summary>
        /// The THUM b_ FILE
        /// </summary>
        protected const string ThumbFile = "thumb.jpg";

        /// <summary>
        /// The DIS c_ FILE
        /// </summary>
        protected const string DiscFile = "disc.png";

        /// <summary>
        /// The BANNE r_ FILE
        /// </summary>
        protected const string BannerFile = "banner.png";

        /// <summary>
        /// The Backdrop
        /// </summary>
        protected const string BackdropFile = "backdrop.jpg";

        /// <summary>
        /// The Primary image
        /// </summary>
        protected const string PrimaryFile = "folder.jpg";

        /// <summary>
        /// The API key
        /// </summary>
        internal const string ApiKey = "5c6b04c68e904cfed1e6cbc9a9e683d4";

        protected FanartBaseProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get { return true; }
        }

        #region Result Objects

        protected class FanArtImageInfo
        {
            public string id { get; set; }
            public string url { get; set; }
            public string likes { get; set; }
        }

        protected class FanArtMusicInfo
        {
            public string mbid_id { get; set; }
            public List<FanArtImageInfo> musiclogo { get; set; }
            public List<FanArtImageInfo> artistbackground { get; set; }
            public List<FanArtImageInfo> artistthumb { get; set; }
            public List<FanArtImageInfo> hdmusiclogo { get; set; }
            public List<FanArtImageInfo> musicbanner { get; set; }
        }

        protected class FanArtMusicResult
        {
            public FanArtMusicInfo result { get; set; }
        }

        #endregion

    }

}
