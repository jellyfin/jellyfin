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
    public interface ILiveTvRecording : IHasImages, IHasMediaSources, IHasUserData, ILiveTvItem, IHasStartDate, IHasProgramAttributes
    {
        string ChannelId { get; }
        string ProgramId { get; set; }
        string MediaType { get; }

        string Container { get; }

        long? RunTimeTicks { get; set; }

        string GetClientTypeName();

        bool IsParentalAllowed(User user);

        Task<ItemUpdateType> RefreshMetadata(MetadataRefreshOptions options, CancellationToken cancellationToken);

        PlayAccess GetPlayAccess(User user);

        bool CanDelete();

        bool CanDelete(User user);

        string ProviderImagePath { get; set; }

        string ProviderImageUrl { get; set; }

        string ExternalId { get; set; }
        string EpisodeTitle { get; set; }
        bool IsSeries { get; set; }
        string SeriesTimerId { get; set; }
        RecordingStatus Status { get; set; }
        DateTime? EndDate { get; set; }
        ChannelType ChannelType { get; set; }
    }
}
