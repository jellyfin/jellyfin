
namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasAlbumArtist
    {
        string[] AlbumArtists { get; set; }
    }

    public interface IHasArtist
    {
        string[] AllArtists { get; }

        string[] Artists { get; set; }
    }
}
