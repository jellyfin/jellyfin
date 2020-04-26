using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.MusicBrainz;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// MusicBrainz Track External Id.
    /// </summary>
    public class MusicBrainzTrackId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "MusicBrainz Track";

        /// <inheritdoc />
        public string Key => MetadataProviders.MusicBrainzTrack.ToString();

        /// <inheritdoc />
        public string UrlFormatString => Plugin.Instance.Configuration.Server + "/track/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio;
    }
}
