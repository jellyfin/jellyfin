using MediaBrowser.Model.Logging;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Api.Playback.Progressive
{
    public class ProgressiveStreamWriter : IStreamWriter, IHasOptions
    {
        private string Path { get; set; }
        private ILogger Logger { get; set; }
        private readonly IFileSystem _fileSystem;
        private readonly TranscodingJob _job;

        /// <summary>
        /// The _options
        /// </summary>
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>();
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, string> Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressiveStreamWriter" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        public ProgressiveStreamWriter(string path, ILogger logger, IFileSystem fileSystem, TranscodingJob job)
        {
            Path = path;
            Logger = logger;
            _fileSystem = fileSystem;
            _job = job;
        }

        /// <summary>
        /// Writes to.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        public void WriteTo(Stream responseStream)
        {
            WriteToInternal(responseStream);
        }

        /// <summary>
        /// Writes to async.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <returns>Task.</returns>
        private void WriteToInternal(Stream responseStream)
        {
            try
            {
                var task = new ProgressiveFileCopier(_fileSystem, _job, Logger).StreamFile(Path, responseStream);

                Task.WaitAll(task);
            }
            catch (IOException)
            {
                // These error are always the same so don't dump the whole stack trace
                Logger.Error("Error streaming media. The client has most likely disconnected or transcoding has failed.");

                throw;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error streaming media. The client has most likely disconnected or transcoding has failed.", ex);

                throw;
            }
            finally
            {
                if (_job != null)
                {
                    ApiEntryPoint.Instance.OnTranscodeEndRequest(_job);
                }
            }
        }
    }

    public class ProgressiveFileCopier
    {
        private readonly IFileSystem _fileSystem;
        private readonly TranscodingJob _job;
        private readonly ILogger _logger;

        // 256k
        private const int BufferSize = 262144;

        private long _bytesWritten = 0;

        public ProgressiveFileCopier(IFileSystem fileSystem, TranscodingJob job, ILogger logger)
        {
            _fileSystem = fileSystem;
            _job = job;
            _logger = logger;
        }

        public async Task StreamFile(string path, Stream outputStream)
        {
            var eofCount = 0;
            long position = 0;

            using (var fs = _fileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, false))
            {
                while (eofCount < 15)
                {
                    CopyToInternal(fs, outputStream, BufferSize);

                    var fsPosition = fs.Position;

                    var bytesRead = fsPosition - position;

                    //Logger.Debug("Streamed {0} bytes from file {1}", bytesRead, path);

                    if (bytesRead == 0)
                    {
                        if (_job == null || _job.HasExited)
                        {
                            eofCount++;
                        }
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                    else
                    {
                        eofCount = 0;
                    }

                    position = fsPosition;
                }
            }
        }

        private void CopyToInternal(Stream source, Stream destination, int bufferSize)
        {
            var array = new byte[bufferSize];
            int count;
            while ((count = source.Read(array, 0, array.Length)) != 0)
            {
                //if (_job != null)
                //{
                //    var didPause = false;
                //    var totalPauseTime = 0;

                //    if (_job.IsUserPaused)
                //    {
                //        _logger.Debug("Pausing writing to network stream while user has paused playback.");

                //        while (_job.IsUserPaused && totalPauseTime < 30000)
                //        {
                //            didPause = true;
                //            var pauseTime = 500;
                //            totalPauseTime += pauseTime;
                //            await Task.Delay(pauseTime).ConfigureAwait(false);
                //        }
                //    }

                //    if (didPause)
                //    {
                //        _logger.Debug("Resuming writing to network stream due to user unpausing playback.");
                //    }
                //}

                destination.Write(array, 0, count);

                _bytesWritten += count;

                if (_job != null)
                {
                    _job.BytesDownloaded = Math.Max(_job.BytesDownloaded ?? _bytesWritten, _bytesWritten);
                }
            }
        }
    }
}
