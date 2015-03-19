using System;
using System.Collections.Generic;
using System.Linq;

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
        public static bool HasArtist(this IHasArtist hasArtist, string artist)
        {
            return hasArtist.Artists.Contains(artist, StringComparer.OrdinalIgnoreCase);
        }
        public static bool HasAnyArtist(this IHasArtist hasArtist, string artist)
        {
            return hasArtist.AllArtists.Contains(artist, StringComparer.OrdinalIgnoreCase);
        }
    }
}
