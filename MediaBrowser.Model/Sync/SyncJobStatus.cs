#pragma warning disable CS1591
#pragma warning disable SA1602 // Enumeration items should be documented

namespace MediaBrowser.Model.Sync
{
    public enum SyncJobStatus
    {
        Queued = 0,
        Converting = 1,
        ReadyToTransfer = 2,
        Transferring = 3,
        Completed = 4,
        CompletedWithError = 5,
        Failed = 6
    }
}
