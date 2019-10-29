using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class MusicVideoInfo : ItemLookupInfo
    {
        public IReadOnlyList<string> Artists { get; set; }
    }
}
