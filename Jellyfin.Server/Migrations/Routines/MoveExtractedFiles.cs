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
using MediaBrowser.Model.MediaInfo;
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
        const int Limit = 500;
        int itemCount = 0, offset = 0;

        var sw = Stopwatch.StartNew();
        var itemsQuery = new InternalItemsQuery
        {
            MediaTypes = [MediaType.Video],
            SourceTypes = [SourceType.Library],
            IsVirtualItem = false,
            IsFolder = false,
            Limit = Limit,
            StartIndex = offset,
            EnableTotalRecordCount = true,
        };

        var records = _libraryManager.GetItemsResult(itemsQuery).TotalRecordCount;
        _logger.LogInformation("Checking {Count} items for movable extracted files.", records);

        // Make sure directories exist
        Directory.CreateDirectory(SubtitleCachePath);
        Directory.CreateDirectory(AttachmentCachePath);

        itemsQuery.EnableTotalRecordCount = false;
        do
        {
            itemsQuery.StartIndex = offset;
            var result = _libraryManager.GetItemsResult(itemsQuery);

            var items = result.Items;
            foreach (var item in items)
            {
                if (MoveSubtitleAndAttachmentFiles(item))
                {
                    itemCount++;
                }
            }

            offset += Limit;
            if (offset % 5_000 == 0)
            {
                _logger.LogInformation("Checked extracted files for {Count} items in {Time}.", offset, sw.Elapsed);
            }
        } while (offset < records);

        _logger.LogInformation("Checked {Checked} items - Moved files for {Items} items in {Time}.", records, itemCount, sw.Elapsed);

        // Get all subdirectories with 1 character names (those are the legacy directories)
        var subdirectories = Directory.GetDirectories(SubtitleCachePath, "*", SearchOption.AllDirectories).Where(s => s.Length == SubtitleCachePath.Length + 2).ToList();
        subdirectories.AddRange(Directory.GetDirectories(AttachmentCachePath, "*", SearchOption.AllDirectories).Where(s => s.Length == AttachmentCachePath.Length + 2));

        // Remove all legacy subdirectories
        foreach (var subdir in subdirectories)
        {
            Directory.Delete(subdir, true);
        }

        // Remove old cache path
        var attachmentCachePath = Path.Join(_appPaths.CachePath, "attachments");
        if (Directory.Exists(attachmentCachePath))
        {
            Directory.Delete(attachmentCachePath, true);
        }

        _logger.LogInformation("Cleaned up left over subtitles and attachments.");
    }

    private bool MoveSubtitleAndAttachmentFiles(BaseItem item)
    {
        var mediaStreams = item.GetMediaStreams().Where(s => s.Type == MediaStreamType.Subtitle && !s.IsExternal);
        var itemIdString = item.Id.ToString("N", CultureInfo.InvariantCulture);
        var modified = false;
        foreach (var mediaStream in mediaStreams)
        {
            if (mediaStream.Codec is null)
            {
                continue;
            }

            var mediaStreamIndex = mediaStream.Index;
            var extension = GetSubtitleExtension(mediaStream.Codec);
            var oldSubtitleCachePath = GetOldSubtitleCachePath(item.Path, mediaStream.Index, extension);
            if (string.IsNullOrEmpty(oldSubtitleCachePath) || !File.Exists(oldSubtitleCachePath))
            {
                continue;
            }

            var newSubtitleCachePath = _pathManager.GetSubtitlePath(itemIdString, mediaStreamIndex, extension);
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
                    _logger.LogDebug("Moved subtitle {Index} for {Item} from {Source} to {Destination}", mediaStreamIndex, item.Id, oldSubtitleCachePath, newSubtitleCachePath);

                    modified = true;
                }
            }
        }

        var attachments = _mediaSourceManager.GetMediaAttachments(item.Id).Where(a => !string.Equals(a.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase)).ToList();
        var shouldExtractOneByOne = attachments.Any(a => !string.IsNullOrEmpty(a.FileName)
                                                                              && (a.FileName.Contains('/', StringComparison.OrdinalIgnoreCase) || a.FileName.Contains('\\', StringComparison.OrdinalIgnoreCase)));
        foreach (var attachment in attachments)
        {
            var attachmentIndex = attachment.Index;
            var oldAttachmentPath = GetOldAttachmentDataPath(item.Path, attachmentIndex);
            if (string.IsNullOrEmpty(oldAttachmentPath) || !File.Exists(oldAttachmentPath))
            {
                oldAttachmentPath = GetOldAttachmentCachePath(itemIdString, attachment, shouldExtractOneByOne);
                if (string.IsNullOrEmpty(oldAttachmentPath) || !File.Exists(oldAttachmentPath))
                {
                    continue;
                }
            }

            var newAttachmentPath = _pathManager.GetAttachmentPath(itemIdString, attachment.FileName ?? attachmentIndex.ToString(CultureInfo.InvariantCulture));
            if (File.Exists(newAttachmentPath))
            {
                File.Delete(oldAttachmentPath);
            }
            else
            {
                var newDirectory = Path.GetDirectoryName(newAttachmentPath);
                if (newDirectory is not null)
                {
                    Directory.CreateDirectory(newDirectory);
                    File.Move(oldAttachmentPath, newAttachmentPath, false);
                    _logger.LogDebug("Moved attachment {Index} for {Item} from {Source} to {Destination}", attachmentIndex, item.Id, oldAttachmentPath, newAttachmentPath);

                    modified = true;
                }
            }
        }

        return modified;
    }

    private string? GetOldAttachmentDataPath(string? mediaPath, int attachmentStreamIndex)
    {
        if (mediaPath is null)
        {
            return null;
        }

        string filename;
        var protocol = _mediaSourceManager.GetPathProtocol(mediaPath);
        if (protocol == MediaProtocol.File)
        {
            DateTime? date;
            try
            {
                date = File.GetLastWriteTimeUtc(mediaPath);
            }
            catch (IOException e)
            {
                _logger.LogDebug("Skipping attachment at index {Index} for {Path}: {Exception}", attachmentStreamIndex, mediaPath, e.Message);

                return null;
            }

            filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture) + "_" + date.Value.Ticks.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D", CultureInfo.InvariantCulture);
        }
        else
        {
            filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D", CultureInfo.InvariantCulture);
        }

        return Path.Join(_appPaths.DataPath, "attachments", filename[..1], filename);
    }

    private string? GetOldAttachmentCachePath(string mediaSourceId, MediaAttachment attachment, bool shouldExtractOneByOne)
    {
        var attachmentFolderPath = Path.Join(_appPaths.CachePath, "attachments", mediaSourceId);
        if (shouldExtractOneByOne)
        {
            return Path.Join(attachmentFolderPath, attachment.Index.ToString(CultureInfo.InvariantCulture));
        }

        if (string.IsNullOrEmpty(attachment.FileName))
        {
            return null;
        }

        return Path.Join(attachmentFolderPath, attachment.FileName);
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
            _logger.LogDebug("Skipping subtitle at index {Index} for {Path}: {Exception}", streamIndex, path, e.Message);

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
