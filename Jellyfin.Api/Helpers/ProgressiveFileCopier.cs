using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Progressive file copier.
    /// </summary>
    public class ProgressiveFileCopier
    {
        private readonly string? _path;
        private readonly IDirectStreamProvider? _directStreamProvider;
        private readonly IStreamHelper _streamHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressiveFileCopier"/> class.
        /// </summary>
        /// <param name="streamHelper">Instance of the <see cref="IStreamHelper"/> interface.</param>
        /// <param name="path">Filepath to stream from.</param>
        public ProgressiveFileCopier(IStreamHelper streamHelper, string path)
        {
            _path = path;
            _streamHelper = streamHelper;
            _directStreamProvider = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressiveFileCopier"/> class.
        /// </summary>
        /// <param name="streamHelper">Instance of the <see cref="IStreamHelper"/> interface.</param>
        /// <param name="directStreamProvider">Instance of the <see cref="IDirectStreamProvider"/> interface.</param>
        public ProgressiveFileCopier(IStreamHelper streamHelper, IDirectStreamProvider directStreamProvider)
        {
            _directStreamProvider = directStreamProvider;
            _streamHelper = streamHelper;
            _path = null;
        }

        /// <summary>
        /// Write source stream to output.
        /// </summary>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task WriteToAsync(Stream outputStream, CancellationToken cancellationToken)
        {
            if (_directStreamProvider != null)
            {
                await _directStreamProvider.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
                return;
            }

            var fileOptions = FileOptions.SequentialScan;

            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                fileOptions |= FileOptions.Asynchronous;
            }

            await using var inputStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, fileOptions);
            const int emptyReadLimit = 100;
            var eofCount = 0;
            while (eofCount < emptyReadLimit)
            {
                var bytesRead = await _streamHelper.CopyToAsync(inputStream, outputStream, cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    eofCount++;
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    eofCount = 0;
                }
            }
        }
    }
}
