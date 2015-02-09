using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncDataResponse
    {
        public List<string> ItemIdsToRemove { get; set; }
        public Dictionary<string, List<string>> ItemUserAccess { get; set; }

        public SyncDataResponse()
        {
            ItemIdsToRemove = new List<string>();
            ItemUserAccess = new Dictionary<string, List<string>>();
        }
    }
}
