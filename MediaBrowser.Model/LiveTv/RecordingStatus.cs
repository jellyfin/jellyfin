#pragma warning disable CS1591

namespace MediaBrowser.Model.LiveTv
{
    public enum RecordingStatus
    {
        New,
        InProgress,
        Completed,
        Cancelled,
        ConflictedOk,
        ConflictedNotOk,
        Error
    }
}
