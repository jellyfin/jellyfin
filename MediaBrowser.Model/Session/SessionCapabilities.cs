using System.Collections.Generic;

namespace MediaBrowser.Model.Session
{
    public class SessionCapabilities
    {
        public List<string> PlayableMediaTypes { get; set; }

        public List<string> SupportedCommands { get; set; }

        public SessionCapabilities()
        {
            PlayableMediaTypes = new List<string>();
            SupportedCommands = new List<string>();
        }
    }
}
