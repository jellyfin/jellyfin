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
    private readonly ILogger<MoveTrickplayFiles> _logger;
    private readonly IApplicationPaths _appPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveExtractedFiles"/> class.
    /// </summary>
    /// <param name="pathManager">Instance of the <see cref="IPathManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="appPaths">Instace of the <see cref="IApplicationPaths"/> internface.</param>
    /// <param name="logger">The logger.</param>
    public MoveExtractedFiles(
        IPathManager pathManager,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        ILogger<MoveTrickplayFiles> logger,
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
    public Guid Id => new("9063b0ef-cff1-4edc-9a13-74093681a89b");

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
            IsFolder = false
        };

        do
        {
            var items = _libraryManager.GetItemList(itemsQuery);
            previousCount = items.Count;
            offset += Limit;
            foreach (var item in items)
            {
                if (++itemCount % 1_000 == 0)
                {
                    _logger.LogInformation("Moved {Count} items in {Time}", itemCount, sw.Elapsed);
                }

                MoveSubtitleandAttachmentFiles(item);
            }
        } while (previousCount == Limit);

        _logger.LogInformation("Moved {Count} items in {Time}", itemCount, sw.Elapsed);
        _logger.LogInformation("Cleaning up left over subtitles and attachments!");

        // Get all subdirectories
        var subdirectories = Directory.GetDirectories(SubtitleCachePath, "*", SearchOption.AllDirectories).ToList();
        subdirectories.AddRange(Directory.GetDirectories(AttachmentCachePath, "*", SearchOption.AllDirectories));

        // Remove all subdirectories with 1 character names (those are the legacy directories)
        foreach (var subdir in subdirectories.Where(s => s.Length == 1))
        {
            Directory.Delete(subdir, true);
        }
    }

    private void MoveSubtitleandAttachmentFiles(BaseItem item)
    {
        var mediaSources = item.GetMediaSources(false);
        foreach (var mediaSource in mediaSources)
        {
            var mediaStreams = mediaSource.MediaStreams.Where(s => !s.IsExternal);
            foreach (var mediaStream in mediaStreams)
            {
                var extension = GetSubtitleExtension(mediaStream.Codec);
                var oldSubtitleCachePath = GetOldSubtitleCachePath(mediaSource.Path, mediaStream.Index, extension);
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

    private string GetOldAttachmentCachePath(MediaSourceInfo mediaSource, string mediaPath, int attachmentStreamIndex)
    {
        string filename;
        if (mediaSource.Protocol == MediaProtocol.File)
        {
            var date = File.GetLastWriteTimeUtc(mediaPath);
            filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture) + "_" + date.Ticks.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D", CultureInfo.InvariantCulture);
        }
        else
        {
            filename = (mediaPath + attachmentStreamIndex.ToString(CultureInfo.InvariantCulture)).GetMD5().ToString("D", CultureInfo.InvariantCulture);
        }

        return Path.Join(AttachmentCachePath, filename[..1], filename);
    }

    private string GetOldSubtitleCachePath(string path, int streamIndex, string outputSubtitleExtension)
    {
        var ticksParam = string.Empty;
        var date = File.GetLastWriteTimeUtc(path);
        ReadOnlySpan<char> filename = new Guid(MD5.HashData(Encoding.Unicode.GetBytes(path + "_" + streamIndex.ToString(CultureInfo.InvariantCulture) + "_" + date.Ticks.ToString(CultureInfo.InvariantCulture) + ticksParam))) + outputSubtitleExtension;

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
