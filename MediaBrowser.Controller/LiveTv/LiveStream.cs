using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.System;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveStream
    {
        public MediaSourceInfo OriginalMediaSource { get; set; }
        public MediaSourceInfo OpenedMediaSource { get; set; }
        public int ConsumerCount
        {
            get { return SharedStreamIds.Count; }
        }
        public ITunerHost TunerHost { get; set; }
        public string OriginalStreamId { get; set; }
        public bool EnableStreamSharing { get; set; }
        public string UniqueId = Guid.NewGuid().ToString("N");

        public List<string> SharedStreamIds = new List<string>();
        protected readonly IEnvironmentInfo Environment;
        protected readonly IFileSystem FileSystem;
        const int StreamCopyToBufferSize = 81920;

        public LiveStream(MediaSourceInfo mediaSource, IEnvironmentInfo environment, IFileSystem fileSystem)
        {
            OriginalMediaSource = mediaSource;
            Environment = environment;
            FileSystem = fileSystem;
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

        protected Stream GetInputStream(string path, long startPosition, bool allowAsyncFileRead)
        {
            var fileOpenOptions = FileOpenOptions.SequentialScan;

            if (allowAsyncFileRead)
            {
                fileOpenOptions |= FileOpenOptions.Asynchronous;
            }

            return FileSystem.GetFileStream(path, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.ReadWrite, fileOpenOptions);
        }

        protected async Task DeleteTempFile(string path, int retryCount = 0)
        {
            try
            {
                FileSystem.DeleteFile(path);
                return;
            }
            catch
            {

            }

            if (retryCount > 20)
            {
                return;
            }

            await Task.Delay(500).ConfigureAwait(false);
            await DeleteTempFile(path, retryCount + 1).ConfigureAwait(false);
        }
    }
}
