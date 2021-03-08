#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using UtfUnknown;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class SubtitleEncoder : ISubtitleEncoder
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<SubtitleEncoder> _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMediaSourceManager _mediaSourceManager;

        /// <summary>
        /// The _semaphoreLocks.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        public SubtitleEncoder(
            ILibraryManager libraryManager,
            ILogger<SubtitleEncoder> logger,
            IApplicationPaths appPaths,
            IFileSystem fileSystem,
            IMediaEncoder mediaEncoder,
            IHttpClientFactory httpClientFactory,
            IMediaSourceManager mediaSourceManager)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _mediaEncoder = mediaEncoder;
            _httpClientFactory = httpClientFactory;
            _mediaSourceManager = mediaSourceManager;
        }

        private string SubtitleCachePath => Path.Combine(_appPaths.DataPath, "subtitles");

        private Stream ConvertSubtitles(
            Stream stream,
            string inputFormat,
            string outputFormat,
            long startTimeTicks,
            long endTimeTicks,
            bool preserveOriginalTimestamps,
            CancellationToken cancellationToken)
        {
            var ms = new MemoryStream();

            try
            {
                var reader = GetReader(inputFormat, true);

                var trackInfo = reader.Parse(stream, cancellationToken);

                FilterEvents(trackInfo, startTimeTicks, endTimeTicks, preserveOriginalTimestamps);

                var writer = GetWriter(outputFormat);

                writer.Write(trackInfo, ms, cancellationToken);
                ms.Position = 0;
            }
            catch
            {
                ms.Dispose();
                throw;
            }

            return ms;
        }

        private void FilterEvents(SubtitleTrackInfo track, long startPositionTicks, long endTimeTicks, bool preserveTimestamps)
        {
            // Drop subs that are earlier than what we're looking for
            track.TrackEvents = track.TrackEvents
                .SkipWhile(i => (i.StartPositionTicks - startPositionTicks) < 0 || (i.EndPositionTicks - startPositionTicks) < 0)
                .ToArray();

            if (endTimeTicks > 0)
            {
                track.TrackEvents = track.TrackEvents
                    .TakeWhile(i => i.StartPositionTicks <= endTimeTicks)
                    .ToArray();
            }

            if (!preserveTimestamps)
            {
                foreach (var trackEvent in track.TrackEvents)
                {
                    trackEvent.EndPositionTicks -= startPositionTicks;
                    trackEvent.StartPositionTicks -= startPositionTicks;
                }
            }
        }

        async Task<Stream> ISubtitleEncoder.GetSubtitles(BaseItem item, string mediaSourceId, int subtitleStreamIndex, string outputFormat, long startTimeTicks, long endTimeTicks, bool preserveOriginalTimestamps, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (string.IsNullOrWhiteSpace(mediaSourceId))
            {
                throw new ArgumentNullException(nameof(mediaSourceId));
            }

            var mediaSources = await _mediaSourceManager.GetPlaybackMediaSources(item, null, true, false, cancellationToken).ConfigureAwait(false);

            var mediaSource = mediaSources
                .First(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase));

            var subtitleStream = mediaSource.MediaStreams
               .First(i => i.Type == MediaStreamType.Subtitle && i.Index == subtitleStreamIndex);

            var subtitle = await GetSubtitleStream(mediaSource, subtitleStream, cancellationToken)
                        .ConfigureAwait(false);

            var inputFormat = subtitle.format;
            var writer = TryGetWriter(outputFormat);

            // Return the original if we don't have any way of converting it
            if (writer == null)
            {
                return subtitle.stream;
            }

            // Return the original if the same format is being requested
            // Character encoding was already handled in GetSubtitleStream
            if (string.Equals(inputFormat, outputFormat, StringComparison.OrdinalIgnoreCase))
            {
                return subtitle.stream;
            }

            using (var stream = subtitle.stream)
            {
                return ConvertSubtitles(stream, inputFormat, outputFormat, startTimeTicks, endTimeTicks, preserveOriginalTimestamps, cancellationToken);
            }
        }

        private async Task<(Stream stream, string format)> GetSubtitleStream(
            MediaSourceInfo mediaSource,
            MediaStream subtitleStream,
            CancellationToken cancellationToken)
        {
            var fileInfo = await GetReadableFile(mediaSource, subtitleStream, cancellationToken).ConfigureAwait(false);

            var stream = await GetSubtitleStream(fileInfo, cancellationToken).ConfigureAwait(false);

            return (stream, fileInfo.Format);
        }

        private async Task<Stream> GetSubtitleStream(SubtitleInfo fileInfo, CancellationToken cancellationToken)
        {
            if (fileInfo.IsExternal)
            {
                using (var stream = await GetStream(fileInfo.Path, fileInfo.Protocol, cancellationToken).ConfigureAwait(false))
                {
                    var result = CharsetDetector.DetectFromStream(stream).Detected;
                    stream.Position = 0;

                    if (result != null)
                    {
                        _logger.LogDebug("charset {CharSet} detected for {Path}", result.EncodingName, fileInfo.Path);

                        using var reader = new StreamReader(stream, result.Encoding);
                        var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                        return new MemoryStream(Encoding.UTF8.GetBytes(text));
                    }
                }
            }

            return File.OpenRead(fileInfo.Path);
        }

        private async Task<SubtitleInfo> GetReadableFile(
            MediaSourceInfo mediaSource,
            MediaStream subtitleStream,
            CancellationToken cancellationToken)
        {
            if (!subtitleStream.IsExternal)
            {
                string outputFormat;
                string outputCodec;

                if (string.Equals(subtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(subtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(subtitleStream.Codec, "srt", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract
                    outputCodec = "copy";
                    outputFormat = subtitleStream.Codec;
                }
                else if (string.Equals(subtitleStream.Codec, "subrip", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract
                    outputCodec = "copy";
                    outputFormat = "srt";
                }
                else
                {
                    // Extract
                    outputCodec = "srt";
                    outputFormat = "srt";
                }

                // Extract
                var outputPath = GetSubtitleCachePath(mediaSource, subtitleStream.Index, "." + outputFormat);

                await ExtractTextSubtitle(mediaSource, subtitleStream.Index, outputCodec, outputPath, cancellationToken)
                        .ConfigureAwait(false);

                return new SubtitleInfo(outputPath, MediaProtocol.File, outputFormat, false);
            }

            var currentFormat = (Path.GetExtension(subtitleStream.Path) ?? subtitleStream.Codec)
                .TrimStart('.');

            if (GetReader(currentFormat, false) == null)
            {
                // Convert
                var outputPath = GetSubtitleCachePath(mediaSource, subtitleStream.Index, ".srt");

                await ConvertTextSubtitleToSrt(subtitleStream.Path, subtitleStream.Language, mediaSource, outputPath, cancellationToken).ConfigureAwait(false);

                return new SubtitleInfo(outputPath, MediaProtocol.File, "srt", true);
            }

            if (subtitleStream.IsExternal)
            {
                return new SubtitleInfo(subtitleStream.Path, _mediaSourceManager.GetPathProtocol(subtitleStream.Path), currentFormat, true);
            }

            return new SubtitleInfo(subtitleStream.Path, mediaSource.Protocol, currentFormat, true);
        }

        private ISubtitleParser GetReader(string format, bool throwIfMissing)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (string.Equals(format, SubtitleFormat.SRT, StringComparison.OrdinalIgnoreCase))
            {
                return new SrtParser(_logger);
            }

            if (string.Equals(format, SubtitleFormat.SSA, StringComparison.OrdinalIgnoreCase))
            {
                return new SsaParser();
            }

            if (string.Equals(format, SubtitleFormat.ASS, StringComparison.OrdinalIgnoreCase))
            {
                return new AssParser();
            }

            if (throwIfMissing)
            {
                throw new ArgumentException("Unsupported format: " + format);
            }

            return null;
        }

        private ISubtitleWriter TryGetWriter(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                return new JsonWriter();
            }

            if (string.Equals(format, SubtitleFormat.SRT, StringComparison.OrdinalIgnoreCase))
            {
                return new SrtWriter();
            }

            if (string.Equals(format, SubtitleFormat.VTT, StringComparison.OrdinalIgnoreCase))
            {
                return new VttWriter();
            }

            if (string.Equals(format, SubtitleFormat.TTML, StringComparison.OrdinalIgnoreCase))
            {
                return new TtmlWriter();
            }

            return null;
        }

        private ISubtitleWriter GetWriter(string format)
        {
            var writer = TryGetWriter(format);

            if (writer != null)
            {
                return writer;
            }

            throw new ArgumentException("Unsupported format: " + format);
        }

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.Object.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            return _semaphoreLocks.GetOrAdd(filename, _ => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Converts the text subtitle to SRT.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <param name="language">The language.</param>
        /// <param name="mediaSource">The input mediaSource.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task ConvertTextSubtitleToSrt(string inputPath, string language, MediaSourceInfo mediaSource, string outputPath, CancellationToken cancellationToken)
        {
            var semaphore = GetLock(outputPath);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(outputPath))
                {
                    await ConvertTextSubtitleToSrtInternal(inputPath, language, mediaSource, outputPath, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Converts the text subtitle to SRT internal.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <param name="language">The language.</param>
        /// <param name="mediaSource">The input mediaSource.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">
        /// The <c>inputPath</c> or <c>outputPath</c> is <c>null</c>.
        /// </exception>
        private async Task ConvertTextSubtitleToSrtInternal(string inputPath, string language, MediaSourceInfo mediaSource, string outputPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException(nameof(inputPath));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var encodingParam = await GetSubtitleFileCharacterSet(inputPath, language, mediaSource.Protocol, cancellationToken).ConfigureAwait(false);

            // FFmpeg automatically convert character encoding when it is UTF-16
            // If we specify character encoding, it rejects with "do not specify a character encoding" and "Unable to recode subtitle event"
            if ((inputPath.EndsWith(".smi", StringComparison.Ordinal) || inputPath.EndsWith(".sami", StringComparison.Ordinal)) &&
                (encodingParam.Equals("UTF-16BE", StringComparison.OrdinalIgnoreCase) ||
                 encodingParam.Equals("UTF-16LE", StringComparison.OrdinalIgnoreCase)))
            {
                encodingParam = string.Empty;
            }
            else if (!string.IsNullOrEmpty(encodingParam))
            {
                encodingParam = " -sub_charenc " + encodingParam;
            }

            int exitCode;

            using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        FileName = _mediaEncoder.EncoderPath,
                        Arguments = string.Format(CultureInfo.InvariantCulture, "{0} -i \"{1}\" -c:s srt \"{2}\"", encodingParam, inputPath, outputPath),
                        WindowStyle = ProcessWindowStyle.Hidden,
                        ErrorDialog = false
                    },
                    EnableRaisingEvents = true
                })
            {
                _logger.LogInformation("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting ffmpeg");

                    throw;
                }

                var ranToCompletion = await process.WaitForExitAsync(TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (!ranToCompletion)
                {
                    try
                    {
                        _logger.LogInformation("Killing ffmpeg subtitle conversion process");

                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error killing subtitle conversion process");
                    }
                }

                exitCode = ranToCompletion ? process.ExitCode : -1;
            }

            var failed = false;

            if (exitCode == -1)
            {
                failed = true;

                if (File.Exists(outputPath))
                {
                    try
                    {
                        _logger.LogInformation("Deleting converted subtitle due to failure: ", outputPath);
                        _fileSystem.DeleteFile(outputPath);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting converted subtitle {Path}", outputPath);
                    }
                }
            }
            else if (!File.Exists(outputPath))
            {
                failed = true;
            }

            if (failed)
            {
                _logger.LogError("ffmpeg subtitle conversion failed for {Path}", inputPath);

                throw new Exception(
                    string.Format(CultureInfo.InvariantCulture, "ffmpeg subtitle conversion failed for {0}", inputPath));
            }

            await SetAssFont(outputPath, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("ffmpeg subtitle conversion succeeded for {Path}", inputPath);
        }

        /// <summary>
        /// Extracts the text subtitle.
        /// </summary>
        /// <param name="mediaSource">The mediaSource.</param>
        /// <param name="subtitleStreamIndex">Index of the subtitle stream.</param>
        /// <param name="outputCodec">The output codec.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentException">Must use inputPath list overload.</exception>
        private async Task ExtractTextSubtitle(
            MediaSourceInfo mediaSource,
            int subtitleStreamIndex,
            string outputCodec,
            string outputPath,
            CancellationToken cancellationToken)
        {
            var semaphore = GetLock(outputPath);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(outputPath))
                {
                    await ExtractTextSubtitleInternal(
                        _mediaEncoder.GetInputArgument(mediaSource.Path, mediaSource),
                        subtitleStreamIndex,
                        outputCodec,
                        outputPath,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task ExtractTextSubtitleInternal(
            string inputPath,
            int subtitleStreamIndex,
            string outputCodec,
            string outputPath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException(nameof(inputPath));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var processArgs = string.Format(
                CultureInfo.InvariantCulture,
                "-i {0} -map 0:{1} -an -vn -c:s {2} \"{3}\"",
                inputPath,
                subtitleStreamIndex,
                outputCodec,
                outputPath);

            int exitCode;

            using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        FileName = _mediaEncoder.EncoderPath,
                        Arguments = processArgs,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        ErrorDialog = false
                    },
                    EnableRaisingEvents = true
                })
            {
                _logger.LogInformation("{File} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting ffmpeg");

                    throw;
                }

                var ranToCompletion = await process.WaitForExitAsync(TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                if (!ranToCompletion)
                {
                    try
                    {
                        _logger.LogWarning("Killing ffmpeg subtitle extraction process");

                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error killing subtitle extraction process");
                    }
                }

                exitCode = ranToCompletion ? process.ExitCode : -1;
            }

            var failed = false;

            if (exitCode == -1)
            {
                failed = true;

                try
                {
                    _logger.LogWarning("Deleting extracted subtitle due to failure: {Path}", outputPath);
                    _fileSystem.DeleteFile(outputPath);
                }
                catch (FileNotFoundException)
                {
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error deleting extracted subtitle {Path}", outputPath);
                }
            }
            else if (!File.Exists(outputPath))
            {
                failed = true;
            }

            if (failed)
            {
                var msg = $"ffmpeg subtitle extraction failed for {inputPath} to {outputPath}";

                _logger.LogError(msg);

                throw new Exception(msg);
            }
            else
            {
                var msg = $"ffmpeg subtitle extraction completed for {inputPath} to {outputPath}";

                _logger.LogInformation(msg);
            }

            if (string.Equals(outputCodec, "ass", StringComparison.OrdinalIgnoreCase))
            {
                await SetAssFont(outputPath, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets the ass font.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <c>System.Threading.CancellationToken.None</c>.</param>
        /// <returns>Task.</returns>
        private async Task SetAssFont(string file, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Setting ass font within {File}", file);

            string text;
            Encoding encoding;

            using (var fileStream = File.OpenRead(file))
            using (var reader = new StreamReader(fileStream, true))
            {
                encoding = reader.CurrentEncoding;

                text = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            var newText = text.Replace(",Arial,", ",Arial Unicode MS,", StringComparison.Ordinal);

            if (!string.Equals(text, newText, StringComparison.Ordinal))
            {
                // use FileShare.None as this bypasses dotnet bug dotnet/runtime#42790 .
                using (var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(fileStream, encoding))
                {
                    await writer.WriteAsync(newText.AsMemory(), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private string GetSubtitleCachePath(MediaSourceInfo mediaSource, int subtitleStreamIndex, string outputSubtitleExtension)
        {
            if (mediaSource.Protocol == MediaProtocol.File)
            {
                var ticksParam = string.Empty;

                var date = _fileSystem.GetLastWriteTimeUtc(mediaSource.Path);

                ReadOnlySpan<char> filename = (mediaSource.Path + "_" + subtitleStreamIndex.ToString(CultureInfo.InvariantCulture) + "_" + date.Ticks.ToString(CultureInfo.InvariantCulture) + ticksParam).GetMD5() + outputSubtitleExtension;

                var prefix = filename.Slice(0, 1);

                return Path.Join(SubtitleCachePath, prefix, filename);
            }
            else
            {
                ReadOnlySpan<char> filename = (mediaSource.Path + "_" + subtitleStreamIndex.ToString(CultureInfo.InvariantCulture)).GetMD5() + outputSubtitleExtension;

                var prefix = filename.Slice(0, 1);

                return Path.Join(SubtitleCachePath, prefix, filename);
            }
        }

        /// <inheritdoc />
        public async Task<string> GetSubtitleFileCharacterSet(string path, string language, MediaProtocol protocol, CancellationToken cancellationToken)
        {
            using (var stream = await GetStream(path, protocol, cancellationToken).ConfigureAwait(false))
            {
                var charset = CharsetDetector.DetectFromStream(stream).Detected?.EncodingName;

                // UTF16 is automatically converted to UTF8 by FFmpeg, do not specify a character encoding
                if ((path.EndsWith(".ass", StringComparison.Ordinal) || path.EndsWith(".ssa", StringComparison.Ordinal) || path.EndsWith(".srt", StringComparison.Ordinal))
                    && (string.Equals(charset, "utf-16le", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(charset, "utf-16be", StringComparison.OrdinalIgnoreCase)))
                {
                    charset = string.Empty;
                }

                _logger.LogDebug("charset {0} detected for {Path}", charset ?? "null", path);

                return charset;
            }
        }

        private async Task<Stream> GetStream(string path, MediaProtocol protocol, CancellationToken cancellationToken)
        {
            switch (protocol)
            {
                case MediaProtocol.Http:
                {
                    using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                        .GetAsync(new Uri(path), cancellationToken)
                        .ConfigureAwait(false);
                    return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                }

                case MediaProtocol.File:
                    return File.OpenRead(path);
                default:
                    throw new ArgumentOutOfRangeException(nameof(protocol));
            }
        }

        private struct SubtitleInfo
        {
            public SubtitleInfo(string path, MediaProtocol protocol, string format, bool isExternal)
            {
                Path = path;
                Protocol = protocol;
                Format = format;
                IsExternal = isExternal;
            }

            public string Path { get; set; }

            public MediaProtocol Protocol { get; set; }

            public string Format { get; set; }

            public bool IsExternal { get; set; }
        }
    }
}
