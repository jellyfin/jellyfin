
namespace MediaBrowser.Model.Sync
{
    public enum SyncJobStatus
    {
        Queued = 0,
        Converting = 1,
        Transferring = 2,
        Completed = 3,
        Cancelled = 4
    }
}
