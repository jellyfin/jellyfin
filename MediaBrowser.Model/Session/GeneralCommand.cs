using System.Collections.Generic;

namespace MediaBrowser.Model.Session
{
    public class GeneralCommand
    {
        public string Name { get; set; }

        public string ControllingUserId { get; set; }

        public Dictionary<string, string> Arguments { get; set; }

        public GeneralCommand()
        {
            Arguments = new Dictionary<string, string>();
        }
    }
}
