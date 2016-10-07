using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveStream
    {
        public MediaSourceInfo OriginalMediaSource { get; set; }
        public MediaSourceInfo OpenedMediaSource { get; set; }
        public DateTime DateOpened { get; set; }
        public int ConsumerCount { get; set; }
        public ITunerHost TunerHost { get; set; }
        public string OriginalStreamId { get; set; }
        public bool EnableStreamSharing { get; set; }
        public string UniqueId = Guid.NewGuid().ToString("N");

        public LiveStream(MediaSourceInfo mediaSource)
        {
            OriginalMediaSource = mediaSource;
            OpenedMediaSource = mediaSource;
            EnableStreamSharing = true;
        }

        public async Task Open(CancellationToken cancellationToken)
        {
            await OpenInternal(cancellationToken).ConfigureAwait(false);
            DateOpened = DateTime.UtcNow;

            OpenedMediaSource.DateLiveStreamOpened = DateOpened;
        }

        protected virtual Task OpenInternal(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public virtual Task Close()
        {
            return Task.FromResult(true);
        }
    }
}
