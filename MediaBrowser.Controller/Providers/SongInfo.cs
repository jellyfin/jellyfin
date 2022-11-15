#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class SongInfo : ItemLookupInfo
    {
        public SongInfo()
        {
            Artists = Array.Empty<string>();
            AlbumArtists = Array.Empty<string>();
        }

        public IReadOnlyList<string> AlbumArtists { get; set; }

        public string Album { get; set; }

        public IReadOnlyList<string> Artists { get; set; }
    }
}
