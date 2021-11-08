#pragma warning disable CS1591

using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.MusicBrainz;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzOtherArtistExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "MusicBrainz";

        /// <inheritdoc />
        public string Key => MetadataProvider.MusicBrainzArtist.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.OtherArtist;

        /// <inheritdoc />
        public string? UrlFormatString => Plugin.Instance.Configuration.Server + "/artist/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
    }
}
