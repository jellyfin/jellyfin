using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Providers.Music
{
    /// <summary>
    /// Class LastfmArtistByNameProvider
    /// </summary>
    public class LastfmArtistByNameProvider : LastfmArtistProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LastfmArtistByNameProvider"/> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public LastfmArtistByNameProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager, providerManager)
        {
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

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Artist;
        }
    }
}
