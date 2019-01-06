using MediaBrowser.Model.Dlna;
using System;

namespace MediaBrowser.Model.Session
{
    public class ClientCapabilities
    {
        public string[] PlayableMediaTypes { get; set; }

        public string[] SupportedCommands { get; set; }

        public bool SupportsMediaControl { get; set; }
        public bool SupportsContentUploading { get; set; }
        public string MessageCallbackUrl { get; set; }

        public bool SupportsPersistentIdentifier { get; set; }
        public bool SupportsSync { get; set; }

        public DeviceProfile DeviceProfile { get; set; }

        public string AppStoreUrl { get; set; }
        public string IconUrl { get; set; }

        public ClientCapabilities()
        {
            PlayableMediaTypes = Array.Empty<string>();
            SupportedCommands = Array.Empty<string>();
            SupportsPersistentIdentifier = true;
        }
    }
}
