#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Attachments
{
    public sealed class AttachmentExtractor : IAttachmentExtractor
    {
        private readonly ILogger<AttachmentExtractor> _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IMediaSourceManager _mediaSourceManager;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        public AttachmentExtractor(
            ILogger<AttachmentExtractor> logger,
            IApplicationPaths appPaths,
            IFileSystem fileSystem,
            IMediaEncoder mediaEncoder,
            IMediaSourceManager mediaSourceManager)
        {
            _logger = logger;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _mediaEncoder = mediaEncoder;
            _mediaSourceManager = mediaSourceManager;
        }

        /// <inheritdoc />
        public async Task<(MediaAttachment Attachment, Stream Stream)> GetAttachment(BaseItem item, string mediaSourceId, int attachmentStreamIndex, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (string.IsNullOrWhiteSpace(mediaSourceId))
            {
                throw new ArgumentNullException(nameof(mediaSourceId));
            }

            var mediaSources = await _mediaSourceManager.GetPlaybackMediaSources(item, null, true, false, cancellationToken).ConfigureAwait(false);
            var mediaSource = mediaSources
                .FirstOrDefault(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase));
            if (mediaSource is null)
            {
                throw new ResourceNotFoundException($"MediaSource {mediaSourceId} not found");
            }

            var mediaAttachment = mediaSource.MediaAttachments
                .FirstOrDefault(i => i.Index == attachmentStreamIndex);
            if (mediaAttachment is null)
            {
                throw new ResourceNotFoundException($"MediaSource {mediaSourceId} has no attachment with stream index {attachmentStreamIndex}");
            }

            var attachmentStream = await GetAttachmentStream(mediaSource, mediaAttachment, cancellationToken)
                    .ConfigureAwait(false);

            return (mediaAttachment, attachmentStream);
        }

        public async Task ExtractAllAttachments(
            string inputFile,
            MediaSourceInfo mediaSource,
            string outputPath,
            CancellationToken cancellationToken)
        {
            var semaphore = _semaphoreLocks.GetOrAdd(outputPath, key => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!Directory.Exists(outputPath))
                {
                    await ExtractAllAttachmentsInternal(
                        _mediaEncoder.GetInputArgument(inputFile, mediaSource),
                        outputPath,
                        false,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task ExtractAllAttachmentsExternal(
            string inputArgument,
            string id,
            string outputPath,
            CancellationToken cancellationToken)
        {
            var semaphore = _semaphoreLocks.GetOrAdd(outputPath, key => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(Path.Join(outputPath, id)))
                {
                    await ExtractAllAttachmentsInternal(
                        inputArgument,
                        outputPath,
                        true,
                        cancellationToken).ConfigureAwait(false);

                    if (Directory.Exists(outputPath))
                    {
                        File.Create(Path.Join(outputPath, id));
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task ExtractAllAttachmentsInternal(
            string inputPath,
            string outputPath,
            bool isExternal,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(inputPath);
            ArgumentException.ThrowIfNullOrEmpty(outputPath);

            Directory.CreateDirectory(outputPath);

            var processArgs = string.Format(
                CultureInfo.InvariantCulture,
                "-dump_attachment:t \"\" -y -i {0} -t 0 -f null null",
                inputPath);

            int exitCode;

            using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        Arguments = processArgs,
                        FileName = _mediaEncoder.EncoderPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        WorkingDirectory = outputPath,
                        ErrorDialog = false
                    },
                    EnableRaisingEvents = true
                })
            {
                _logger.LogInformation("{File} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

                process.Start();

                var ranToCompletion = await ProcessExtensions.WaitForExitAsync(process, cancellationToken).ConfigureAwait(false);

                if (!ranToCompletion)
                {
                    try
                    {
                        _logger.LogWarning("Killing ffmpeg attachment extraction process");
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error killing attachment extraction process");
                    }
                }

                exitCode = ranToCompletion ? process.ExitCode : -1;
            }

            var failed = false;

            if (exitCode != 0)
            {
                if (isExternal && exitCode == 1)
                {
                    // ffmpeg returns exitCode 1 because there is no video or audio stream
                    // this can be ignored
                }
                else
                {
                    failed = true;

                    _logger.LogWarning("Deleting extracted attachments {Path} due to failure: {ExitCode}", outputPath, exitCode);
                    try
                    {
                        Directory.Delete(outputPath);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting extracted attachments {Path}", outputPath);
                    }
                }
            }
            else if (!Directory.Exists(outputPath))
            {
                failed = true;
            }

            if (failed)
            {
                _logger.LogError("ffmpeg attachment extraction failed for {InputPath} to {OutputPath}", inputPath, outputPath);

                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "ffmpeg attachment extraction failed for {0} to {1}", inputPath, outputPath));
            }

            _logger.LogInformation("ffmpeg attachment extraction completed for {InputPath} to {OutputPath}", inputPath, outputPath);
        }

        private async Task<Stream> GetAttachmentStream(
            MediaSourceInfo mediaSource,
            MediaAttachment mediaAttachment,
            CancellationToken cancellationToken)
        {
            var attachmentPath = await GetReadableFile(mediaSource.Path, mediaSource.Path, mediaSource, mediaAttachment, cancellationToken).ConfigureAwait(false);
            return AsyncFile.OpenRead(attachmentPath);
        }

        private async Task<string> GetReadableFile(
            string mediaPath,
            string inputFile,
            MediaSourceInfo mediaSource,
            MediaAttachment mediaAttachment,
            CancellationToken cancellationToken)
        {
            var outputPath = GetAttachmentCachePath(mediaPath, mediaSource, mediaAttachment.Index);
            await ExtractAttachment(inputFile, mediaSource, mediaAttachment.Index, outputPath, cancellationToken)
                .ConfigureAwait(false);

            return outputPath;
        }

        private async Task ExtractAttachment(
            string inputFile,
            MediaSourceInfo mediaSource,
            int attachmentStreamIndex,
            string outputPath,
            CancellationToken cancellationToken)
        {
            var semaphore = _semaphoreLocks.GetOrAdd(outputPath, key => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!File.Exists(outputPath))
                {
                    await ExtractAttachmentInternal(
                        _mediaEncoder.GetInputArgument(inputFile, mediaSource),
                        attachmentStreamIndex,
                        outputPath,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task ExtractAttachmentInternal(
            string inputPath,
            int attachmentStreamIndex,
            string outputPath,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(inputPath);

            ArgumentException.ThrowIfNullOrEmpty(outputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new ArgumentException("Path can't be a root directory.", nameof(outputPath)));

            var processArgs = string.Format(
                CultureInfo.InvariantCulture,
                "-dump_attachment:{1} \"{2}\" -i {0} -t 0 -f null null",
                inputPath,
                attachmentStreamIndex,
                EncodingUtils.NormalizePath(outputPath));

            int exitCode;

            using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        Arguments = processArgs,
                        FileName = _mediaEncoder.EncoderPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        ErrorDialog = false
                    },
                    EnableRaisingEvents = true
                })
            {
                _logger.LogInformation("{File} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

                process.Start();

                var ranToCompletion = await ProcessExtensions.WaitForExitAsync(process, cancellationToken).ConfigureAwait(false);

                if (!ranToCompletion)
                {
                    try
                    {
                        _logger.LogWarning("Killing ffmpeg attachment extraction process");
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error killing attachment extraction process");
                    }
                }

                exitCode = ranToCompletion ? process.ExitCode : -1;
            }

            var failed = false;

            if (exitCode != 0)
            {
                failed = true;

                _logger.LogWarning("Deleting extracted attachment {Path} due to failure: {ExitCode}", outputPath, exitCode);
                try
                {
                    if (File.Exists(outputPath))
                    {
                        _fileSystem.DeleteFile(outputPath);
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error deleting extracted attachment {Path}", outputPath);
                }
            }
            else if (!File.Exists(outputPath))
            {
                failed = true;
            }

            if (failed)
            {
                _logger.LogError("ffmpeg attachment extraction failed for {InputPath} to {OutputPath}", inputPath, outputPath);

                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "ffmpeg attachment extraction failed for {0} to {1}", inputPath, outputPath));
            }

            _logger.LogInformation("ffmpeg attachment extraction completed for {InputPath} to {OutputPath}", inputPath, outputPath);
        }

        private string GetAttachmentCachePath(string mediaPath, MediaSourceInfo mediaSource, int attachmentStreamIndex)
        {
            string filename;
            if (mediaSource.Protocol == MediaProtocol.File)
            {
                var date = _fileSystem.GetLastWriteTimeUtc(mediaPath);
                filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture) + "_" + date.Ticks.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D", CultureInfo.InvariantCulture);
            }
            else
            {
                filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D", CultureInfo.InvariantCulture);
            }

            var prefix = filename.AsSpan(0, 1);
            return Path.Join(_appPaths.DataPath, "attachments", prefix, filename);
        }
    }
}
