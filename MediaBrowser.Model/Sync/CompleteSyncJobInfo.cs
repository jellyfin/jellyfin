using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class CompleteSyncJobInfo
    {
        public SyncJob Job { get; set; }
        public List<SyncJobItem> JobItems { get; set; }

        public CompleteSyncJobInfo()
        {
            JobItems = new List<SyncJobItem>();
        }
    }
}
