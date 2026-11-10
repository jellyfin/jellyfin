#nullable disable

#pragma warning disable CS1591

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
}
