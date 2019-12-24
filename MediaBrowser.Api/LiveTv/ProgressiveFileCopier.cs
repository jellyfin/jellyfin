using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.LiveTv
{
    public class ProgressiveFileCopier : IAsyncStreamWriter, IHasHeaders
    {
        private readonly ILogger _logger;
        private readonly string _path;
        private readonly Dictionary<string, string> _outputHeaders;

        public bool AllowEndOfFile = true;

        private readonly IDirectStreamProvider _directStreamProvider;
        private readonly IStreamHelper _streamHelper;

        public ProgressiveFileCopier(IStreamHelper streamHelper, string path, Dictionary<string, string> outputHeaders, ILogger logger)
        {
            _path = path;
            _outputHeaders = outputHeaders;
            _logger = logger;
            _streamHelper = streamHelper;
        }

        public ProgressiveFileCopier(IDirectStreamProvider directStreamProvider, IStreamHelper streamHelper, Dictionary<string, string> outputHeaders, ILogger logger)
        {
            _directStreamProvider = directStreamProvider;
            _outputHeaders = outputHeaders;
            _logger = logger;
            _streamHelper = streamHelper;
        }

        public IDictionary<string, string> Headers => _outputHeaders;

        public async Task WriteToAsync(Stream outputStream, CancellationToken cancellationToken)
        {
            if (_directStreamProvider != null)
            {
                await _directStreamProvider.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
                return;
            }

            var fileOptions = FileOptions.SequentialScan;

            // use non-async file stream along with read due to https://github.com/dotnet/corefx/issues/6039
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                fileOptions |= FileOptions.Asynchronous;
            }

            using (var inputStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, fileOptions))
            {
                var emptyReadLimit = AllowEndOfFile ? 20 : 100;
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
}
