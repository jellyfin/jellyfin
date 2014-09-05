using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasAlbumArtist
    {
        List<string> AlbumArtists { get; set; }
    }

    public interface IHasArtist
    {
        bool HasArtist(string name);

        List<string> AllArtists { get; }

        List<string> Artists { get; }
    }
}
