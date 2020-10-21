#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class AudioDbArtistProviderRootObject
    {
        public IEnumerable<AudioDbArtistProvider.Artist> Artists { get; set; }
    }
}
