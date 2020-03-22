using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public interface IExternalId
    {
        string Name { get; }

        string Key { get; }

        ExternalIdType Type { get; }

        string UrlFormatString { get; }

        bool Supports(IHasProviderIds item);
    }

    public enum ExternalIdType
    {
        None,
        Album,
        AlbumArtist,
        Artist,
        BoxSet,
        Episode,
        Movie,
        OtherArtist,
        Person,
        ReleaseGroup,
        Season,
        Series,
        Track
    }
}
