using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Controller.Providers.Music
{
    /// <summary>
    /// Class FanArtArtistByNameProvider
    /// </summary>
    public class FanArtArtistByNameProvider : FanArtArtistProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FanArtArtistByNameProvider" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public FanArtArtistByNameProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(httpClient, logManager, configurationManager, providerManager)
        {
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Artist;
        }

        /// <summary>
        /// Gets a value indicating whether [save local meta].
        /// </summary>
        /// <value><c>true</c> if [save local meta]; otherwise, <c>false</c>.</value>
        protected override bool SaveLocalMeta
        {
            get
            {
                return true;
            }
        }
    }
}
