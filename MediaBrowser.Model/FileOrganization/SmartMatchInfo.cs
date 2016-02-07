
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.FileOrganization
{
    public class SmartMatchInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public FileOrganizerType OrganizerType { get; set; }
        public List<string> MatchStrings { get; set; }

        public SmartMatchInfo()
        {
            MatchStrings = new List<string>();
        }
    }
}
