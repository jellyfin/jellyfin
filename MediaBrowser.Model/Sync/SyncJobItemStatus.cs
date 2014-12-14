
namespace MediaBrowser.Model.Sync
{
    public enum SyncJobItemStatus
    {
        Queued = 0,
        Converting = 1,
        Transferring = 2,
        Completed = 3,
        Failed = 4
    }
}
