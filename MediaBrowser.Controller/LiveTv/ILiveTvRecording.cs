using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Library;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    public interface ILiveTvRecording : IHasImages, IHasMediaStreams
    {
        string ServiceName { get; set; }

        string MediaType { get; }

        RecordingInfo RecordingInfo { get; set; }

        long? RunTimeTicks { get; set; }

        string GetClientTypeName();

        string GetUserDataKey();

        bool IsParentalAllowed(User user);

        Task RefreshMetadata(MetadataRefreshOptions options, CancellationToken cancellationToken);

        PlayAccess GetPlayAccess(User user);
    }
}
