using System;
using System.Linq;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicArtist
    /// </summary>
    public class MusicArtist : Folder
    {
        public override List<string> Genres
        {
            get
            {
                return Children
                    .OfType<MusicAlbum>()
                    .SelectMany(i => i.Genres)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            set
            {
                base.Genres = value;
            }
        }
    }
}
