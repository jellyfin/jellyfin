using MediaBrowser.Model.Sync;

namespace MediaBrowser.Controller.Sync
{
    public class SyncedItemProgress
    {
        public string ItemId { get; set; }
        public SyncJobItemStatus Status { get; set; }
    }
}
