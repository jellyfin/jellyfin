using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.MediaEncoder
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
        private readonly SemaphoreSlim _audioImageResourcePool = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The FF probe resource pool
        /// </summary>
        private readonly SemaphoreSlim _ffProbeResourcePool = new SemaphoreSlim(1, 1);
        private readonly IFileSystem _fileSystem;

        public string FFMpegPath { get; private set; }

        public string FFProbePath { get; private set; }

        public string Version { get; private set; }

        public MediaEncoder(ILogger logger, IApplicationPaths appPaths,
                            IJsonSerializer jsonSerializer, string ffMpegPath, string ffProbePath, string version, IFileSystem fileSystem)
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
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task<MediaInfoResult> GetMediaInfo(string[] inputFiles, InputType type,
                                                  CancellationToken cancellationToken)
        {
            return GetMediaInfoInternal(GetInputArgument(inputFiles, type), type != InputType.AudioFile,
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
                case InputType.Dvd:
                case InputType.VideoFile:
                case InputType.AudioFile:
                    inputPath = GetConcatInputArgument(inputFiles);
                    break;
                case InputType.Bluray:
                    inputPath = GetBlurayInputArgument(inputFiles[0]);
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
        private async Task<MediaInfoResult> GetMediaInfoInternal(string inputPath, bool extractChapters,
                                                                 string probeSizeArgument,
                                                                 CancellationToken cancellationToken)
        {
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
                            Arguments =
                                string.Format(
                                    "{0} -i {1} -threads 0 -v info -print_format json -show_streams -show_format",
                                    probeSizeArgument, inputPath).Trim(),

                            WindowStyle = ProcessWindowStyle.Hidden,
                            ErrorDialog = false
                        },

                    EnableRaisingEvents = true
                };

            _logger.Debug("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.Exited += ProcessExited;

            await _ffProbeResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            MediaInfoResult result;
            string standardError = null;

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
                Task<string> standardErrorReadTask = null;

                // MUST read both stdout and stderr asynchronously or a deadlock may occurr
                if (extractChapters)
                {
                    standardErrorReadTask = process.StandardError.ReadToEndAsync();
                }
                else
                {
                    process.BeginErrorReadLine();
                }

                result = _jsonSerializer.DeserializeFromStream<MediaInfoResult>(process.StandardOutput.BaseStream);

                if (extractChapters)
                {
                    standardError = await standardErrorReadTask.ConfigureAwait(false);
                }
            }
            catch
            {
                // Hate having to do this
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException ex1)
                {
                    _logger.ErrorException("Error killing ffprobe", ex1);
                }
                catch (Win32Exception ex1)
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

            if (extractChapters && !string.IsNullOrEmpty(standardError))
            {
                AddChapters(result, standardError);
            }

            return result;
        }

        /// <summary>
        /// The us culture
        /// </summary>
        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Adds the chapters.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="standardError">The standard error.</param>
        private void AddChapters(MediaInfoResult result, string standardError)
        {
            var lines = standardError.Split('\n').Select(l => l.TrimStart());

            var chapters = new List<ChapterInfo>();

            ChapterInfo lastChapter = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("Chapter", StringComparison.OrdinalIgnoreCase))
                {
                    // Example:
                    // Chapter #0.2: start 400.534, end 4565.435
                    const string srch = "start ";
                    var start = line.IndexOf(srch, StringComparison.OrdinalIgnoreCase);

                    if (start == -1)
                    {
                        continue;
                    }

                    var subString = line.Substring(start + srch.Length);
                    subString = subString.Substring(0, subString.IndexOf(','));

                    double seconds;

                    if (double.TryParse(subString, NumberStyles.Any, UsCulture, out seconds))
                    {
                        lastChapter = new ChapterInfo
                            {
                                StartPositionTicks = TimeSpan.FromSeconds(seconds).Ticks
                            };

                        chapters.Add(lastChapter);
                    }
                }

                else if (line.StartsWith("title", StringComparison.OrdinalIgnoreCase))
                {
                    if (lastChapter != null && string.IsNullOrEmpty(lastChapter.Name))
                    {
                        var index = line.IndexOf(':');

                        if (index != -1)
                        {
                            lastChapter.Name = line.Substring(index + 1).Trim().TrimEnd('\r');
                        }
                    }
                }
            }

            result.Chapters = chapters;
        }

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
        /// <param name="offset">The offset.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ConvertTextSubtitleToAss(string inputPath, string outputPath, string language, TimeSpan offset,
                                                   CancellationToken cancellationToken)
        {
            var semaphore = GetLock(outputPath);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(outputPath))
                {
                    await ConvertTextSubtitleToAssInternal(inputPath, outputPath, language, offset).ConfigureAwait(false);
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
        /// <param name="offset">The offset.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">inputPath
        /// or
        /// outputPath</exception>
        /// <exception cref="System.ApplicationException"></exception>
        private async Task ConvertTextSubtitleToAssInternal(string inputPath, string outputPath, string language, TimeSpan offset)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException("inputPath");
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }

            var slowSeekParam = GetSlowSeekCommandLineParameter(offset);
            var fastSeekParam = GetFastSeekCommandLineParameter(offset);

            var encodingParam = string.IsNullOrEmpty(language) ? string.Empty :
                GetSubtitleLanguageEncodingParam(language) + " ";

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
                                string.Format("{0}{1}-i \"{2}\"{3} \"{4}\"", 
                                fastSeekParam, 
                                encodingParam, 
                                inputPath, 
                                slowSeekParam,
                                outputPath),

                            WindowStyle = ProcessWindowStyle.Hidden,
                            ErrorDialog = false
                        }
                };

            _logger.Debug("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "ffmpeg-sub-convert-" + Guid.NewGuid() + ".txt");

            var logFileStream = _fileSystem.GetFileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, true);

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
        /// <param name="offset">The offset.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentException">Must use inputPath list overload</exception>
        public async Task ExtractTextSubtitle(string[] inputFiles, InputType type, int subtitleStreamIndex, TimeSpan offset, string outputPath, CancellationToken cancellationToken)
        {
            var semaphore = GetLock(outputPath);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(outputPath))
                {
                    await ExtractTextSubtitleInternal(GetInputArgument(inputFiles, type), subtitleStreamIndex, offset, outputPath, cancellationToken).ConfigureAwait(false);
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
        /// <param name="offset">The offset.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">inputPath
        /// or
        /// outputPath
        /// or
        /// cancellationToken</exception>
        /// <exception cref="System.ApplicationException"></exception>
        private async Task ExtractTextSubtitleInternal(string inputPath, int subtitleStreamIndex, TimeSpan offset, string outputPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException("inputPath");
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }

            var slowSeekParam = offset.TotalSeconds > 0 ? " -ss " + offset.TotalSeconds.ToString(UsCulture) : string.Empty;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    RedirectStandardOutput = false,
                    RedirectStandardError = true,

                    FileName = FFMpegPath,
                    Arguments = string.Format("-i {0}{1} -map 0:{2} -an -vn -c:s ass \"{3}\"", inputPath, slowSeekParam, subtitleStreamIndex, outputPath),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                }
            };

            _logger.Debug("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "ffmpeg-sub-extract-" + Guid.NewGuid() + ".txt");

            var logFileStream = _fileSystem.GetFileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, true);

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

        /// <summary>
        /// Extracts the image.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="type">The type.</param>
        /// <param name="threedFormat">The threed format.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentException">Must use inputPath list overload</exception>
        public async Task ExtractImage(string[] inputFiles, InputType type, Video3DFormat? threedFormat, TimeSpan? offset, string outputPath, CancellationToken cancellationToken)
        {
            var resourcePool = type == InputType.AudioFile ? _audioImageResourcePool : _videoImageResourcePool;

            var inputArgument = GetInputArgument(inputFiles, type);

            if (type != InputType.AudioFile)
            {
                try
                {
                    await ExtractImageInternal(inputArgument, type, threedFormat, offset, outputPath, true, resourcePool, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch
                {
                    _logger.Error("I-frame image extraction failed, will attempt standard way. Input: {0}", inputArgument);
                }
            }

            await ExtractImageInternal(inputArgument, type, threedFormat, offset, outputPath, false, resourcePool, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Extracts the image.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <param name="type">The type.</param>
        /// <param name="threedFormat">The threed format.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="useIFrame">if set to <c>true</c> [use I frame].</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">inputPath
        /// or
        /// outputPath</exception>
        /// <exception cref="System.ApplicationException"></exception>
        private async Task ExtractImageInternal(string inputPath, InputType type, Video3DFormat? threedFormat, TimeSpan? offset, string outputPath, bool useIFrame, SemaphoreSlim resourcePool, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException("inputPath");
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath");
            }

            var vf = "scale=iw*sar:ih, scale=600:-1";

            if (threedFormat.HasValue)
            {
                switch (threedFormat.Value)
                {
                    case Video3DFormat.HalfSideBySide:
                    case Video3DFormat.FullSideBySide:
                        vf = "crop=iw/2:ih:0:0,scale=(iw*2):ih,scale=600:-1";
                        break;
                    case Video3DFormat.HalfTopAndBottom:
                    case Video3DFormat.FullTopAndBottom:
                        vf = "crop=iw:ih/2:0:0,scale=iw:(ih*2),scale=600:-1";
                        break;
                }
            }

            var args = useIFrame ? string.Format("-i {0} -threads 0 -v quiet -vframes 1 -filter:v select=\"eq(pict_type\\,I)\" -vf \"{2}\" -f image2 \"{1}\"", inputPath, outputPath, vf) :
                string.Format("-i {0} -threads 0 -v quiet -vframes 1 -vf \"{2}\" -f image2 \"{1}\"", inputPath, outputPath, vf);

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
                    ErrorDialog = false
                }
            };

            await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            var ranToCompletion = StartAndWaitForProcess(process);

            resourcePool.Release();

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
                        _logger.Info("Deleting extracted image due to failure: ", outputPath);
                        File.Delete(outputPath);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error deleting extracted image {0}", ex, outputPath);
                    }
                }
            }
            else if (!File.Exists(outputPath))
            {
                failed = true;
            }

            if (failed)
            {
                var msg = string.Format("ffmpeg image extraction failed for {0}", inputPath);

                _logger.Error(msg);

                throw new ApplicationException(msg);
            }
        }

        /// <summary>
        /// Starts the and wait for process.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool StartAndWaitForProcess(Process process, int timeout = 10000)
        {
            process.Start();

            var ranToCompletion = process.WaitForExit(timeout);

            if (!ranToCompletion)
            {
                try
                {
                    _logger.Info("Killing ffmpeg process");

                    process.Kill();

                    process.WaitForExit(1000);
                }
                catch (Win32Exception ex)
                {
                    _logger.ErrorException("Error killing process", ex);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.ErrorException("Error killing process", ex);
                }
                catch (NotSupportedException ex)
                {
                    _logger.ErrorException("Error killing process", ex);
                }
            }

            return ranToCompletion;
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

        /// <summary>
        /// Gets the bluray input argument.
        /// </summary>
        /// <param name="blurayRoot">The bluray root.</param>
        /// <returns>System.String.</returns>
        private string GetBlurayInputArgument(string blurayRoot)
        {
            return string.Format("bluray:\"{0}\"", blurayRoot);
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
