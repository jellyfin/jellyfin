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
    public interface ILiveTvRecording : IHasImages, IHasMediaSources, IHasUserData, IHasStartDate, IHasProgramAttributes
    {
        string ServiceName { get; set; }
        string ExternalId { get; set; }
        string ChannelId { get; }
        string MediaType { get; }

        string Container { get; }

        long? RunTimeTicks { get; set; }

        string GetClientTypeName();

        bool IsParentalAllowed(User user);

        Task<ItemUpdateType> RefreshMetadata(MetadataRefreshOptions options, CancellationToken cancellationToken);

        PlayAccess GetPlayAccess(User user);

        bool CanDelete();

        bool CanDelete(User user);

        string SeriesTimerId { get; set; }
        RecordingStatus Status { get; set; }
        DateTime? EndDate { get; set; }
        DateTime DateLastSaved { get; set; }
        DateTime DateCreated { get; set; }
        DateTime DateModified { get; set; }
    }
}
