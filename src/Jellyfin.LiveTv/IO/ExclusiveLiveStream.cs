#nullable disable

#pragma warning disable CA1711
#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;

namespace Jellyfin.LiveTv.IO
{
    public sealed class ExclusiveLiveStream : ILiveStream
    {
        private readonly Func<Task> _closeFn;

        public ExclusiveLiveStream(MediaSourceInfo mediaSource, Func<Task> closeFn)
        {
            MediaSource = mediaSource;
            EnableStreamSharing = false;
            _closeFn = closeFn;
            ConsumerCount = 1;
            UniqueId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        }

        public int ConsumerCount { get; set; }

        public string OriginalStreamId { get; set; }

        public string TunerHostId => null;

        public bool EnableStreamSharing { get; set; }

        public MediaSourceInfo MediaSource { get; set; }

        public string UniqueId { get; }

        public Task Close()
        {
            return _closeFn();
        }

        public Stream GetStream()
        {
            throw new NotSupportedException();
        }

        public Task Open(CancellationToken openCancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
