using MediaBrowser.Model.Sync;

namespace MediaBrowser.Model.Dto
{
    public interface IHasSyncInfo
    {
        string Id { get; }
        bool? SupportsSync { get; set; }
        bool? HasSyncJob { get; set; }
        double? SyncPercent { get; set; }
        bool? IsSynced { get; set; }
        SyncJobItemStatus? SyncStatus { get; set; }
    }
}
