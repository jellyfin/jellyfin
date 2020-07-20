using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Library
{
    public interface ILiveStream
    {
        Task Open(CancellationToken openCancellationToken);
        Task Close();
        int ConsumerCount { get; set; }

        string OriginalStreamId { get; set; }

        string TunerHostId { get; }

        bool EnableStreamSharing { get; }

        MediaSourceInfo MediaSource { get; set; }

        string UniqueId { get; }
    }
}
