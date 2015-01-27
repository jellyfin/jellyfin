using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncJobCreationResult
    {
        public SyncJob Job { get; set; }
        public List<SyncJobItem> JobItems { get; set; }

        public SyncJobCreationResult()
        {
            JobItems = new List<SyncJobItem>();
        }
    }
}
