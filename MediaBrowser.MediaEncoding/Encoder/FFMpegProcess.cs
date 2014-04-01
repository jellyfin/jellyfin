using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class FFMpegProcess : IDisposable
    {
        private readonly string _ffmpegPath;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IApplicationPaths _appPaths;
        private readonly IIsoManager _isoManager;
        private readonly ILiveTvManager _liveTvManager;

        private Stream _logFileStream;
        private InternalEncodingTask _task;
        private IIsoMount _isoMount;

        public FFMpegProcess(string ffmpegPath, ILogger logger, IFileSystem fileSystem, IApplicationPaths appPaths, IIsoManager isoManager, ILiveTvManager liveTvManager)
        {
            _ffmpegPath = ffmpegPath;
            _logger = logger;
            _fileSystem = fileSystem;
            _appPaths = appPaths;
            _isoManager = isoManager;
            _liveTvManager = liveTvManager;
        }

        public async Task Start(InternalEncodingTask task, Func<InternalEncodingTask,string,string> argumentsFactory)
        {
            _task = task;
            if (!File.Exists(_ffmpegPath))
            {
                throw new InvalidOperationException("ffmpeg was not found at " + _ffmpegPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(task.Request.OutputPath));

            string mountedPath = null;
            if (task.InputVideoType.HasValue && task.InputVideoType == VideoType.Iso && task.IsoType.HasValue)
            {
                if (_isoManager.CanMount(task.MediaPath))
                {
                    _isoMount = await _isoManager.Mount(task.MediaPath, CancellationToken.None).ConfigureAwait(false);
                    mountedPath = _isoMount.MountedPath;
                }
            }
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both stdout and stderr or deadlocks may occur
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    FileName = _ffmpegPath,
                    WorkingDirectory = Path.GetDirectoryName(_ffmpegPath),
                    Arguments = argumentsFactory(task, mountedPath),

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                },

                EnableRaisingEvents = true
            };

            _logger.Info(process.StartInfo.FileName + " " + process.StartInfo.Arguments);

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "ffmpeg-" + task.Id + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            _logFileStream = _fileSystem.GetFileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, true);

            process.Exited += process_Exited;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error starting ffmpeg", ex);

                task.OnError();

                DisposeLogFileStream();

                process.Dispose();

                throw;
            }

            task.OnBegin();

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            process.BeginOutputReadLine();

#pragma warning disable 4014
            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            process.StandardError.BaseStream.CopyToAsync(_logFileStream);
#pragma warning restore 4014
        }

        async void process_Exited(object sender, EventArgs e)
        {
            var process = (Process)sender;

            if (_isoMount != null)
            {
                _isoMount.Dispose();
                _isoMount = null;
            }

            DisposeLogFileStream();

            try
            {
                _logger.Info("FFMpeg exited with code {0} for {1}", process.ExitCode, _task.Request.OutputPath);
            }
            catch
            {
                _logger.Info("FFMpeg exited with an error for {0}", _task.Request.OutputPath);
            }

            _task.OnCompleted();
            
            if (!string.IsNullOrEmpty(_task.LiveTvStreamId))
            {
                try
                {
                    await _liveTvManager.CloseLiveStream(_task.LiveTvStreamId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error closing live tv stream", ex);
                }
            }
        }

        public void Dispose()
        {
            DisposeLogFileStream();
        }

        private void DisposeLogFileStream()
        {
            if (_logFileStream != null)
            {
                _logFileStream.Dispose();
                _logFileStream = null;
            }
        }
    }
}
