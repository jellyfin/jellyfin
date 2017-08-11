
namespace MediaBrowser.Model.Sync
{
    public enum SyncJobItemStatus
    {
        Queued = 0,
        Converting = 1,
        ReadyToTransfer = 2,
        Transferring = 3,
        Synced = 4,
        Failed = 5
    }
}
