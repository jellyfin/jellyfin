#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
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
    public sealed class SubtitleEncoder : ISubtitleEncoder
    {
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
            ILogger<SubtitleEncoder> logger,
            IApplicationPaths appPaths,
            IFileSystem fileSystem,
            IMediaEncoder mediaEncoder,
            IHttpClientFactory httpClientFactory,
            IMediaSourceManager mediaSourceManager)
        {
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
                var reader = GetReader(inputFormat);
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

            var inputFormat = subtitle.Format;

            // Return the original if the same format is being requested
            // Character encoding was already handled in GetSubtitleStream
            if (string.Equals(inputFormat, outputFormat, StringComparison.OrdinalIgnoreCase))
            {
                return subtitle.Stream;
            }

            using (var stream = subtitle.Stream)
            {
                return ConvertSubtitles(stream, inputFormat, outputFormat, startTimeTicks, endTimeTicks, preserveOriginalTimestamps, cancellationToken);
            }
        }

        private async Task<(Stream Stream, string Format)> GetSubtitleStream(
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

            return AsyncFile.OpenRead(fileInfo.Path);
        }

        internal async Task<SubtitleInfo> GetReadableFile(
            MediaSourceInfo mediaSource,
            MediaStream subtitleStream,
            CancellationToken cancellationToken)
        {
            if (!subtitleStream.IsExternal || subtitleStream.Path.EndsWith(".mks", StringComparison.OrdinalIgnoreCase))
            {
                string outputFormat;
                string outputCodec;

                if (string.Equals(subtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(subtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(subtitleStream.Codec, "srt", StringComparison.OrdinalIgnoreCase))
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

                await ExtractTextSubtitle(mediaSource, subtitleStream, outputCodec, outputPath, cancellationToken)
                        .ConfigureAwait(false);

                return new SubtitleInfo(outputPath, MediaProtocol.File, outputFormat, false);
            }

            var currentFormat = (Path.GetExtension(subtitleStream.Path) ?? subtitleStream.Codec)
                .TrimStart('.');

            if (!TryGetReader(currentFormat, out _))
            {
                // Convert
                var outputPath = GetSubtitleCachePath(mediaSource, subtitleStream.Index, ".srt");

                await ConvertTextSubtitleToSrt(subtitleStream, mediaSource, outputPath, cancellationToken).ConfigureAwait(false);

                return new SubtitleInfo(outputPath, MediaProtocol.File, "srt", true);
            }

            // It's possbile that the subtitleStream and mediaSource don't share the same protocol (e.g. .STRM file with local subs)
            return new SubtitleInfo(subtitleStream.Path, _mediaSourceManager.GetPathProtocol(subtitleStream.Path), currentFormat, true);
        }

        private bool TryGetReader(string format, [NotNullWhen(true)] out ISubtitleParser? value)
        {
            if (string.Equals(format, SubtitleFormat.SRT, StringComparison.OrdinalIgnoreCase))
            {
                value = new SrtParser(_logger);
                return true;
            }

            if (string.Equals(format, SubtitleFormat.SSA, StringComparison.OrdinalIgnoreCase))
            {
                value = new SsaParser(_logger);
                return true;
            }

            if (string.Equals(format, SubtitleFormat.ASS, StringComparison.OrdinalIgnoreCase))
            {
                value = new AssParser(_logger);
                return true;
            }

            value = null;
            return false;
        }

        private ISubtitleParser GetReader(string format)
        {
            if (TryGetReader(format, out var reader))
            {
                return reader;
            }

            throw new ArgumentException("Unsupported format: " + format);
        }

        private bool TryGetWriter(string format, [NotNullWhen(true)] out ISubtitleWriter? value)
        {
            if (string.Equals(format, SubtitleFormat.ASS, StringComparison.OrdinalIgnoreCase))
            {
                value = new AssWriter();
                return true;
            }

            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                value = new JsonWriter();
                return true;
            }

            if (string.Equals(format, SubtitleFormat.SRT, StringComparison.OrdinalIgnoreCase) || string.Equals(format, SubtitleFormat.SUBRIP, StringComparison.OrdinalIgnoreCase))
            {
                value = new SrtWriter();
                return true;
            }

            if (string.Equals(format, SubtitleFormat.SSA, StringComparison.OrdinalIgnoreCase))
            {
                value = new SsaWriter();
                return true;
            }

            if (string.Equals(format, SubtitleFormat.VTT, StringComparison.OrdinalIgnoreCase))
            {
                value = new VttWriter();
                return true;
            }

            if (string.Equals(format, SubtitleFormat.TTML, StringComparison.OrdinalIgnoreCase))
            {
                value = new TtmlWriter();
                return true;
            }

            value = null;
            return false;
        }

        private ISubtitleWriter GetWriter(string format)
        {
            if (TryGetWriter(format, out var writer))
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
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <param name="mediaSource">The input mediaSource.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task ConvertTextSubtitleToSrt(MediaStream subtitleStream, MediaSourceInfo mediaSource, string outputPath, CancellationToken cancellationToken)
        {
            var semaphore = GetLock(outputPath);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(outputPath))
                {
                    await ConvertTextSubtitleToSrtInternal(subtitleStream, mediaSource, outputPath, cancellationToken).ConfigureAwait(false);
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
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <param name="mediaSource">The input mediaSource.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">
        /// The <c>inputPath</c> or <c>outputPath</c> is <c>null</c>.
        /// </exception>
        private async Task ConvertTextSubtitleToSrtInternal(MediaStream subtitleStream, MediaSourceInfo mediaSource, string outputPath, CancellationToken cancellationToken)
        {
            var inputPath = subtitleStream.Path;
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException(nameof(inputPath));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath)));

            var encodingParam = await GetSubtitleFileCharacterSet(subtitleStream, subtitleStream.Language, mediaSource, cancellationToken).ConfigureAwait(false);

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

                var ranToCompletion = await process.WaitForExitAsync(TimeSpan.FromMinutes(30)).ConfigureAwait(false);

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

                throw new FfmpegException(
                    string.Format(CultureInfo.InvariantCulture, "ffmpeg subtitle conversion failed for {0}", inputPath));
            }

            await SetAssFont(outputPath, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("ffmpeg subtitle conversion succeeded for {Path}", inputPath);
        }

        /// <summary>
        /// Extracts the text subtitle.
        /// </summary>
        /// <param name="mediaSource">The mediaSource.</param>
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <param name="outputCodec">The output codec.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentException">Must use inputPath list overload.</exception>
        private async Task ExtractTextSubtitle(
            MediaSourceInfo mediaSource,
            MediaStream subtitleStream,
            string outputCodec,
            string outputPath,
            CancellationToken cancellationToken)
        {
            var semaphore = GetLock(outputPath);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            var subtitleStreamIndex = EncodingHelper.FindIndex(mediaSource.MediaStreams, subtitleStream);

            try
            {
                if (!File.Exists(outputPath))
                {
                    var args = _mediaEncoder.GetInputArgument(mediaSource.Path, mediaSource);

                    if (subtitleStream.IsExternal)
                    {
                        args = _mediaEncoder.GetExternalSubtitleInputArgument(subtitleStream.Path);
                    }

                    await ExtractTextSubtitleInternal(
                        args,
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

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath)));

            var processArgs = string.Format(
                CultureInfo.InvariantCulture,
                "-i {0} -copyts -map 0:{1} -an -vn -c:s {2} \"{3}\"",
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

                var ranToCompletion = await process.WaitForExitAsync(TimeSpan.FromMinutes(30)).ConfigureAwait(false);

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
                _logger.LogError("ffmpeg subtitle extraction failed for {InputPath} to {OutputPath}", inputPath, outputPath);

                throw new FfmpegException(
                    string.Format(CultureInfo.InvariantCulture, "ffmpeg subtitle extraction failed for {0} to {1}", inputPath, outputPath));
            }
            else
            {
                _logger.LogInformation("ffmpeg subtitle extraction completed for {InputPath} to {OutputPath}", inputPath, outputPath);
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

            using (var fileStream = AsyncFile.OpenRead(file))
            using (var reader = new StreamReader(fileStream, true))
            {
                encoding = reader.CurrentEncoding;

                text = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            var newText = text.Replace(",Arial,", ",Arial Unicode MS,", StringComparison.Ordinal);

            if (!string.Equals(text, newText, StringComparison.Ordinal))
            {
                var fileStream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);
                await using (fileStream.ConfigureAwait(false))
                {
                    var writer = new StreamWriter(fileStream, encoding);
                    await using (writer.ConfigureAwait(false))
                    {
                        await writer.WriteAsync(newText.AsMemory(), cancellationToken).ConfigureAwait(false);
                    }
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
        public async Task<string> GetSubtitleFileCharacterSet(MediaStream subtitleStream, string language, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            var subtitleCodec = subtitleStream.Codec;
            var path = subtitleStream.Path;

            if (path.EndsWith(".mks", StringComparison.OrdinalIgnoreCase))
            {
                path = GetSubtitleCachePath(mediaSource, subtitleStream.Index, "." + subtitleCodec);
                await ExtractTextSubtitle(mediaSource, subtitleStream, subtitleCodec, path, cancellationToken)
                    .ConfigureAwait(false);
            }

            using (var stream = await GetStream(path, mediaSource.Protocol, cancellationToken).ConfigureAwait(false))
            {
                var charset = CharsetDetector.DetectFromStream(stream).Detected?.EncodingName ?? string.Empty;

                // UTF16 is automatically converted to UTF8 by FFmpeg, do not specify a character encoding
                if ((path.EndsWith(".ass", StringComparison.Ordinal) || path.EndsWith(".ssa", StringComparison.Ordinal) || path.EndsWith(".srt", StringComparison.Ordinal))
                    && (string.Equals(charset, "utf-16le", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(charset, "utf-16be", StringComparison.OrdinalIgnoreCase)))
                {
                    charset = string.Empty;
                }

                _logger.LogDebug("charset {0} detected for {Path}", charset, path);

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
                    return AsyncFile.OpenRead(path);
                default:
                    throw new ArgumentOutOfRangeException(nameof(protocol));
            }
        }

        internal readonly struct SubtitleInfo
        {
            public SubtitleInfo(string path, MediaProtocol protocol, string format, bool isExternal)
            {
                Path = path;
                Protocol = protocol;
                Format = format;
                IsExternal = isExternal;
            }

            public string Path { get; }

            public MediaProtocol Protocol { get; }

            public string Format { get; }

            public bool IsExternal { get; }
        }
    }
}
