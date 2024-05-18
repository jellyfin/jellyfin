#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using MediaBrowser.Common;
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
    public sealed class AttachmentExtractor : IAttachmentExtractor, IDisposable
    {
        private readonly ILogger<AttachmentExtractor> _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IMediaSourceManager _mediaSourceManager;

        private readonly AsyncKeyedLocker<string> _semaphoreLocks = new(o =>
        {
            o.PoolSize = 20;
            o.PoolInitialFill = 1;
        });

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
            using (await _semaphoreLocks.LockAsync(outputPath, cancellationToken).ConfigureAwait(false))
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
        }

        public async Task ExtractAllAttachmentsExternal(
            string inputArgument,
            string id,
            string outputPath,
            CancellationToken cancellationToken)
        {
            using (await _semaphoreLocks.LockAsync(outputPath, cancellationToken).ConfigureAwait(false))
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
                "-dump_attachment:t \"\" -y {0} -i {1} -t 0 -f null null",
                inputPath.EndsWith(".concat\"", StringComparison.OrdinalIgnoreCase) ? "-f concat -safe 0" : string.Empty,
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

                try
                {
                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    exitCode = process.ExitCode;
                }
                catch (OperationCanceledException)
                {
                    process.Kill(true);
                    exitCode = -1;
                }
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
            await CacheAllAttachments(mediaPath, inputFile, mediaSource, cancellationToken).ConfigureAwait(false);

            var outputPath = GetAttachmentCachePath(mediaPath, mediaSource, mediaAttachment.Index);
            await ExtractAttachment(inputFile, mediaSource, mediaAttachment.Index, outputPath, cancellationToken)
                .ConfigureAwait(false);

            return outputPath;
        }

        private async Task CacheAllAttachments(
            string mediaPath,
            string inputFile,
            MediaSourceInfo mediaSource,
            CancellationToken cancellationToken)
        {
            var outputFileLocks = new List<IDisposable>();
            var extractableAttachmentIds = new List<int>();

            try
            {
                foreach (var attachment in mediaSource.MediaAttachments)
                {
                    var outputPath = GetAttachmentCachePath(mediaPath, mediaSource, attachment.Index);

                    var releaser = await _semaphoreLocks.LockAsync(outputPath, cancellationToken).ConfigureAwait(false);

                    if (File.Exists(outputPath))
                    {
                        releaser.Dispose();
                        continue;
                    }

                    outputFileLocks.Add(releaser);
                    extractableAttachmentIds.Add(attachment.Index);
                }

                if (extractableAttachmentIds.Count > 0)
                {
                    await CacheAllAttachmentsInternal(mediaPath, inputFile, mediaSource, extractableAttachmentIds, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to cache media attachments for File:{File}", mediaPath);
            }
            finally
            {
                outputFileLocks.ForEach(x => x.Dispose());
            }
        }

        private async Task CacheAllAttachmentsInternal(
            string mediaPath,
            string inputFile,
            MediaSourceInfo mediaSource,
            List<int> extractableAttachmentIds,
            CancellationToken cancellationToken)
        {
            var outputPaths = new List<string>();
            var processArgs = string.Empty;

            foreach (var attachmentId in extractableAttachmentIds)
            {
                var outputPath = GetAttachmentCachePath(mediaPath, mediaSource, attachmentId);

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new FileNotFoundException($"Calculated path ({outputPath}) is not valid."));

                outputPaths.Add(outputPath);
                processArgs += string.Format(
                    CultureInfo.InvariantCulture,
                    " -dump_attachment:{0} \"{1}\"",
                    attachmentId,
                    EncodingUtils.NormalizePath(outputPath));
            }

            processArgs += string.Format(
                CultureInfo.InvariantCulture,
                " -i \"{0}\" -t 0 -f null null",
                inputFile);

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

                try
                {
                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    exitCode = process.ExitCode;
                }
                catch (OperationCanceledException)
                {
                    process.Kill(true);
                    exitCode = -1;
                }
            }

            var failed = false;

            if (exitCode == -1)
            {
                failed = true;

                foreach (var outputPath in outputPaths)
                {
                    try
                    {
                        _logger.LogWarning("Deleting extracted media attachment due to failure: {Path}", outputPath);
                        _fileSystem.DeleteFile(outputPath);
                    }
                    catch (FileNotFoundException)
                    {
                        // ffmpeg failed, so it is normal that one or more expected output files do not exist.
                        // There is no need to log anything for the user here.
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error deleting extracted media attachment {Path}", outputPath);
                    }
                }
            }
            else
            {
                foreach (var outputPath in outputPaths)
                {
                    if (!File.Exists(outputPath))
                    {
                        _logger.LogError("ffmpeg media attachment extraction failed for {InputPath} to {OutputPath}", inputFile, outputPath);
                        failed = true;
                        continue;
                    }

                    _logger.LogInformation("ffmpeg media attachment extraction completed for {InputPath} to {OutputPath}", inputFile, outputPath);
                }
            }

            if (failed)
            {
                throw new FfmpegException(
                    string.Format(CultureInfo.InvariantCulture, "ffmpeg media attachment extraction failed for {0}", inputFile));
            }
        }

        private async Task ExtractAttachment(
            string inputFile,
            MediaSourceInfo mediaSource,
            int attachmentStreamIndex,
            string outputPath,
            CancellationToken cancellationToken)
        {
            using (await _semaphoreLocks.LockAsync(outputPath, cancellationToken).ConfigureAwait(false))
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

                try
                {
                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    exitCode = process.ExitCode;
                }
                catch (OperationCanceledException)
                {
                    process.Kill(true);
                    exitCode = -1;
                }
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

        /// <inheritdoc />
        public void Dispose()
        {
            _semaphoreLocks.Dispose();
        }
    }
}
