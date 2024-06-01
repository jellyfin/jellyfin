#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class AudioBookFolderInfo : ItemLookupInfo
    {
        public AudioBookFolderInfo()
        {
            Authors = Array.Empty<string>();
        }

        public string BookTitle { get; set; }

        public string Container { get; set; }

        public int Chapters { get; set; }

        public IReadOnlyList<string> Authors { get; set; }
    }
}
