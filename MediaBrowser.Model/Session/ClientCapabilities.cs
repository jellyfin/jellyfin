#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Model.Session
{
    public class ClientCapabilities
    {
        public ClientCapabilities()
        {
            PlayableMediaTypes = Array.Empty<string>();
            SupportedCommands = Array.Empty<GeneralCommandType>();
            SupportsPersistentIdentifier = true;
        }

        public string[] PlayableMediaTypes { get; set; }

        public GeneralCommandType[] SupportedCommands { get; set; }

        public bool SupportsMediaControl { get; set; }

        public bool SupportsContentUploading { get; set; }

        public string MessageCallbackUrl { get; set; }

        public bool SupportsPersistentIdentifier { get; set; }

        public bool SupportsSync { get; set; }

        public DeviceProfile DeviceProfile { get; set; }

        public string AppStoreUrl { get; set; }

        public string IconUrl { get; set; }
    }
}
