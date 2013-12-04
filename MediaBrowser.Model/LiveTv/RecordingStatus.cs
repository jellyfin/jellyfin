
namespace MediaBrowser.Model.LiveTv
{
    public enum RecordingStatus
    {
        Pending,
        InProgress,
        Completed,
        CompletedWithError,
        Conflicted,
        Deleted
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
