
namespace MediaBrowser.Model.Sync
{
    public enum SyncJobStatus
    {
        Queued = 0,
        InProgress = 1,
        Completed = 2,
        CompletedWithError = 3
    }
}
