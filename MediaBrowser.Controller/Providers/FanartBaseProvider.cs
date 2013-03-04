using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using System;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Class FanartBaseProvider
    /// </summary>
    public abstract class FanartBaseProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The LOG o_ FILE
        /// </summary>
        protected const string LOGO_FILE = "logo.png";

        /// <summary>
        /// The AR t_ FILE
        /// </summary>
        protected const string ART_FILE = "clearart.png";

        /// <summary>
        /// The THUM b_ FILE
        /// </summary>
        protected const string THUMB_FILE = "thumb.jpg";

        /// <summary>
        /// The DIS c_ FILE
        /// </summary>
        protected const string DISC_FILE = "disc.png";

        /// <summary>
        /// The BANNE r_ FILE
        /// </summary>
        protected const string BANNER_FILE = "banner.png";

        /// <summary>
        /// The Backdrop
        /// </summary>
        protected const string BACKDROP_FILE = "backdrop.jpg";

        /// <summary>
        /// The Primary image
        /// </summary>
        protected const string PRIMARY_FILE = "folder.jpg";

        /// <summary>
        /// The API key
        /// </summary>
        protected const string APIKey = "5c6b04c68e904cfed1e6cbc9a9e683d4";

        protected FanartBaseProvider(ILogManager logManager)
            : base(logManager)
        {
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.DontFetchMeta) return false;

            return DateTime.UtcNow > (providerInfo.LastRefreshed.AddDays(Kernel.Instance.Configuration.MetadataRefreshDays))
                   && ShouldFetch(item, providerInfo);
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }

        /// <summary>
        /// Shoulds the fetch.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected virtual bool ShouldFetch(BaseItem item, BaseProviderInfo providerInfo)
        {
            return false;
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
