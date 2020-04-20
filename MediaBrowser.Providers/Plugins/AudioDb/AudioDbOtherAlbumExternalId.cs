using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    /// <summary>
    /// TheAudioDb Album External Id.
    /// </summary>
    public class AudioDbOtherAlbumExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheAudioDb Album";

        /// <inheritdoc />
        public string Key => MetadataProviders.AudioDbAlbum.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://www.theaudiodb.com/album/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio;
    }
}
