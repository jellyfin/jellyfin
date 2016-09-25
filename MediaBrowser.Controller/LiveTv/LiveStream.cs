using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveStream
    {
        public MediaSourceInfo OriginalMediaSource { get; set; }
        public MediaSourceInfo PublicMediaSource { get; set; }
        public string Id { get; set; }

        public LiveStream(MediaSourceInfo mediaSource)
        {
            OriginalMediaSource = mediaSource;
            PublicMediaSource = mediaSource;
            Id = mediaSource.Id;
        }

        public virtual Task Open(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public virtual Task Close()
        {
            return Task.FromResult(true);
        }
    }
}
