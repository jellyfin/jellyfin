using MediaBrowser.Model.Sync;

namespace MediaBrowser.Controller.Sync
{
    public class SyncedItemProgress
    {
        public double Progress { get; set; }
        public SyncJobItemStatus Status { get; set; }
    }
}
