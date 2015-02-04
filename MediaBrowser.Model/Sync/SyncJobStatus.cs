
namespace MediaBrowser.Model.Sync
{
    public enum SyncJobStatus
    {
        Queued = 0,
        Converting = 1,
        Transferring = 2,
        Completed = 3,
        CompletedWithError = 4,
        Failed = 5,
        Cancelled = 6
    }
}
