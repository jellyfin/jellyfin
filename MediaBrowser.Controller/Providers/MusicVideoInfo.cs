#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class MusicVideoInfo : ItemLookupInfo
    {
        public IReadOnlyList<string> Artists { get; set; }
    }
}
