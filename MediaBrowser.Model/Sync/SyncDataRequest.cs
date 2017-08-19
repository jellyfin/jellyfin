using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncDataRequest
    {
        public string[] LocalItemIds { get; set; }
        public string[] OfflineUserIds { get; set; }
        public string[] SyncJobItemIds { get; set; }

        public string TargetId { get; set; }

        public SyncDataRequest()
        {
            LocalItemIds = new string[] { };
            OfflineUserIds = new string[] { };
        }
    }
}
