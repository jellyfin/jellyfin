
namespace MediaBrowser.Model.LiveTv
{
    public enum RecordingStatus
    {
        New,
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
        ConflictedOk,
        ConflictedNotOk,
        Error
    }
}
