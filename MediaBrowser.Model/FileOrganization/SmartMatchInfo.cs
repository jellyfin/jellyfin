
namespace MediaBrowser.Model.FileOrganization
{
    public class SmartMatchInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public FileOrganizerType OrganizerType { get; set; }
        public string[] MatchStrings { get; set; }

        public SmartMatchInfo()
        {
            MatchStrings = new string[] { };
        }
    }
}
