#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to move extracted files to the new directories.
/// </summary>
public class MoveExtractedFiles : IMigrationRoutine
{
    private readonly IPathManager _pathManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<MoveExtractedFiles> _logger;
    private readonly IApplicationPaths _appPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveExtractedFiles"/> class.
    /// </summary>
    /// <param name="pathManager">Instance of the <see cref="IPathManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="appPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="logger">The logger.</param>
    public MoveExtractedFiles(
        IPathManager pathManager,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        ILogger<MoveExtractedFiles> logger,
        IApplicationPaths appPaths)
    {
        _pathManager = pathManager;
        _fileSystem = fileSystem;
        _libraryManager = libraryManager;
        _logger = logger;
        _appPaths = appPaths;
    }

    private string SubtitleCachePath => Path.Combine(_appPaths.DataPath, "subtitles");

    private string AttachmentCachePath => Path.Combine(_appPaths.DataPath, "attachments");

    /// <inheritdoc />
    public Guid Id => new("9063b0Ef-CFF1-4EDC-9A13-74093681A89B");

    /// <inheritdoc />
    public string Name => "MoveExtractedFiles";

    /// <inheritdoc />
    public bool PerformOnNewInstall => false;

    /// <inheritdoc />
    public void Perform()
    {
        const int Limit = 100;
        int itemCount = 0, offset = 0, previousCount;

        var sw = Stopwatch.StartNew();
        var itemsQuery = new InternalItemsQuery
        {
            MediaTypes = [MediaType.Video],
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            IsFolder = false,
            EnableTotalRecordCount = true
        };

        do
        {
            var result = _libraryManager.GetItemsResult(itemsQuery);
            _logger.LogInformation("Moving extracted files for {Count} items", result.TotalRecordCount);

            var items = result.Items;
            previousCount = items.Count;
            offset += Limit;
            foreach (var item in items)
            {
                if (++itemCount % 1_000 == 0)
                {
                    _logger.LogInformation("Moved extracted data for {Count} items in {Time}", itemCount, sw.Elapsed);
                }

                MoveSubtitleAndAttachmentFiles(item);
            }
        } while (previousCount == Limit);

        _logger.LogInformation("Moved extracted data for {Count} items in {Time}", itemCount, sw.Elapsed);

        // Get all subdirectories with 1 character names (those are the legacy directories)
        var subdirectories = Directory.GetDirectories(SubtitleCachePath, "*", SearchOption.AllDirectories).Where(s => s.Length == SubtitleCachePath.Length + 1).ToList();
        subdirectories.AddRange(Directory.GetDirectories(AttachmentCachePath, "*", SearchOption.AllDirectories).Where(s => s.Length == AttachmentCachePath.Length + 1));

        // Remove all legacy subdirectories
        foreach (var subdir in subdirectories)
        {
            Directory.Delete(subdir, true);
        }

        _logger.LogInformation("Cleaned up left over subtitles and attachments");
    }

    private void MoveSubtitleAndAttachmentFiles(BaseItem item)
    {
        var mediaSources = item.GetMediaSources(false);
        foreach (var mediaSource in mediaSources)
        {
            var mediaStreams = mediaSource.MediaStreams.Where(s => !s.IsExternal);
            foreach (var mediaStream in mediaStreams)
            {
                var extension = GetSubtitleExtension(mediaStream.Codec);
                var oldSubtitleCachePath = GetOldSubtitleCachePath(mediaSource.Path, mediaStream.Index, extension);
                if (string.IsNullOrEmpty(oldSubtitleCachePath) || !_fileSystem.FileExists(oldSubtitleCachePath))
                {
                    continue;
                }

                var newSubtitleCachePath = _pathManager.GetSubtitlePath(mediaSource.Id, mediaStream.Index, extension);
                if (_fileSystem.FileExists(newSubtitleCachePath))
                {
                    _fileSystem.DeleteFile(oldSubtitleCachePath);
                }
                else
                {
                    var newDirectory = Path.GetDirectoryName(newSubtitleCachePath);
                    if (newDirectory is not null)
                    {
                        Directory.CreateDirectory(newDirectory);
                        File.Move(oldSubtitleCachePath, newSubtitleCachePath, false);
                    }
                }
            }

            foreach (var attachment in mediaSource.MediaAttachments)
            {
                var attachmentIndex = attachment.Index;
                var oldAttachmentCachePath = GetOldAttachmentCachePath(mediaSource, mediaSource.Path, attachmentIndex);
                if (string.IsNullOrEmpty(oldAttachmentCachePath) || !_fileSystem.FileExists(oldAttachmentCachePath))
                {
                    continue;
                }

                var newAttachmentCachePath = _pathManager.GetAttachmentPath(mediaSource.Id, attachmentIndex);
                var newDirectory = Path.GetDirectoryName(newAttachmentCachePath);
                if (_fileSystem.FileExists(newAttachmentCachePath))
                {
                    _fileSystem.DeleteFile(oldAttachmentCachePath);
                }
                else
                {
                    if (newDirectory is not null)
                    {
                        Directory.CreateDirectory(newDirectory);
                        File.Move(oldAttachmentCachePath, newAttachmentCachePath, false);
                    }
                }
            }
        }
    }

    private string? GetOldAttachmentCachePath(MediaSourceInfo mediaSource, string mediaPath, int attachmentStreamIndex)
    {
        string filename;
        if (mediaSource.Protocol == MediaProtocol.File)
        {
            DateTime? date;
            try
            {
                date = File.GetLastWriteTimeUtc(mediaPath);
            }
            catch (IOException e)
            {
                _logger.LogDebug("Skipping index {Index} for {Path}: {Exception}", attachmentStreamIndex, mediaPath, e.Message);

                return null;
            }

            filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture) + "_" + date.Value.Ticks.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D", CultureInfo.InvariantCulture);
        }
        else
        {
            filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D", CultureInfo.InvariantCulture);
        }

        return Path.Join(AttachmentCachePath, filename[..1], filename);
    }

    private string? GetOldSubtitleCachePath(string path, int streamIndex, string outputSubtitleExtension)
    {
        DateTime? date;
        try
        {
            date = File.GetLastWriteTimeUtc(path);
        }
        catch (IOException e)
        {
            _logger.LogDebug("Skipping index {Index} for {Path}: {Exception}", streamIndex, path, e.Message);

            return null;
        }

        var ticksParam = string.Empty;
        ReadOnlySpan<char> filename = new Guid(MD5.HashData(Encoding.Unicode.GetBytes(path + "_" + streamIndex.ToString(CultureInfo.InvariantCulture) + "_" + date.Value.Ticks.ToString(CultureInfo.InvariantCulture) + ticksParam))) + outputSubtitleExtension;

        return Path.Join(SubtitleCachePath, filename[..1], filename);
    }

    private static string GetSubtitleExtension(string codec)
    {
        if (codec.ToLower(CultureInfo.InvariantCulture).Equals("ass", StringComparison.OrdinalIgnoreCase)
            || codec.ToLower(CultureInfo.InvariantCulture).Equals("ssa", StringComparison.OrdinalIgnoreCase))
        {
            return "." + codec;
        }
        else if (codec.Contains("pgs", StringComparison.OrdinalIgnoreCase))
        {
            return ".sup";
        }
        else
        {
            return ".srt";
        }
    }
}
