#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using UtfUnknown;
using SubtitleFormat = MediaBrowser.Model.MediaInfo.SubtitleFormat;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public sealed class SubtitleEncoder : ISubtitleEncoder, IDisposable
    {
        private readonly ILogger<SubtitleEncoder> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly ISubtitleParser _subtitleParser;
        private readonly IPathManager _pathManager;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        // ASS alignment numbering (numpad layout): 1/4/7 are the bottom/middle/top-LEFT variants.
        private static readonly Dictionary<string, string> _movTextLeftToCenterAlignment = new()
        {
            ["1"] = "2",
            ["4"] = "5",
            ["7"] = "8"
        };

        private static readonly Regex _assStyleLineRegex = new(@"^Style:.*$", RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex _assOverrideAlignmentRegex = new(@"\\an([147])(?=[}\\])", RegexOptions.Compiled);

        private static readonly Regex _assOverrideFontSizeRegex = new(@"\\fs(\d+)", RegexOptions.Compiled);

        /// <summary>
        /// The _semaphoreLocks.
        /// </summary>
        private readonly AsyncKeyedLocker<string> _semaphoreLocks = new(o =>
        {
            o.PoolSize = 20;
            o.PoolInitialFill = 1;
        });

        public SubtitleEncoder(
            ILogger<SubtitleEncoder> logger,
            IFileSystem fileSystem,
            IMediaEncoder mediaEncoder,
            IHttpClientFactory httpClientFactory,
            IMediaSourceManager mediaSourceManager,
            ISubtitleParser subtitleParser,
            IPathManager pathManager,
            IServerConfigurationManager serverConfigurationManager)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _mediaEncoder = mediaEncoder;
            _httpClientFactory = httpClientFactory;
            _mediaSourceManager = mediaSourceManager;
            _subtitleParser = subtitleParser;
            _pathManager = pathManager;
            _serverConfigurationManager = serverConfigurationManager;
        }

        internal MemoryStream ConvertSubtitles(
            Stream stream,
            SubtitleInfo inputInfo,
            string outputFormat,
            long startTimeTicks,
            long endTimeTicks,
            bool preserveOriginalTimestamps)
        {
            var subtitle = _subtitleParser.Parse(stream, inputInfo.Format);

            FilterEvents(subtitle, startTimeTicks, endTimeTicks, preserveOriginalTimestamps);

            var formatter = GetWriter(outputFormat);

            var text = formatter.ToText(subtitle, "untitled");
            var bytes = Encoding.UTF8.GetBytes(text);

            return new MemoryStream(bytes, 0, bytes.Length, false, true);
        }

        internal void FilterEvents(Subtitle track, long startPositionTicks, long endTimeTicks, bool preserveTimestamps)
        {
            // Drop subs that have fully elapsed before the requested start position
            track.Paragraphs
                .RemoveAll(i => (i.StartTime.TimeSpan.Ticks - startPositionTicks) < 0 && (i.EndTime.TimeSpan.Ticks - startPositionTicks) < 0);

            if (endTimeTicks > 0)
            {
                track.Paragraphs
                    .RemoveAll(i => i.StartTime.TimeSpan.Ticks > endTimeTicks);
            }

            if (!preserveTimestamps)
            {
                foreach (var trackEvent in track.Paragraphs)
                {
                    trackEvent.StartTime = new TimeCode(TimeSpan.FromTicks(Math.Max(0, trackEvent.StartTime.TimeSpan.Ticks - startPositionTicks)));
                    trackEvent.EndTime = new TimeCode(TimeSpan.FromTicks(Math.Max(0, trackEvent.EndTime.TimeSpan.Ticks - startPositionTicks)));
                }
            }
        }

        async Task<Stream> ISubtitleEncoder.GetSubtitles(BaseItem item, string mediaSourceId, int subtitleStreamIndex, string outputFormat, long startTimeTicks, long endTimeTicks, bool preserveOriginalTimestamps, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (string.IsNullOrWhiteSpace(mediaSourceId))
            {
                throw new ArgumentNullException(nameof(mediaSourceId));
            }

            var mediaSources = await _mediaSourceManager.GetPlaybackMediaSources(item, null, true, false, cancellationToken).ConfigureAwait(false);

            var mediaSource = mediaSources
                .First(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase));

            var subtitleStream = mediaSource.MediaStreams
               .First(i => i.Type == MediaStreamType.Subtitle && i.Index == subtitleStreamIndex);

            var (stream, info) = await GetSubtitleStream(mediaSource, subtitleStream, cancellationToken)
                        .ConfigureAwait(false);

            // Return the original if the same format is being requested
            // Character encoding was already handled in GetSubtitleStream
            // ASS is a superset of SSA, skipping the conversion and preserving the styles
            if (string.Equals(info.Format, outputFormat, StringComparison.OrdinalIgnoreCase)
                || (string.Equals(info.Format, SubtitleFormat.SSA, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(outputFormat, SubtitleFormat.ASS, StringComparison.OrdinalIgnoreCase)))
            {
                return stream;
            }

            using (stream)
            {
                return ConvertSubtitles(stream, info, outputFormat, startTimeTicks, endTimeTicks, preserveOriginalTimestamps);
            }
        }

        private async Task<(Stream Stream, SubtitleInfo Info)> GetSubtitleStream(
            MediaSourceInfo mediaSource,
            MediaStream subtitleStream,
            CancellationToken cancellationToken)
        {
            var fileInfo = await GetReadableFile(mediaSource, subtitleStream, cancellationToken).ConfigureAwait(false);

            var stream = await GetSubtitleStream(fileInfo, cancellationToken).ConfigureAwait(false);

            return (stream, fileInfo);
        }

        internal async Task<Stream> GetSubtitleStream(SubtitleInfo fileInfo, CancellationToken cancellationToken)
        {
            if (fileInfo.IsExternal && MediaStream.IsTextFormat(fileInfo.Format))
            {
                var result = await DetectCharset(fileInfo.Path, cancellationToken).ConfigureAwait(false);
                var detected = result.Detected;

                var stream = fileInfo.Protocol == MediaProtocol.Http
                    ? await _httpClientFactory.CreateClient(NamedClient.Default)
                        .GetStreamAsync(new Uri(fileInfo.Path), cancellationToken)
                        .ConfigureAwait(false)
                    : AsyncFile.OpenRead(fileInfo.Path);

                // Short-circuit when the file is already UTF-8/ASCII.
                if (detected is null
                    || string.Equals(detected.EncodingName, "utf-8", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(detected.EncodingName, "ascii", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(detected.EncodingName, "us-ascii", StringComparison.OrdinalIgnoreCase))
                {
                    return stream;
                }

                _logger.LogDebug("charset {CharSet} detected for {Path}", detected.EncodingName, fileInfo.Path);

                await using (stream.ConfigureAwait(false))
                {
                    using var reader = new StreamReader(stream, detected.Encoding);
                    var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

                    return new MemoryStream(Encoding.UTF8.GetBytes(text));
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
                await ExtractAllExtractableSubtitles(mediaSource, cancellationToken).ConfigureAwait(false);

                var outputFileExtension = GetExtractableSubtitleFileExtension(subtitleStream);
                var outputFormat = GetExtractableSubtitleFormat(subtitleStream);
                var outputPath = GetSubtitleCachePath(mediaSource, subtitleStream.Index, "." + outputFileExtension)
                    ?? throw new ResourceNotFoundException($"MediaSource {mediaSource.Id} has no subtitle cache (non-GUID Id, e.g. Live TV stream).");

                return new SubtitleInfo()
                {
                    Path = outputPath,
                    Protocol = MediaProtocol.File,
                    Format = outputFormat,
                    IsExternal = MediaStream.IsVobSubFormat(outputFormat)
                };
            }

            // Normalize ffmpeg codec names to the file extensions the parser is keyed on
            var currentFormat = NormalizeCodecToParserExtension((Path.GetExtension(subtitleStream.Path) ?? subtitleStream.Codec).TrimStart('.'));

            // Handle PGS subtitles as raw streams for the client to render
            if (MediaStream.IsPgsFormat(currentFormat))
            {
                return new SubtitleInfo()
                {
                    Path = subtitleStream.Path,
                    Protocol = _mediaSourceManager.GetPathProtocol(subtitleStream.Path),
                    Format = "pgssub",
                    IsExternal = true
                };
            }

            // Fallback to ffmpeg conversion
            if (!_subtitleParser.SupportsFileExtension(currentFormat))
            {
                // Convert
                var outputPath = GetSubtitleCachePath(mediaSource, subtitleStream.Index, ".srt")
                    ?? throw new ResourceNotFoundException($"MediaSource {mediaSource.Id} has no subtitle cache (non-GUID Id, e.g. Live TV stream).");

                await ConvertTextSubtitleToSrt(subtitleStream, mediaSource, outputPath, cancellationToken).ConfigureAwait(false);

                return new SubtitleInfo()
                {
                    Path = outputPath,
                    Protocol = MediaProtocol.File,
                    Format = "srt",
                    IsExternal = true
                };
            }

            // It's possible that the subtitleStream and mediaSource don't share the same protocol (e.g. .STRM file with local subs)
            return new SubtitleInfo()
            {
                Path = subtitleStream.Path,
                Protocol = _mediaSourceManager.GetPathProtocol(subtitleStream.Path),
                Format = currentFormat,
                IsExternal = true
            };
        }

        private bool TryGetWriter(string format, [NotNullWhen(true)] out Nikse.SubtitleEdit.Core.SubtitleFormats.SubtitleFormat? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(format);

            if (string.Equals(format, SubtitleFormat.ASS, StringComparison.OrdinalIgnoreCase))
            {
                value = new AdvancedSubStationAlpha();
                return true;
            }

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                value = new JsonWriter();
                return true;
            }

            if (string.Equals(format, SubtitleFormat.SRT, StringComparison.OrdinalIgnoreCase)
                || string.Equals(format, SubtitleFormat.SUBRIP, StringComparison.OrdinalIgnoreCase))
            {
                value = new SubRip();
                return true;
            }

            if (string.Equals(format, SubtitleFormat.SSA, StringComparison.OrdinalIgnoreCase))
            {
                value = new SubStationAlpha();
                return true;
            }

            if (string.Equals(format, SubtitleFormat.VTT, StringComparison.OrdinalIgnoreCase)
                || string.Equals(format, SubtitleFormat.WEBVTT, StringComparison.OrdinalIgnoreCase))
            {
                value = new WebVTT();
                return true;
            }

            if (string.Equals(format, SubtitleFormat.TTML, StringComparison.OrdinalIgnoreCase))
            {
                value = new TimedText10();
                return true;
            }

            value = null;
            return false;
        }

        private Nikse.SubtitleEdit.Core.SubtitleFormats.SubtitleFormat GetWriter(string format)
        {
            if (TryGetWriter(format, out var writer))
            {
                return writer;
            }

            throw new ArgumentException("Unsupported format: " + format);
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
            using (await _semaphoreLocks.LockAsync(outputPath, cancellationToken).ConfigureAwait(false))
            {
                if (!IsCachedSubtitleFresh(outputPath, subtitleStream.Path))
                {
                    await ConvertTextSubtitleToSrtInternal(subtitleStream, mediaSource, outputPath, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // ffmpeg codec names don't always match the file extensions the subtitle parser is keyed on.
        private static string NormalizeCodecToParserExtension(string codecOrExtension)
        {
            return codecOrExtension switch
            {
                "subrip" => "srt",
                "webvtt" => "vtt",
                _ => codecOrExtension
            };
        }

        // Records "this cache was built from this exact source revision" in a sidecar file next to the cache: "<sizeBytes>:<mtimeTicks>"
        private static string GetCacheMetaPath(string cachePath) => cachePath + ".meta";

        private static string FormatCacheMeta(long length, DateTime lastWriteUtc)
            => string.Create(CultureInfo.InvariantCulture, $"{length}:{lastWriteUtc.Ticks}");

        private bool IsCachedSubtitleFresh(string cachePath, string? sourcePath)
        {
            if (!File.Exists(cachePath))
            {
                return false;
            }

            var cacheInfo = _fileSystem.GetFileInfo(cachePath);
            if (cacheInfo.Length == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                return true;
            }

            var metaPath = GetCacheMetaPath(cachePath);
            if (!File.Exists(metaPath))
            {
                // Pre-existing cache from before metadata tracking - regenerate so we can record the source state.
                return false;
            }

            try
            {
                var sourceInfo = _fileSystem.GetFileInfo(sourcePath);
                var expected = FormatCacheMeta(sourceInfo.Length, sourceInfo.LastWriteTimeUtc);
                var actual = File.ReadAllText(metaPath);
                return string.Equals(expected, actual, StringComparison.Ordinal);
            }
            catch (IOException)
            {
                return false;
            }
        }

        private void WriteCacheMeta(string cachePath, string? sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                return;
            }

            try
            {
                var sourceInfo = _fileSystem.GetFileInfo(sourcePath);
                if (!sourceInfo.Exists)
                {
                    return;
                }

                File.WriteAllText(GetCacheMetaPath(cachePath), FormatCacheMeta(sourceInfo.Length, sourceInfo.LastWriteTimeUtc));
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to record subtitle cache metadata for {CachePath}", cachePath);
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
            ArgumentException.ThrowIfNullOrEmpty(inputPath);

            ArgumentException.ThrowIfNullOrEmpty(outputPath);

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

            var args = string.Format(CultureInfo.InvariantCulture, "-y {0} -i \"{1}\" -c:s srt \"{2}\"", encodingParam, inputPath, outputPath);

            await ExtractSubtitlesForFile(
                inputPath,
                args,
                [outputPath],
                cancellationToken).ConfigureAwait(false);

            WriteCacheMeta(outputPath, inputPath);
        }

        private string GetExtractableSubtitleFormat(MediaStream subtitleStream)
        {
            if (string.Equals(subtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase)
                || string.Equals(subtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase)
                || string.Equals(subtitleStream.Codec, "pgssub", StringComparison.OrdinalIgnoreCase))
            {
                return subtitleStream.Codec;
            }
            else if (MediaStream.IsVobSubFormat(subtitleStream.Codec))
            {
                return "mks";
            }
            else if (string.Equals(subtitleStream.Codec, "mov_text", StringComparison.OrdinalIgnoreCase))
            {
                // mov_text (tx3g) embeds an absolute per-style font size with no reference
                // resolution of its own. Extracting to plain SubRip loses that context
                // entirely; libavcodec's mov_text decoder needs to be told the real video
                // frame size (see the -width/-height args added in
                // ExtractAllExtractableSubtitlesInternal) and the result kept as ASS so the
                // font size is interpreted against the correct PlayResX/PlayResY instead of
                // whatever the burn-in filter would otherwise default to.
                return "ass";
            }
            else
            {
                return "srt";
            }
        }

        private string GetExtractableSubtitleFileExtension(MediaStream subtitleStream)
        {
            // Using .pgssub as file extension is not allowed by ffmpeg. The file extension for pgs subtitles is .sup.
            if (string.Equals(subtitleStream.Codec, "pgssub", StringComparison.OrdinalIgnoreCase))
            {
                return "sup";
            }
            else if (MediaStream.IsVobSubFormat(subtitleStream.Codec))
            {
                // FFmpeg cannot mux VobSub subtitle streams back into the .idx/.sub pair, so we use .mks container instead.
                return "mks";
            }
            else
            {
                return GetExtractableSubtitleFormat(subtitleStream);
            }
        }

        private bool IsCodecCopyable(string codec)
        {
            return string.Equals(codec, "ass", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "ssa", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "srt", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "subrip", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "pgssub", StringComparison.OrdinalIgnoreCase)
                || MediaStream.IsVobSubFormat(codec);
        }

        /// <inheritdoc />
        public async Task ExtractAllExtractableSubtitles(MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            var locks = new List<IDisposable>();
            var extractableStreams = new List<MediaStream>();

            try
            {
                var subtitleStreams = mediaSource.MediaStreams
                    .Where(stream => stream is { IsExtractableSubtitleStream: true, SupportsExternalStream: true });

                foreach (var subtitleStream in subtitleStreams)
                {
                    if (subtitleStream.IsExternal
                        && !subtitleStream.Path.EndsWith(".mks", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var outputPath = GetSubtitleCachePath(mediaSource, subtitleStream.Index, "." + GetExtractableSubtitleFileExtension(subtitleStream));
                    if (outputPath is null)
                    {
                        continue;
                    }

                    var releaser = await _semaphoreLocks.LockAsync(outputPath, cancellationToken).ConfigureAwait(false);

                    var sourcePath = string.IsNullOrEmpty(subtitleStream.Path) ? mediaSource.Path : subtitleStream.Path;
                    if (IsCachedSubtitleFresh(outputPath, sourcePath))
                    {
                        releaser.Dispose();
                        continue;
                    }

                    locks.Add(releaser);
                    extractableStreams.Add(subtitleStream);
                }

                if (extractableStreams.Count > 0)
                {
                    await ExtractAllExtractableSubtitlesInternal(mediaSource, extractableStreams, cancellationToken).ConfigureAwait(false);
                    await ExtractAllExtractableSubtitlesMKS(mediaSource, extractableStreams, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to get streams for File:{File}", mediaSource.Path);
            }
            finally
            {
                locks.ForEach(x => x.Dispose());
            }
        }

        private async Task ExtractAllExtractableSubtitlesMKS(
           MediaSourceInfo mediaSource,
           List<MediaStream> subtitleStreams,
           CancellationToken cancellationToken)
        {
            var mksFiles = new List<string>();

            foreach (var subtitleStream in subtitleStreams)
            {
                if (string.IsNullOrEmpty(subtitleStream.Path) || !subtitleStream.Path.EndsWith(".mks", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!mksFiles.Contains(subtitleStream.Path))
                {
                    mksFiles.Add(subtitleStream.Path);
                }
            }

            if (mksFiles.Count == 0)
            {
                return;
            }

            foreach (string mksFile in mksFiles)
            {
                var inputPath = _mediaEncoder.GetInputArgument(mksFile, mediaSource);
                var outputPaths = new List<string>();
                var args = string.Format(
                    CultureInfo.InvariantCulture,
                    "-y -i {0}",
                    inputPath);

                foreach (var subtitleStream in subtitleStreams)
                {
                    if (!subtitleStream.Path.Equals(mksFile, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var outputPath = GetSubtitleCachePath(mediaSource, subtitleStream.Index, "." + GetExtractableSubtitleFileExtension(subtitleStream));
                    if (outputPath is null)
                    {
                        continue;
                    }

                    var outputCodec = IsCodecCopyable(subtitleStream.Codec) ? "copy" : "srt";
                    // FFmpeg does not provide an .idx/.sub muxer, so VobSub streams must be written as MKS files.
                    var outputFormatOption = MediaStream.IsVobSubFormat(subtitleStream.Codec) ? " -f matroska" : string.Empty;
                    var streamIndex = EncodingHelper.FindIndex(mediaSource.MediaStreams, subtitleStream);

                    if (streamIndex == -1)
                    {
                        _logger.LogError("Cannot find subtitle stream index for {InputPath} ({Index}), skipping this stream", inputPath, subtitleStream.Index);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new FileNotFoundException($"Calculated path ({outputPath}) is not valid."));

                    outputPaths.Add(outputPath);
                    args += string.Format(
                        CultureInfo.InvariantCulture,
                        " -map 0:{0} -an -vn -c:s {1}{2} -flush_packets 1 \"{3}\"",
                        streamIndex,
                        outputCodec,
                        outputFormatOption,
                        outputPath);
                }

                await ExtractSubtitlesForFile(inputPath, args, outputPaths, cancellationToken).ConfigureAwait(false);

                foreach (var outputPath in outputPaths)
                {
                    WriteCacheMeta(outputPath, mksFile);
                }
            }
        }

        private async Task ExtractAllExtractableSubtitlesInternal(
            MediaSourceInfo mediaSource,
            List<MediaStream> subtitleStreams,
            CancellationToken cancellationToken)
        {
            var inputPath = _mediaEncoder.GetInputArgument(mediaSource.Path, mediaSource);
            var outputPaths = new List<string>();
            var movTextOutputPaths = new List<string>();
            var inputOptions = new StringBuilder();
            var mapOptions = new StringBuilder();
            var videoStream = mediaSource.VideoStream;

            foreach (var subtitleStream in subtitleStreams)
            {
                if (!string.IsNullOrEmpty(subtitleStream.Path) && subtitleStream.Path.EndsWith(".mks", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Subtitle {Index} for file {InputPath} is part in an MKS file. Skipping", inputPath, subtitleStream.Index);
                    continue;
                }

                var outputPath = GetSubtitleCachePath(mediaSource, subtitleStream.Index, "." + GetExtractableSubtitleFileExtension(subtitleStream));
                if (outputPath is null)
                {
                    continue;
                }

                var isMovText = string.Equals(subtitleStream.Codec, "mov_text", StringComparison.OrdinalIgnoreCase);
                var outputCodec = IsCodecCopyable(subtitleStream.Codec) ? "copy" : GetExtractableSubtitleFormat(subtitleStream);
                // FFmpeg does not provide an .idx/.sub muxer, so VobSub streams must be written as MKS files.
                var outputFormatOption = MediaStream.IsVobSubFormat(subtitleStream.Codec) ? " -f matroska" : string.Empty;
                var streamIndex = EncodingHelper.FindIndex(mediaSource.MediaStreams, subtitleStream);

                if (streamIndex == -1)
                {
                    _logger.LogError("Cannot find subtitle stream index for {InputPath} ({Index}), skipping this stream", inputPath, subtitleStream.Index);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new FileNotFoundException($"Calculated path ({outputPath}) is not valid."));

                // The mov_text (tx3g) decoder embeds an absolute font size authored against the
                // real video's pixel dimensions, but falls back to a 384x288 reference if it isn't
                // told the actual frame size, which inflates burned-in text several times over.
                // https://github.com/FFmpeg/FFmpeg/blob/master/libavcodec/movtextdec.c
                if (isMovText && videoStream is { Width: > 0, Height: > 0 })
                {
                    inputOptions.Append(CultureInfo.InvariantCulture, $" -width:{streamIndex} {videoStream.Width} -height:{streamIndex} {videoStream.Height}");
                    movTextOutputPaths.Add(outputPath);
                }

                outputPaths.Add(outputPath);
                mapOptions.Append(CultureInfo.InvariantCulture, $" -map 0:{streamIndex} -an -vn -c:s {outputCodec}{outputFormatOption} -flush_packets 1 \"{outputPath}\"");
            }

            var args = string.Format(CultureInfo.InvariantCulture, "-y{0} -i {1}{2}", inputOptions, inputPath, mapOptions);

            if (outputPaths.Count > 0)
            {
                await ExtractSubtitlesForFile(inputPath, args, outputPaths, cancellationToken).ConfigureAwait(false);

                // Only mov_text outputs land in movTextOutputPaths, and ExtractSubtitlesForFile
                // throws unless every output was written, so each of these is guaranteed to
                // exist by this point. Any other subtitle stream of the same file is untouched.
                if (videoStream is { Height: > 0 } movTextReference)
                {
                    foreach (var movTextOutputPath in movTextOutputPaths)
                    {
                        await FixMovTextStyle(movTextOutputPath, movTextReference.Height.Value, cancellationToken).ConfigureAwait(false);
                    }
                }

                foreach (var outputPath in outputPaths)
                {
                    WriteCacheMeta(outputPath, mediaSource.Path);
                }
            }
        }

        private async Task ExtractSubtitlesForFile(
            string inputPath,
            string args,
            IReadOnlyList<string> outputPaths,
            CancellationToken cancellationToken)
        {
            var (exitCode, ffmpegError) = await RunSubtitleExtractionProcess(args, cancellationToken).ConfigureAwait(false);

            var failed = false;

            if (exitCode == -1)
            {
                failed = true;

                foreach (var outputPath in outputPaths)
                {
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
            }
            else
            {
                foreach (var outputPath in outputPaths)
                {
                    if (!File.Exists(outputPath) || _fileSystem.GetFileInfo(outputPath).Length == 0)
                    {
                        _logger.LogError("ffmpeg subtitle extraction failed for {InputPath} to {OutputPath}", inputPath, outputPath);
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

                        continue;
                    }

                    if (outputPath.EndsWith("ass", StringComparison.OrdinalIgnoreCase))
                    {
                        await SetAssFont(outputPath, cancellationToken).ConfigureAwait(false);
                    }

                    _logger.LogInformation("ffmpeg subtitle extraction completed for {InputPath} to {OutputPath}", inputPath, outputPath);
                }
            }

            if (failed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(ffmpegError))
                {
                    _logger.LogError("ffmpeg subtitle extraction failed for {InputPath}: {FfmpegOutput}", inputPath, ffmpegError);
                }

                throw new FfmpegException(
                    string.Format(CultureInfo.InvariantCulture, "ffmpeg subtitle extraction failed for {0}", inputPath));
            }
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
            using (await _semaphoreLocks.LockAsync(outputPath, cancellationToken).ConfigureAwait(false))
            {
                if (!File.Exists(outputPath) || _fileSystem.GetFileInfo(outputPath).Length == 0)
                {
                    var subtitleStreamIndex = EncodingHelper.FindIndex(mediaSource.MediaStreams, subtitleStream);

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
        }

        private async Task ExtractTextSubtitleInternal(
            string inputPath,
            int subtitleStreamIndex,
            string outputCodec,
            string outputPath,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(inputPath);

            ArgumentException.ThrowIfNullOrEmpty(outputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath)));
            var processArgs = string.Format(
                CultureInfo.InvariantCulture,
                "-y -i {0} -copyts -map 0:{1} -an -vn -c:s {2} \"{3}\"",
                inputPath,
                subtitleStreamIndex,
                outputCodec,
                outputPath);

            await ExtractSubtitlesForFile(
                inputPath,
                processArgs,
                [outputPath],
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs ffmpeg to extract or convert subtitles, capturing its exit code and stderr output.
        /// </summary>
        /// <remarks>
        /// stdin is redirected and closed, and <c>-nostdin</c> is prepended to the arguments, so ffmpeg can never
        /// block reading an inherited stdin handle (which happens when Jellyfin runs as a service, e.g. under NSSM,
        /// and stalls subtitle extraction until the timeout). stderr is redirected and drained so a full pipe buffer
        /// cannot deadlock ffmpeg and so its output can be surfaced on failure; stdout is left un-redirected as it is
        /// unused for subtitle extraction.
        /// </remarks>
        /// <param name="arguments">The ffmpeg command line arguments.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The ffmpeg exit code (-1 on timeout) and its captured stderr output.</returns>
        private async Task<(int ExitCode, string StandardError)> RunSubtitleExtractionProcess(string arguments, CancellationToken cancellationToken)
        {
            int exitCode;
            var standardError = string.Empty;

            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    FileName = _mediaEncoder.EncoderPath,
                    Arguments = "-nostdin " + arguments,
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

                // Close stdin so ffmpeg observes EOF instead of blocking on an inherited handle.
                process.StandardInput.Close();

                // Begin draining stderr before waiting for exit; a full stderr pipe buffer would otherwise deadlock ffmpeg.
                var standardErrorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
                var timeoutMinutes = _serverConfigurationManager.GetEncodingOptions().SubtitleExtractionTimeoutMinutes;
                using var waitSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                waitSource.CancelAfter(TimeSpan.FromMinutes(timeoutMinutes));

                try
                {
                    await process.WaitForExitAsync(waitSource.Token).ConfigureAwait(false);
                    exitCode = process.ExitCode;
                }
                catch (OperationCanceledException)
                {
                    process.Kill(true);
                    exitCode = -1;
                }

                try
                {
                    standardError = await standardErrorTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Reading ffmpeg output was cancelled; nothing more to capture.
                }
            }

            return (exitCode, standardError);
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

                text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// ffmpeg's mov_text decoder has two fidelity gaps compared to reference tx3g
        /// renderers (e.g. VLC's modules/codec/substx3g.c):
        /// (1) it treats the track's embedded font size as an absolute ASS Fontsize, but
        /// VLC's own FontSizeConvert() always renders the sample description's default
        /// style at a fixed 5% of the frame height ("as the line should always be 5%") -
        /// the embedded byte only expresses relative size between text runs on the same
        /// line, not an absolute size, so ffmpeg's interpretation renders noticeably
        /// smaller or larger than reference players depending on what that byte happens
        /// to be; and
        /// (2) it maps the track's default text-box justification straight onto ASS
        /// alignment while ignoring the box's actual position/extent, which for
        /// left-justified tracks renders noticeably left of where VLC places the same
        /// (in practice centered) text.
        /// This re-derives the Style's Fontsize/MarginV from the real video height and
        /// centers left-column alignment, both in the Style definition and in any
        /// per-line override.
        /// </summary>
        /// <param name="file">The extracted .ass file path.</param>
        /// <param name="videoHeight">The coded height of the video the track was authored against.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        private async Task FixMovTextStyle(string file, int videoHeight, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Normalizing mov_text style within {File}", file);

            string text;
            Encoding encoding;

            using (var fileStream = AsyncFile.OpenRead(file))
            using (var reader = new StreamReader(fileStream, true))
            {
                encoding = reader.CurrentEncoding;

                text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            }

            var newText = NormalizeMovTextAss(text, videoHeight);

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

        /// <summary>
        /// Only public for the unit tests. See <see cref="FixMovTextStyle"/>.
        /// </summary>
        /// <param name="assText">The contents of an .ass file produced from a mov_text track.</param>
        /// <param name="videoHeight">The coded height of the video the track was authored against.</param>
        /// <returns>The text with a VLC-equivalent font size, margin and centered alignment.</returns>
        public static string NormalizeMovTextAss(string assText, int videoHeight)
        {
            // VLC's own tx3g decoder documents its default style as always rendering at
            // "5%" of the frame height (modules/codec/substx3g.c, FontSizeConvert), but
            // that percentage is measured on VLC's internal text metrics, not on libass's
            // Fontsize/PlayResY ratio -- burning the two at the same raw "5" produces
            // visibly different glyph heights. The 0.121 ratio below was calibrated by
            // measuring VLC's rendered glyph height against libass's for the same PlayResY
            // on macOS (Helvetica Neue); on the actual deployment target (Linux, whatever
            // font "Arial Unicode MS" resolves to via fontconfig) it rendered roughly twice
            // too large, so it's halved here (0.121 / 2 = 0.0605) pending a font-specific
            // recalibration.
            var targetFontSize = (int)Math.Round(videoHeight * 0.0605, MidpointRounding.AwayFromZero);
            var targetMarginV = (int)Math.Round(videoHeight * 0.0231, MidpointRounding.AwayFromZero);
            // ffmpeg's mov_text default style always carries an Outline of 1, authored for
            // the small original Fontsize -- left untouched, the outline becomes visibly
            // too thin relative to the corrected (larger) font and loses the crisp
            // black-outlined look most reference renderers (including VLC) give tx3g text.
            // Scale it with the corrected font size using a standard ~6% stroke-to-em ratio.
            var targetOutline = Math.Max(1, (int)Math.Round(targetFontSize * 0.06, MidpointRounding.AwayFromZero));
            var originalFontSize = 0;

            var newText = _assStyleLineRegex.Replace(assText, m =>
            {
                var fields = m.Value.Split(',');
                if (fields.Length < 23)
                {
                    return m.Value;
                }

                if (int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var fontSize))
                {
                    originalFontSize = fontSize;
                }

                fields[2] = targetFontSize.ToString(CultureInfo.InvariantCulture);
                // ffmpeg writes OutlineColour/BackColour as 8-digit &HAABBGGRR with the alpha
                // byte set to "ff". ASS uses an inverted alpha (00 = opaque, ff = fully
                // transparent), so this renders the outline and shadow completely invisible
                // regardless of Outline/Shadow width. PrimaryColour/SecondaryColour are
                // unaffected because ffmpeg writes those as 6-digit RRGGBB with no alpha byte.
                fields[5] = "&H00000000";
                fields[6] = "&H00000000";
                fields[16] = targetOutline.ToString(CultureInfo.InvariantCulture);
                fields[21] = targetMarginV.ToString(CultureInfo.InvariantCulture);

                if (_movTextLeftToCenterAlignment.TryGetValue(fields[18], out var centeredAlignment))
                {
                    fields[18] = centeredAlignment;
                }

                return string.Join(',', fields);
            });

            // Per-line {\fsNN} overrides carry the original absolute value; rescale them
            // proportionally so their size relative to the new default is preserved.
            if (originalFontSize > 0 && originalFontSize != targetFontSize)
            {
                newText = _assOverrideFontSizeRegex.Replace(newText, m =>
                {
                    var originalOverride = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    var scaled = (int)Math.Round(targetFontSize * (double)originalOverride / originalFontSize, MidpointRounding.AwayFromZero);
                    return "\\fs" + scaled.ToString(CultureInfo.InvariantCulture);
                });
            }

            return _assOverrideAlignmentRegex.Replace(
                newText,
                m => "\\an" + _movTextLeftToCenterAlignment[m.Groups[1].Value]);
        }

        private string? GetSubtitleCachePath(MediaSourceInfo mediaSource, int subtitleStreamIndex, string outputSubtitleExtension)
        {
            return _pathManager.GetSubtitlePath(mediaSource.Id, subtitleStreamIndex, outputSubtitleExtension);
        }

        /// <inheritdoc />
        public async Task<string> GetSubtitleFileCharacterSet(MediaStream subtitleStream, string language, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            var subtitleCodec = subtitleStream.Codec;
            var path = subtitleStream.Path;

            if (path.EndsWith(".mks", StringComparison.OrdinalIgnoreCase))
            {
                var cachePath = GetSubtitleCachePath(mediaSource, subtitleStream.Index, "." + subtitleCodec);
                if (cachePath is not null)
                {
                    path = cachePath;
                    await ExtractTextSubtitle(mediaSource, subtitleStream, subtitleCodec, path, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            var result = await DetectCharset(path, cancellationToken).ConfigureAwait(false);
            var charset = result.Detected?.EncodingName ?? string.Empty;

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

        private async Task<DetectionResult> DetectCharset(string path, CancellationToken cancellationToken)
        {
            var protocol = _mediaSourceManager.GetPathProtocol(path);
            switch (protocol)
            {
                case MediaProtocol.Http:
                    {
                        using var stream = await _httpClientFactory
                          .CreateClient(NamedClient.Default)
                          .GetStreamAsync(new Uri(path), cancellationToken)
                          .ConfigureAwait(false);

                        return await CharsetDetector.DetectFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                    }

                case MediaProtocol.File:
                    {
                        return await CharsetDetector.DetectFromFileAsync(path, cancellationToken)
                                              .ConfigureAwait(false);
                    }

                default:
                    throw new NotSupportedException($"Unsupported protocol: {protocol}");
            }
        }

        public async Task<string> GetSubtitleFilePath(MediaStream subtitleStream, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            var info = await GetReadableFile(mediaSource, subtitleStream, cancellationToken)
                .ConfigureAwait(false);
            return info.Path;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _semaphoreLocks.Dispose();
        }

#pragma warning disable CA1034 // Nested types should not be visible
        // Only public for the unit tests
        public readonly record struct SubtitleInfo
        {
            public string Path { get; init; }

            public MediaProtocol Protocol { get; init; }

            public string Format { get; init; }

            public bool IsExternal { get; init; }
        }
    }
}
