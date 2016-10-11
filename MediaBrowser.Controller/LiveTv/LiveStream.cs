using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveStream
    {
        public MediaSourceInfo OriginalMediaSource { get; set; }
        public MediaSourceInfo OpenedMediaSource { get; set; }
        public int ConsumerCount {
            get { return SharedStreamIds.Count; }
        }
        public ITunerHost TunerHost { get; set; }
        public string OriginalStreamId { get; set; }
        public bool EnableStreamSharing { get; set; }
        public string UniqueId = Guid.NewGuid().ToString("N");

        public List<string> SharedStreamIds = new List<string>(); 

        public LiveStream(MediaSourceInfo mediaSource)
        {
            OriginalMediaSource = mediaSource;
            OpenedMediaSource = mediaSource;
            EnableStreamSharing = true;
        }

        public Task Open(CancellationToken cancellationToken)
        {
            return OpenInternal(cancellationToken);
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
