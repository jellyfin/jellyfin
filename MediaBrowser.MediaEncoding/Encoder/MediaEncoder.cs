using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
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
        /// The _app paths
        /// </summary>
        private readonly IApplicationPaths _appPaths;

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

        private readonly IFileSystem _fileSystem;

        public string FFMpegPath { get; private set; }

        public string FFProbePath { get; private set; }

        public string Version { get; private set; }

        public MediaEncoder(ILogger logger, IApplicationPaths appPaths,
            IJsonSerializer jsonSerializer, string ffMpegPath, string ffProbePath, string version,
            IFileSystem fileSystem)
        {
            _logger = logger;
            _appPaths = appPaths;
            _jsonSerializer = jsonSerializer;
            Version = version;
            _fileSystem = fileSystem;
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
        /// <param name="type">The type.</param>
        /// <param name="isAudio">if set to <c>true</c> [is audio].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task<InternalMediaInfoResult> GetMediaInfo(string[] inputFiles, InputType type, bool isAudio,
            CancellationToken cancellationToken)
        {
            return GetMediaInfoInternal(GetInputArgument(inputFiles, type), !isAudio,
                GetProbeSizeArgument(type), cancellationToken);
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentException">Unrecognized InputType</exception>
        public string GetInputArgument(string[] inputFiles, InputType type)
        {
            string inputPath;

            switch (type)
            {
                case InputType.Bluray:
                case InputType.Dvd:
                case InputType.File:
                    inputPath = GetConcatInputArgument(inputFiles);
                    break;
                case InputType.Url:
                    inputPath = GetHttpInputArgument(inputFiles);
                    break;
                default:
                    throw new ArgumentException("Unrecognized InputType");
            }

            return inputPath;
        }

        /// <summary>
        /// Gets the HTTP input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <returns>System.String.</returns>
        private string GetHttpInputArgument(string[] inputFiles)
        {
            var url = inputFiles[0];

            return string.Format("\"{0}\"", url);
        }

        /// <summary>
        /// Gets the probe size argument.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        public string GetProbeSizeArgument(InputType type)
        {
            return type == InputType.Dvd ? "-probesize 1G -analyzeduration 200M" : string.Empty;
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

        /// <summary>
        /// Converts the text subtitle to ass.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="language">The language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ConvertTextSubtitleToAss(string inputPath, string outputPath, string language,
            CancellationToken cancellationToken)
        {
            var semaphore = GetLock(outputPath);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(outputPath))
                {
                    await ConvertTextSubtitleToAssInternal(inputPath, outputPath, language).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private const int FastSeekOffsetSeconds = 1;

        /// <summary>
        /// Converts the text subtitle to ass.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="language">The language.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">inputPath
        /// or
        /// outputPath</exception>
        /// <exception cref="System.ApplicationException"></exception>
        private async Task ConvertTextSubtitleToAssInternal(string inputPath, string outputPath, string language)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException("inputPath");
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }


            var encodingParam = string.IsNullOrEmpty(language)
                ? string.Empty
                : GetSubtitleLanguageEncodingParam(language) + " ";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,

                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = FFMpegPath,
                    Arguments =
                        string.Format("{0} -i \"{1}\" -c:s ass \"{2}\"", encodingParam, inputPath, outputPath),

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                }
            };

            _logger.Debug("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "ffmpeg-sub-convert-" + Guid.NewGuid() + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            var logFileStream = _fileSystem.GetFileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read,
                true);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                logFileStream.Dispose();

                _logger.ErrorException("Error starting ffmpeg", ex);

                throw;
            }

            var logTask = process.StandardError.BaseStream.CopyToAsync(logFileStream);

            var ranToCompletion = process.WaitForExit(60000);

            if (!ranToCompletion)
            {
                try
                {
                    _logger.Info("Killing ffmpeg subtitle conversion process");

                    process.Kill();

                    process.WaitForExit(1000);

                    await logTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error killing subtitle conversion process", ex);
                }
                finally
                {
                    logFileStream.Dispose();
                }
            }

            var exitCode = ranToCompletion ? process.ExitCode : -1;

            process.Dispose();

            var failed = false;

            if (exitCode == -1)
            {
                failed = true;

                if (File.Exists(outputPath))
                {
                    try
                    {
                        _logger.Info("Deleting converted subtitle due to failure: ", outputPath);
                        File.Delete(outputPath);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error deleting converted subtitle {0}", ex, outputPath);
                    }
                }
            }
            else if (!File.Exists(outputPath))
            {
                failed = true;
            }

            if (failed)
            {
                var msg = string.Format("ffmpeg subtitle converted failed for {0}", inputPath);

                _logger.Error(msg);

                throw new ApplicationException(msg);
            }
            await SetAssFont(outputPath).ConfigureAwait(false);
        }

        protected string GetFastSeekCommandLineParameter(TimeSpan offset)
        {
            var seconds = offset.TotalSeconds - FastSeekOffsetSeconds;

            if (seconds > 0)
            {
                return string.Format("-ss {0} ", seconds.ToString(UsCulture));
            }

            return string.Empty;
        }

        protected string GetSlowSeekCommandLineParameter(TimeSpan offset)
        {
            if (offset.TotalSeconds - FastSeekOffsetSeconds > 0)
            {
                return string.Format(" -ss {0}", FastSeekOffsetSeconds.ToString(UsCulture));
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the subtitle language encoding param.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <returns>System.String.</returns>
        private string GetSubtitleLanguageEncodingParam(string language)
        {
            switch (language.ToLower())
            {
                case "pol":
                case "cze":
                case "ces":
                case "slo":
                case "slk":
                case "hun":
                case "slv":
                case "srp":
                case "hrv":
                case "rum":
                case "ron":
                case "rup":
                case "alb":
                case "sqi":
                    return "-sub_charenc windows-1250";
                case "ara":
                    return "-sub_charenc windows-1256";
                case "heb":
                    return "-sub_charenc windows-1255";
                case "grc":
                case "gre":
                    return "-sub_charenc windows-1253";
                case "crh":
                case "ota":
                case "tur":
                    return "-sub_charenc windows-1254";
                case "rus":
                    return "-sub_charenc windows-1251";
                case "vie":
                    return "-sub_charenc windows-1258";
                case "kor":
                    return "-sub_charenc cp949";
                default:
                    return "-sub_charenc windows-1252";
            }
        }

        /// <summary>
        /// Extracts the text subtitle.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="type">The type.</param>
        /// <param name="subtitleStreamIndex">Index of the subtitle stream.</param>
        /// <param name="copySubtitleStream">if set to true, copy stream instead of converting.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentException">Must use inputPath list overload</exception>
        public async Task ExtractTextSubtitle(string[] inputFiles, InputType type, int subtitleStreamIndex,
            bool copySubtitleStream, string outputPath, CancellationToken cancellationToken)
        {
            var semaphore = GetLock(outputPath);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(outputPath))
                {
                    await
                        ExtractTextSubtitleInternal(GetInputArgument(inputFiles, type), subtitleStreamIndex,
                            copySubtitleStream, outputPath, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Extracts the text subtitle.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <param name="subtitleStreamIndex">Index of the subtitle stream.</param>
        /// <param name="copySubtitleStream">if set to true, copy stream instead of converting.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">inputPath
        /// or
        /// outputPath
        /// or
        /// cancellationToken</exception>
        /// <exception cref="System.ApplicationException"></exception>
        private async Task ExtractTextSubtitleInternal(string inputPath, int subtitleStreamIndex,
            bool copySubtitleStream, string outputPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException("inputPath");
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }

            string processArgs = string.Format("-i {0} -map 0:{1} -an -vn -c:s ass \"{2}\"", inputPath,
                subtitleStreamIndex, outputPath);

            if (copySubtitleStream)
            {
                processArgs = string.Format("-i {0} -map 0:{1} -an -vn -c:s copy \"{2}\"", inputPath,
                    subtitleStreamIndex, outputPath);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    RedirectStandardOutput = false,
                    RedirectStandardError = true,

                    FileName = FFMpegPath,
                    Arguments = processArgs,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                }
            };

            _logger.Debug("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "ffmpeg-sub-extract-" + Guid.NewGuid() + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            var logFileStream = _fileSystem.GetFileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read,
                true);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                logFileStream.Dispose();

                _logger.ErrorException("Error starting ffmpeg", ex);

                throw;
            }

            process.StandardError.BaseStream.CopyToAsync(logFileStream);

            var ranToCompletion = process.WaitForExit(60000);

            if (!ranToCompletion)
            {
                try
                {
                    _logger.Info("Killing ffmpeg subtitle extraction process");

                    process.Kill();

                    process.WaitForExit(1000);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error killing subtitle extraction process", ex);
                }
                finally
                {
                    logFileStream.Dispose();
                }
            }

            var exitCode = ranToCompletion ? process.ExitCode : -1;

            process.Dispose();

            var failed = false;

            if (exitCode == -1)
            {
                failed = true;

                if (File.Exists(outputPath))
                {
                    try
                    {
                        _logger.Info("Deleting extracted subtitle due to failure: ", outputPath);
                        File.Delete(outputPath);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error deleting extracted subtitle {0}", ex, outputPath);
                    }
                }
            }
            else if (!File.Exists(outputPath))
            {
                failed = true;
            }

            if (failed)
            {
                var msg = string.Format("ffmpeg subtitle extraction failed for {0} to {1}", inputPath, outputPath);

                _logger.Error(msg);

                throw new ApplicationException(msg);
            }
            else
            {
                var msg = string.Format("ffmpeg subtitle extraction completed for {0} to {1}", inputPath, outputPath);

                _logger.Info(msg);
            }

            await SetAssFont(outputPath).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the ass font.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>Task.</returns>
        private async Task SetAssFont(string file)
        {
            _logger.Info("Setting ass font within {0}", file);

            string text;
            Encoding encoding;

            using (var reader = new StreamReader(file, detectEncodingFromByteOrderMarks: true))
            {
                encoding = reader.CurrentEncoding;

                text = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            var newText = text.Replace(",Arial,", ",Arial Unicode MS,");

            if (!string.Equals(text, newText))
            {
                using (var writer = new StreamWriter(file, false, encoding))
                {
                    writer.Write(newText);
                }
            }
        }

        public Task<Stream> ExtractAudioImage(string path, CancellationToken cancellationToken)
        {
            return ExtractImage(new[] { path }, InputType.File, true, null, null, cancellationToken);
        }

        public Task<Stream> ExtractVideoImage(string[] inputFiles, InputType type, Video3DFormat? threedFormat,
            TimeSpan? offset, CancellationToken cancellationToken)
        {
            return ExtractImage(inputFiles, type, false, threedFormat, offset, cancellationToken);
        }

        private async Task<Stream> ExtractImage(string[] inputFiles, InputType type, bool isAudio,
            Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken)
        {
            var resourcePool = isAudio ? _audioImageResourcePool : _videoImageResourcePool;

            var inputArgument = GetInputArgument(inputFiles, type);

            if (!isAudio)
            {
                try
                {
                    return await ExtractImageInternal(inputArgument, type, threedFormat, offset, true, resourcePool, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    _logger.Error("I-frame image extraction failed, will attempt standard way. Input: {0}", inputArgument);
                }
            }

            return await ExtractImageInternal(inputArgument, type, threedFormat, offset, false, resourcePool, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Stream> ExtractImageInternal(string inputPath, InputType type, Video3DFormat? threedFormat, TimeSpan? offset, bool useIFrame, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
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
            var args = useIFrame ? string.Format("-i {0} -threads 0 -v quiet -vframes 1 -vf \"{2},thumbnail=80\" -f image2 \"{1}\"", inputPath, "-", vf) :
                string.Format("-i {0} -threads 0 -v quiet -vframes 1 -vf \"{2}\" -f image2 \"{1}\"", inputPath, "-", vf);

            var probeSize = GetProbeSizeArgument(type);

            if (!string.IsNullOrEmpty(probeSize))
            {
                args = probeSize + " " + args;
            }

            if (offset.HasValue)
            {
                args = string.Format("-ss {0} ", Convert.ToInt32(offset.Value.TotalSeconds)).ToString(UsCulture) + args;
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

        /// <summary>
        /// Gets the file input argument.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        private string GetFileInputArgument(string path)
        {
            return string.Format("file:\"{0}\"", path);
        }

        /// <summary>
        /// Gets the concat input argument.
        /// </summary>
        /// <param name="playableStreamFiles">The playable stream files.</param>
        /// <returns>System.String.</returns>
        private string GetConcatInputArgument(string[] playableStreamFiles)
        {
            // Get all streams
            // If there's more than one we'll need to use the concat command
            if (playableStreamFiles.Length > 1)
            {
                var files = string.Join("|", playableStreamFiles);

                return string.Format("concat:\"{0}\"", files);
            }

            // Determine the input path for video files
            return GetFileInputArgument(playableStreamFiles[0]);
        }

        public Task<Stream> EncodeImage(ImageEncodingOptions options, CancellationToken cancellationToken)
        {
            return new ImageEncoder(FFMpegPath, _logger, _fileSystem).EncodeImage(options, cancellationToken);
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
    }
}
