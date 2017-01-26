using MediaBrowser.Model.Dlna;
using System.Collections.Generic;

namespace MediaBrowser.Model.Session
{
    public class ClientCapabilities
    {
        public List<string> PlayableMediaTypes { get; set; }

        public List<string> SupportedCommands { get; set; }

        public bool SupportsMediaControl { get; set; }
        public bool SupportsContentUploading { get; set; }
        public string MessageCallbackUrl { get; set; }

        public bool SupportsPersistentIdentifier { get; set; }
        public bool SupportsSync { get; set; }

        public DeviceProfile DeviceProfile { get; set; }
        public List<string> SupportedLiveMediaTypes { get; set; }

        public string AppStoreUrl { get; set; }
        public string IconUrl { get; set; }

        public ClientCapabilities()
        {
            PlayableMediaTypes = new List<string>();
            SupportedCommands = new List<string>();
            SupportsPersistentIdentifier = true;
            SupportedLiveMediaTypes = new List<string>();
        }
    }
}