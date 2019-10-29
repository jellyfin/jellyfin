using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasAlbumArtist
    {
        IReadOnlyList<string> AlbumArtists { get; set; }
    }

    public interface IHasArtist
    {
        /// <summary>
        /// Gets or sets the artists.
        /// </summary>
        /// <value>The artists.</value>
        IReadOnlyList<string> Artists { get; set; }
    }

    public static class Extentions
    {
        public static IEnumerable<string> GetAllArtists<T>(this T item)
            where T : IHasArtist, IHasAlbumArtist
        {
            foreach (var i in item.AlbumArtists)
            {
                yield return i;
            }

            foreach (var i in item.Artists)
            {
                yield return i;
            }
        }
    }
}
