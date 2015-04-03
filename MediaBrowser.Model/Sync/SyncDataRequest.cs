using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncDataRequest
    {
        public List<string> LocalItemIds { get; set; }
        public List<string> OfflineUserIds { get; set; }
        public List<string> SyncJobItemIds { get; set; }

        public string TargetId { get; set; }

        public SyncDataRequest()
        {
            LocalItemIds = new List<string>();
            OfflineUserIds = new List<string>();
        }
    }
}
