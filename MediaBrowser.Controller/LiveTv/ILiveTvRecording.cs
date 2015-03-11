using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Library;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    public interface ILiveTvRecording : IHasImages, IHasMediaSources, IHasUserData
    {
        string ServiceName { get; set; }

        string MediaType { get; }

        string Container { get; }

        RecordingInfo RecordingInfo { get; set; }

        long? RunTimeTicks { get; set; }

        string GetClientTypeName();

        bool IsParentalAllowed(User user);

        Task<ItemUpdateType> RefreshMetadata(MetadataRefreshOptions options, CancellationToken cancellationToken);

        PlayAccess GetPlayAccess(User user);

        bool CanDelete();

        bool CanDelete(User user);
    }
}
