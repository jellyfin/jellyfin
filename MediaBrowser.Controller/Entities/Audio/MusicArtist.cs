
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicArtist
    /// </summary>
    public class MusicArtist : Folder
    {
        public Dictionary<string, string> AlbumCovers { get; set; }

        public override void ClearMetaValues()
        {
            AlbumCovers = null;
            base.ClearMetaValues();
        }
    }
}
