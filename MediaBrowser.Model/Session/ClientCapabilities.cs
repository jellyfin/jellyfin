using System.Collections.Generic;

namespace MediaBrowser.Model.Session
{
    public class ClientCapabilities
    {
        public List<string> PlayableMediaTypes { get; set; }
        public List<string> SupportedCommands { get; set; }

        public ClientCapabilities()
        {
            PlayableMediaTypes = new List<string>();
            SupportedCommands = new List<string>();
        }
    }
}