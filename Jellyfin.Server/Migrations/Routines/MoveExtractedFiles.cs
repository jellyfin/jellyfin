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
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to move extracted files to the new directories.
/// </summary>
public class MoveExtractedFiles : IDatabaseMigrationRoutine
{
    private readonly IApplicationPaths _appPaths;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<MoveExtractedFiles> _logger;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IPathManager _pathManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveExtractedFiles"/> class.
    /// </summary>
    /// <param name="appPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="pathManager">Instance of the <see cref="IPathManager"/> interface.</param>
    public MoveExtractedFiles(
        IApplicationPaths appPaths,
        ILibraryManager libraryManager,
        ILogger<MoveExtractedFiles> logger,
        IMediaSourceManager mediaSourceManager,
        IPathManager pathManager)
    {
        _appPaths = appPaths;
        _libraryManager = libraryManager;
        _logger = logger;
        _mediaSourceManager = mediaSourceManager;
        _pathManager = pathManager;
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
                if (++itemCount % 5_000 == 0)
                {
                    _logger.LogInformation("Moved extracted data for {Count} items in {Time}", itemCount, sw.Elapsed);
                }

                MoveSubtitleAndAttachmentFiles(item);
            }
        } while (previousCount == Limit);

        _logger.LogInformation("Moved extracted data for {Count} items in {Time}", itemCount, sw.Elapsed);

        // Get all subdirectories with 1 character names (those are the legacy directories)
        var subdirectories = Directory.GetDirectories(SubtitleCachePath, "*", SearchOption.AllDirectories).Where(s => s.Length == SubtitleCachePath.Length + 2).ToList();
        subdirectories.AddRange(Directory.GetDirectories(AttachmentCachePath, "*", SearchOption.AllDirectories).Where(s => s.Length == AttachmentCachePath.Length + 2));

        // Remove all legacy subdirectories
        foreach (var subdir in subdirectories)
        {
            Directory.Delete(subdir, true);
        }

        _logger.LogInformation("Cleaned up left over subtitles and attachments");
    }

    private void MoveSubtitleAndAttachmentFiles(BaseItem item)
    {
        var mediaStreams = item.GetMediaStreams().Where(s => s.Type == MediaStreamType.Subtitle && !s.IsExternal);
        foreach (var mediaStream in mediaStreams)
        {
            if (mediaStream.Codec is null)
            {
                continue;
            }

            var extension = GetSubtitleExtension(mediaStream.Codec);
            var oldSubtitleCachePath = GetOldSubtitleCachePath(item.Path, mediaStream.Index, extension);
            if (string.IsNullOrEmpty(oldSubtitleCachePath) || !File.Exists(oldSubtitleCachePath))
            {
                continue;
            }

            var newSubtitleCachePath = _pathManager.GetSubtitlePath(item.Id.ToString("N", CultureInfo.InvariantCulture), mediaStream.Index, extension);
            if (File.Exists(newSubtitleCachePath))
            {
                File.Delete(oldSubtitleCachePath);
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

        foreach (var attachment in _mediaSourceManager.GetMediaAttachments(item.Id))
        {
            var attachmentIndex = attachment.Index;
            var oldAttachmentCachePath = GetOldAttachmentCachePath(item.Path, attachmentIndex);
            if (string.IsNullOrEmpty(oldAttachmentCachePath) || !File.Exists(oldAttachmentCachePath))
            {
                continue;
            }

            var newAttachmentCachePath = _pathManager.GetAttachmentPath(item.Id.ToString("N", CultureInfo.InvariantCulture), attachmentIndex);
            var newDirectory = Path.GetDirectoryName(newAttachmentCachePath);
            if (File.Exists(newAttachmentCachePath))
            {
                File.Delete(oldAttachmentCachePath);
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

    private string? GetOldAttachmentCachePath(string mediaPath, int attachmentStreamIndex)
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

        var filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture) + "_" + date.Value.Ticks.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D", CultureInfo.InvariantCulture);

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
