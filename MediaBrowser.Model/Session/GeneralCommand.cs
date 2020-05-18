#pragma warning disable CS1591
#pragma warning disable CA2227 // Collection properties should be read only

using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Session
{
    public class GeneralCommand
    {
        public GeneralCommand()
        {
            Arguments = new Dictionary<string, string>();
        }

        public string Name { get; set; }

        public Guid ControllingUserId { get; set; }

        public Dictionary<string, string> Arguments { get; set; }
    }
}
