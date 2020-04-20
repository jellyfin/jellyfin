using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.MusicBrainz;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// MusicBrainz Album External Id.
    /// </summary>
    public class MusicBrainzAlbumExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Album";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzAlbum.ToString();

        /// <inheritdoc />
        public string UrlFormatString => Plugin.Instance.Configuration.Server + "/release/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
    }
}
