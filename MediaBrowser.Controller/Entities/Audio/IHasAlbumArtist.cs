using MediaBrowser.Controller.Library;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasAlbumArtist
    {
        List<string> AlbumArtists { get; set; }
    }

    public interface IHasArtist
    {
        List<string> AllArtists { get; }

        List<string> Artists { get; set; }
    }

    public static class HasArtistExtensions
    {
        public static bool HasAnyArtist(this IHasArtist hasArtist, string artist)
        {
            return NameExtensions.EqualsAny(hasArtist.AllArtists, artist);
        }
    }
}
