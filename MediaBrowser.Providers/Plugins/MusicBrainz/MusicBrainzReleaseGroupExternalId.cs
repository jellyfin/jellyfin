using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.MusicBrainz;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzReleaseGroupExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Release Group";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzReleaseGroup.ToString();

        /// <inheritdoc />
        public string UrlFormatString => Plugin.Instance.Configuration.Server + "/release-group/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
    }
}
