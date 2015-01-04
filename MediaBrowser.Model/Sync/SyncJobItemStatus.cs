
namespace MediaBrowser.Model.Sync
{
    public enum SyncJobItemStatus
    {
        Queued = 0,
        Converting = 1,
        Transferring = 2,
        Synced = 3,
        RemovedFromDevice = 4,
        Failed = 5,
        Cancelled = 6
    }
}
