using MediaBrowser.Model.Dlna;
using System.Collections.Generic;

namespace MediaBrowser.Model.Session
{
    public class ClientCapabilities
    {
        public List<string> PlayableMediaTypes { get; set; }

        public List<string> SupportedCommands { get; set; }

        public bool SupportsMediaControl { get; set; }

        public string MessageCallbackUrl { get; set; }

        public bool SupportsContentUploading { get; set; }
        public bool SupportsUniqueIdentifier { get; set; }
        public bool SupportsSync { get; set; }

        public DeviceProfile DeviceProfile { get; set; }

        public ClientCapabilities()
        {
            PlayableMediaTypes = new List<string>();
            SupportedCommands = new List<string>();
            SupportsUniqueIdentifier = true;
        }
    }
}