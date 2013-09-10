
namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasAlbumArtist
    {
        string AlbumArtist { get; }
    }

    public interface IHasArtist
    {
        bool HasArtist(string name);
    }
}
