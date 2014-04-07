using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasAlbumArtist
    {
        string AlbumArtist { get; set; }
    }

    public interface IHasArtist
    {
        bool HasArtist(string name);

        List<string> AllArtists { get; }
    }
}
