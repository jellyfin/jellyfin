using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder
{
    /// <summary>
    /// Class MediaEncoder
    /// </summary>
    public class MediaEncoder : IMediaEncoder, IDisposable
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The video image resource pool
        /// </summary>
        private readonly SemaphoreSlim _videoImageResourcePool = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The audio image resource pool
        /// </summary>
        private readonly SemaphoreSlim _audioImageResourcePool = new SemaphoreSlim(2, 2);

        /// <summary>
        /// The FF probe resource pool
        /// </summary>
        private readonly SemaphoreSlim _ffProbeResourcePool = new SemaphoreSlim(2, 2);

        public string FFMpegPath { get; private set; }

        public string FFProbePath { get; private set; }

        public string Version { get; private set; }

        public MediaEncoder(ILogger logger, IJsonSerializer jsonSerializer, string ffMpegPath, string ffProbePath, string version)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            Version = version;
            FFProbePath = ffProbePath;
            FFMpegPath = ffMpegPath;
        }

        /// <summary>
        /// Gets the encoder path.
        /// </summary>
        /// <value>The encoder path.</value>
        public string EncoderPath
        {
            get { return FFMpegPath; }
        }

        /// <summary>
        /// The _semaphoreLocks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.Object.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            return _semaphoreLocks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Gets the media info.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="isAudio">if set to <c>true</c> [is audio].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task<InternalMediaInfoResult> GetMediaInfo(string[] inputFiles, MediaProtocol protocol, bool isAudio,
            CancellationToken cancellationToken)
        {
            return GetMediaInfoInternal(GetInputArgument(inputFiles, protocol), !isAudio,
                GetProbeSizeArgument(inputFiles, protocol), cancellationToken);
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentException">Unrecognized InputType</exception>
        public string GetInputArgument(string[] inputFiles, MediaProtocol protocol)
        {
            return EncodingUtils.GetInputArgument(inputFiles.ToList(), protocol);
        }

        /// <summary>
        /// Gets the probe size argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <returns>System.String.</returns>
        public string GetProbeSizeArgument(string[] inputFiles, MediaProtocol protocol)
        {
            return EncodingUtils.GetProbeSizeArgument(inputFiles.Length > 1);
        }

        /// <summary>
        /// Gets the media info internal.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <param name="extractChapters">if set to <c>true</c> [extract chapters].</param>
        /// <param name="probeSizeArgument">The probe size argument.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{MediaInfoResult}.</returns>
        /// <exception cref="System.ApplicationException"></exception>
        private async Task<InternalMediaInfoResult> GetMediaInfoInternal(string inputPath, bool extractChapters,
            string probeSizeArgument,
            CancellationToken cancellationToken)
        {
            var args = extractChapters
                ? "{0} -i {1} -threads 0 -v info -print_format json -show_streams -show_chapters -show_format"
                : "{0} -i {1} -threads 0 -v info -print_format json -show_streams -show_format";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both or ffmpeg may hang due to deadlocks. See comments below.   
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = FFProbePath,
                    Arguments = string.Format(args,
                        probeSizeArgument, inputPath).Trim(),

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                },

                EnableRaisingEvents = true
            };

            _logger.Debug("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.Exited += ProcessExited;

            await _ffProbeResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            InternalMediaInfoResult result;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                _ffProbeResourcePool.Release();

                _logger.ErrorException("Error starting ffprobe", ex);

                throw;
            }

            try
            {
                process.BeginErrorReadLine();

                result =
                    _jsonSerializer.DeserializeFromStream<InternalMediaInfoResult>(process.StandardOutput.BaseStream);
            }
            catch
            {
                // Hate having to do this
                try
                {
                    process.Kill();
                }
                catch (Exception ex1)
                {
                    _logger.ErrorException("Error killing ffprobe", ex1);
                }

                throw;
            }
            finally
            {
                _ffProbeResourcePool.Release();
            }

            if (result == null)
            {
                throw new ApplicationException(string.Format("FFProbe failed for {0}", inputPath));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (result.streams != null)
            {
                // Normalize aspect ratio if invalid
                foreach (var stream in result.streams)
                {
                    if (string.Equals(stream.display_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase))
                    {
                        stream.display_aspect_ratio = string.Empty;
                    }
                    if (string.Equals(stream.sample_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase))
                    {
                        stream.sample_aspect_ratio = string.Empty;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// The us culture
        /// </summary>
        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void ProcessExited(object sender, EventArgs e)
        {
            ((Process)sender).Dispose();
        }

        public Task<Stream> ExtractAudioImage(string path, CancellationToken cancellationToken)
        {
            return ExtractImage(new[] { path }, MediaProtocol.File, true, null, null, cancellationToken);
        }

        public Task<Stream> ExtractVideoImage(string[] inputFiles, MediaProtocol protocol, Video3DFormat? threedFormat,
            TimeSpan? offset, CancellationToken cancellationToken)
        {
            return ExtractImage(inputFiles, protocol, false, threedFormat, offset, cancellationToken);
        }

        private async Task<Stream> ExtractImage(string[] inputFiles, MediaProtocol protocol, bool isAudio,
            Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken)
        {
            var resourcePool = isAudio ? _audioImageResourcePool : _videoImageResourcePool;

            var inputArgument = GetInputArgument(inputFiles, protocol);

            if (!isAudio)
            {
                try
                {
                    return await ExtractImageInternal(inputArgument, protocol, threedFormat, offset, true, resourcePool, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    _logger.Error("I-frame image extraction failed, will attempt standard way. Input: {0}", inputArgument);
                }
            }

            return await ExtractImageInternal(inputArgument, protocol, threedFormat, offset, false, resourcePool, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Stream> ExtractImageInternal(string inputPath, MediaProtocol protocol, Video3DFormat? threedFormat, TimeSpan? offset, bool useIFrame, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException("inputPath");
            }

            // apply some filters to thumbnail extracted below (below) crop any black lines that we made and get the correct ar then scale to width 600. 
            // This filter chain may have adverse effects on recorded tv thumbnails if ar changes during presentation ex. commercials @ diff ar
            var vf = "scale=600:trunc(600/dar/2)*2";
            //crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,scale=600:(600/dar),thumbnail" -f image2

            if (threedFormat.HasValue)
            {
                switch (threedFormat.Value)
                {
                    case Video3DFormat.HalfSideBySide:
                        vf = "crop=iw/2:ih:0:0,scale=(iw*2):ih,setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale=600:trunc(600/dar/2)*2";
                        // hsbs crop width in half,scale to correct size, set the display aspect,crop out any black bars we may have made the scale width to 600. Work out the correct height based on the display aspect it will maintain the aspect where -1 in this case (3d) may not.
                        break;
                    case Video3DFormat.FullSideBySide:
                        vf = "crop=iw/2:ih:0:0,setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale=600:trunc(600/dar/2)*2";
                        //fsbs crop width in half,set the display aspect,crop out any black bars we may have made the scale width to 600.
                        break;
                    case Video3DFormat.HalfTopAndBottom:
                        vf = "crop=iw:ih/2:0:0,scale=(iw*2):ih),setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale=600:trunc(600/dar/2)*2";
                        //htab crop heigh in half,scale to correct size, set the display aspect,crop out any black bars we may have made the scale width to 600
                        break;
                    case Video3DFormat.FullTopAndBottom:
                        vf = "crop=iw:ih/2:0:0,setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale=600:trunc(600/dar/2)*2";
                        // ftab crop heigt in half, set the display aspect,crop out any black bars we may have made the scale width to 600
                        break;
                }
            }

            // Use ffmpeg to sample 100 (we can drop this if required using thumbnail=50 for 50 frames) frames and pick the best thumbnail. Have a fall back just in case.
            var args = useIFrame ? string.Format("-i {0} -threads 0 -v quiet -vframes 1 -vf \"{2},thumbnail=30\" -f image2 \"{1}\"", inputPath, "-", vf) :
                string.Format("-i {0} -threads 0 -v quiet -vframes 1 -vf \"{2}\" -f image2 \"{1}\"", inputPath, "-", vf);

            var probeSize = GetProbeSizeArgument(new[] { inputPath }, protocol);

            if (!string.IsNullOrEmpty(probeSize))
            {
                args = probeSize + " " + args;
            }

            if (offset.HasValue)
            {
                args = string.Format("-ss {0} ", GetTimeParameter(offset.Value)) + args;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = FFMpegPath,
                    Arguments = args,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            process.Start();

            var memoryStream = new MemoryStream();

#pragma warning disable 4014
            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
#pragma warning restore 4014

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            process.BeginErrorReadLine();

            var ranToCompletion = process.WaitForExit(10000);

            if (!ranToCompletion)
            {
                try
                {
                    _logger.Info("Killing ffmpeg process");

                    process.Kill();

                    process.WaitForExit(1000);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error killing process", ex);
                }
            }

            resourcePool.Release();

            var exitCode = ranToCompletion ? process.ExitCode : -1;

            process.Dispose();

            if (exitCode == -1 || memoryStream.Length == 0)
            {
                memoryStream.Dispose();

                var msg = string.Format("ffmpeg image extraction failed for {0}", inputPath);

                _logger.Error(msg);

                throw new ApplicationException(msg);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        public Task<Stream> EncodeImage(ImageEncodingOptions options, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _videoImageResourcePool.Dispose();
            }
        }

        public string GetTimeParameter(long ticks)
        {
            var time = TimeSpan.FromTicks(ticks);

            return GetTimeParameter(time);
        }

        public string GetTimeParameter(TimeSpan time)
        {
            return time.ToString(@"hh\:mm\:ss\.fff", UsCulture);
        }
    }
}
