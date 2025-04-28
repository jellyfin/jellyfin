#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to move extracted files to the new directories.
/// </summary>
[JellyfinMigration("2025-04-20T21:00:00", nameof(MoveExtractedFiles), "9063b0Ef-CFF1-4EDC-9A13-74093681A89B")]
#pragma warning disable CS0618 // Type or member is obsolete
public class MoveExtractedFiles : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly IApplicationPaths _appPaths;
    private readonly ILogger<MoveExtractedFiles> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IPathManager _pathManager;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveExtractedFiles"/> class.
    /// </summary>
    /// <param name="appPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="pathManager">Instance of the <see cref="IPathManager"/> interface.</param>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    public MoveExtractedFiles(
        IApplicationPaths appPaths,
        ILogger<MoveExtractedFiles> logger,
        IPathManager pathManager,
        IFileSystem fileSystem,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _appPaths = appPaths;
        _logger = logger;
        _pathManager = pathManager;
        _fileSystem = fileSystem;
        _dbProvider = dbProvider;
    }

    private string SubtitleCachePath => Path.Combine(_appPaths.DataPath, "subtitles");

    private string AttachmentCachePath => Path.Combine(_appPaths.DataPath, "attachments");

    /// <inheritdoc />
    public void Perform()
    {
        const int Limit = 5000;
        int itemCount = 0, offset = 0;

        var sw = Stopwatch.StartNew();

        using var context = _dbProvider.CreateDbContext();
        var records = context.BaseItems.Count(b => b.MediaType == MediaType.Video.ToString() && !b.IsVirtualItem && !b.IsFolder);
        _logger.LogInformation("Checking {Count} items for movable extracted files.", records);

        // Make sure directories exist
        Directory.CreateDirectory(SubtitleCachePath);
        Directory.CreateDirectory(AttachmentCachePath);
        do
        {
            var results = context.BaseItems
                            .Include(e => e.MediaStreams!.Where(s => s.StreamType == MediaStreamTypeEntity.Subtitle && !s.IsExternal))
                            .Where(b => b.MediaType == MediaType.Video.ToString() && !b.IsVirtualItem && !b.IsFolder)
                            .OrderBy(e => e.Id)
                            .Skip(offset)
                            .Take(Limit)
                            .Select(b => new Tuple<Guid, string?, ICollection<MediaStreamInfo>?>(b.Id, b.Path, b.MediaStreams)).ToList();

            foreach (var result in results)
            {
                if (MoveSubtitleAndAttachmentFiles(result.Item1, result.Item2, result.Item3, context))
                {
                    itemCount++;
                }
            }

            offset += Limit;
            _logger.LogInformation("Checked: {Count} - Moved: {Items} - Time: {Time}", offset, itemCount, sw.Elapsed);
        } while (offset < records);

        _logger.LogInformation("Moved files for {Count} items in {Time}", itemCount, sw.Elapsed);

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

    private bool MoveSubtitleAndAttachmentFiles(Guid id, string? path, ICollection<MediaStreamInfo>? mediaStreams, JellyfinDbContext context)
    {
        var itemIdString = id.ToString("N", CultureInfo.InvariantCulture);
        var modified = false;
        if (mediaStreams is not null)
        {
            foreach (var mediaStream in mediaStreams)
            {
                if (mediaStream.Codec is null)
                {
                    continue;
                }

                var mediaStreamIndex = mediaStream.StreamIndex;
                var extension = GetSubtitleExtension(mediaStream.Codec);
                var oldSubtitleCachePath = GetOldSubtitleCachePath(path, mediaStreamIndex, extension);
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
                        _logger.LogDebug("Moved subtitle {Index} for {Item} from {Source} to {Destination}", mediaStreamIndex, id, oldSubtitleCachePath, newSubtitleCachePath);

                        modified = true;
                    }
                }
            }
        }

#pragma warning disable CA1309 // Use ordinal string comparison
        var attachments = context.AttachmentStreamInfos.Where(a => a.ItemId.Equals(id) && !string.Equals(a.Codec, "mjpeg")).ToList();
#pragma warning restore CA1309 // Use ordinal string comparison
        var shouldExtractOneByOne = attachments.Any(a => !string.IsNullOrEmpty(a.Filename)
                                                                              && (a.Filename.Contains('/', StringComparison.OrdinalIgnoreCase) || a.Filename.Contains('\\', StringComparison.OrdinalIgnoreCase)));
        foreach (var attachment in attachments)
        {
            var attachmentIndex = attachment.Index;
            var oldAttachmentPath = GetOldAttachmentDataPath(path, attachmentIndex);
            if (string.IsNullOrEmpty(oldAttachmentPath) || !File.Exists(oldAttachmentPath))
            {
                oldAttachmentPath = GetOldAttachmentCachePath(itemIdString, attachment, shouldExtractOneByOne);
                if (string.IsNullOrEmpty(oldAttachmentPath) || !File.Exists(oldAttachmentPath))
                {
                    continue;
                }
            }

            var newAttachmentPath = _pathManager.GetAttachmentPath(itemIdString, attachment.Filename ?? attachmentIndex.ToString(CultureInfo.InvariantCulture));
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
                    _logger.LogDebug("Moved attachment {Index} for {Item} from {Source} to {Destination}", attachmentIndex, id, oldAttachmentPath, newAttachmentPath);

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
        if (_fileSystem.IsPathFile(mediaPath))
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

    private string? GetOldAttachmentCachePath(string mediaSourceId, AttachmentStreamInfo attachment, bool shouldExtractOneByOne)
    {
        var attachmentFolderPath = Path.Join(_appPaths.CachePath, "attachments", mediaSourceId);
        if (shouldExtractOneByOne)
        {
            return Path.Join(attachmentFolderPath, attachment.Index.ToString(CultureInfo.InvariantCulture));
        }

        if (string.IsNullOrEmpty(attachment.Filename))
        {
            return null;
        }

        return Path.Join(attachmentFolderPath, attachment.Filename);
    }

    private string? GetOldSubtitleCachePath(string? path, int streamIndex, string outputSubtitleExtension)
    {
        if (path is null)
        {
            return null;
        }

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
