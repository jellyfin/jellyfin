
namespace MediaBrowser.Model.LiveTv
{
    public enum RecordingStatus
    {
        New,
        Scheduled,
        InProgress,
        Completed,
        Abored,
        Cancelled,
        ConflictedOk,
        ConflictedNotOk,
        Error
    }

    public enum RecurrenceType
    {
        Manual,
        NewProgramEventsOneChannel,
        AllProgramEventsOneChannel,
        NewProgramEventsAllChannels,
        AllProgramEventsAllChannels
    }
}
