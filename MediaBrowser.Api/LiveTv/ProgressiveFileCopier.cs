using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;
using MediaBrowser.Controller.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.LiveTv
{
    public class ProgressiveFileCopier : IAsyncStreamWriter, IHasHeaders
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly string _path;
        private readonly Dictionary<string, string> _outputHeaders;

        const int StreamCopyToBufferSize = 81920;

        public long StartPosition { get; set; }
        public bool AllowEndOfFile = true;

        private readonly IDirectStreamProvider _directStreamProvider;
        private readonly IEnvironmentInfo _environment;
        private IStreamHelper _streamHelper;

        public ProgressiveFileCopier(IFileSystem fileSystem, IStreamHelper streamHelper, string path, Dictionary<string, string> outputHeaders, ILogger logger, IEnvironmentInfo environment)
        {
            _fileSystem = fileSystem;
            _path = path;
            _outputHeaders = outputHeaders;
            _logger = logger;
            _environment = environment;
            _streamHelper = streamHelper;
        }

        public ProgressiveFileCopier(IDirectStreamProvider directStreamProvider, IStreamHelper streamHelper, Dictionary<string, string> outputHeaders, ILogger logger, IEnvironmentInfo environment)
        {
            _directStreamProvider = directStreamProvider;
            _outputHeaders = outputHeaders;
            _logger = logger;
            _environment = environment;
            _streamHelper = streamHelper;
        }

        public IDictionary<string, string> Headers
        {
            get
            {
                return _outputHeaders;
            }
        }

        private Stream GetInputStream(bool allowAsyncFileRead)
        {
            var fileOpenOptions = FileOpenOptions.SequentialScan;

            if (allowAsyncFileRead)
            {
                fileOpenOptions |= FileOpenOptions.Asynchronous;
            }

            return _fileSystem.GetFileStream(_path, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.ReadWrite, fileOpenOptions);
        }

        public async Task WriteToAsync(Stream outputStream, CancellationToken cancellationToken)
        {
            if (_directStreamProvider != null)
            {
                await _directStreamProvider.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
                return;
            }

            var eofCount = 0;

            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039
            var allowAsyncFileRead = true;

            using (var inputStream = GetInputStream(allowAsyncFileRead))
            {
                if (StartPosition > 0)
                {
                    inputStream.Position = StartPosition;
                }

                var emptyReadLimit = AllowEndOfFile ? 20 : 100;

                while (eofCount < emptyReadLimit)
                {
                    int bytesRead;
                    bytesRead = await _streamHelper.CopyToAsync(inputStream, outputStream, cancellationToken).ConfigureAwait(false);

                    //var position = fs.Position;
                    //_logger.LogDebug("Streamed {0} bytes to position {1} from file {2}", bytesRead, position, path);

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
}
