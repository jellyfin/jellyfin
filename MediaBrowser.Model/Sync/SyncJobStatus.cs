
namespace MediaBrowser.Model.Sync
{
    public enum SyncJobStatus
    {
        Queued = 0,
        Transcoding = 1,
        TranscodingFailed = 2,
        Transferring = 3,
        Completed = 4,
        Cancelled = 5
    }
}
