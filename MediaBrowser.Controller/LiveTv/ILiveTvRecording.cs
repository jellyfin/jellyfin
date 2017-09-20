using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.LiveTv;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    public interface ILiveTvRecording : IHasMetadata, IHasMediaSources, IHasUserData, IHasStartDate, IHasProgramAttributes
    {
        string ServiceName { get; set; }
        string ExternalId { get; set; }
        string ChannelId { get; }
        string MediaType { get; }

        string Container { get; }

        string GetClientTypeName();

        bool IsParentalAllowed(User user);

        Task<ItemUpdateType> RefreshMetadata(MetadataRefreshOptions options, CancellationToken cancellationToken);

        PlayAccess GetPlayAccess(User user);

        bool CanDelete();

        bool CanDelete(User user);

        string SeriesTimerId { get; set; }
        string TimerId { get; set; }
        RecordingStatus Status { get; set; }
        DateTime? EndDate { get; set; }
        DateTime DateCreated { get; set; }
    }

    public class ActiveRecordingInfo
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public TimerInfo Timer { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
}
