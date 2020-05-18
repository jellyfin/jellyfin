#pragma warning disable CS1591
#pragma warning disable SA1602 // Enumeration items should be documented

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
