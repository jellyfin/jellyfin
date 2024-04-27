#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class AudioBookFileInfo : ItemLookupInfo
    {
        public AudioBookFileInfo()
        {
            Authors = Array.Empty<string>();
        }

        public string BookTitle { get; set; }

        public int Chapter { get; set; }

        public string Container { get; set; }

        public IReadOnlyList<string> Authors { get; set; }
    }
}
