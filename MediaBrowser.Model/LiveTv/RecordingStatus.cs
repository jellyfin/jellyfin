
namespace MediaBrowser.Model.LiveTv
{
    public enum RecordingStatus
    {
        New,
        Scheduled,
        InProgress,
        Completed,
        Aborted,
        Cancelled,
        ConflictedOk,
        ConflictedNotOk,
        Error
    }
}
