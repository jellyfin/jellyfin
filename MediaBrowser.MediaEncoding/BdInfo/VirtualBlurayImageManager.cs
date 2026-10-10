using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DiscUtils.Udf;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// Exposes Blu-ray folders as seekable UDF images backed by their original files.
/// </summary>
public sealed class VirtualBlurayImageManager
{
    private readonly IBlurayExaminer _examiner;
    private readonly IServerConfigurationManager _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VirtualBlurayImageManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualBlurayImageManager"/> class.
    /// </summary>
    /// <param name="examiner">The Blu-ray playlist examiner.</param>
    /// <param name="configuration">The server configuration.</param>
    /// <param name="cache">The metadata cache.</param>
    /// <param name="logger">The logger.</param>
    public VirtualBlurayImageManager(
        IBlurayExaminer examiner,
        IServerConfigurationManager configuration,
        IMemoryCache cache,
        ILogger<VirtualBlurayImageManager> logger)
    {
        _examiner = examiner;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Selects the virtual image representation for an outgoing API source.
    /// Internal media sources retain their Blu-ray folder type for FFmpeg.
    /// </summary>
    /// <param name="source">The client-facing media source.</param>
    public void ApplyTo(MediaSourceInfo source)
    {
        if (source.Protocol != MediaProtocol.File || source.VideoType != VideoType.BluRay)
        {
            return;
        }

        var image = GetImage(source.Path);
        if (
            image is null
            || (
                image.ClipCount == 1
                && !_configuration.Configuration.UseVirtualIsoForSingleClipBluRays))
        {
            return;
        }

        source.Container = "iso";
        source.VideoType = VideoType.Iso;
        source.IsoType = IsoType.BluRay;
        source.Size = image.Length;
        source.ETag = image.Tag;
    }

    /// <summary>
    /// Builds or reuses an image after checking the source paths, lengths and modification times.
    /// No media payload is read or copied to create the image.
    /// </summary>
    /// <param name="path">The disc folder or its BDMV directory.</param>
    /// <returns>The image, or null when the folder cannot be represented.</returns>
    public VirtualBlurayImage? GetImage(string path)
    {
        try
        {
            var root = new DirectoryInfo(path);
            if (!root.Exists)
            {
                return null;
            }

            var bdmv = string.Equals(root.Name, "BDMV", StringComparison.OrdinalIgnoreCase)
                ? root
                : root.GetDirectories()
                    .FirstOrDefault(directory =>
                        string.Equals(directory.Name, "BDMV", StringComparison.OrdinalIgnoreCase));
            if (
                bdmv is null
                || !bdmv.GetFiles()
                    .Any(file =>
                        string.Equals(file.Name, "index.bdmv", StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            root =
                bdmv.Parent
                ?? throw new DirectoryNotFoundException("The disc directory has no parent.");
            var files = new List<(string Name, FileInfo File)>();
            var directories = new List<string>();
            AddDirectory(bdmv, "BDMV", files, directories);
            var certificate = root.GetDirectories()
                .FirstOrDefault(directory =>
                    string.Equals(directory.Name, "CERTIFICATE", StringComparison.OrdinalIgnoreCase));
            if (certificate is not null)
            {
                AddDirectory(certificate, "CERTIFICATE", files, directories);
            }

            var signature = new StringBuilder(root.FullName).Append('\0');
            foreach (var directory in directories)
            {
                signature.Append(directory).Append('\0');
            }

            foreach (var (name, file) in files)
            {
                signature
                    .Append(name)
                    .Append('\0')
                    .Append(file.Length.ToString(CultureInfo.InvariantCulture))
                    .Append('\0')
                    .Append(file.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture))
                    .Append('\0');
            }

            var tag = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(signature.ToString())));
            var key = (typeof(VirtualBlurayImage), root.FullName);
            if (_cache.TryGetValue<VirtualBlurayImage>(key, out var cached) && cached?.Tag == tag)
            {
                return cached;
            }

            var clipCount = _examiner.GetDiscInfo(root.FullName).Files?.Length ?? 0;
            if (clipCount == 0)
            {
                return null;
            }

            var builder = new UdfBuilder
            {
                VolumeIdentifier = "BLURAY",
                VolumeSetIdentifier = tag,
                RecordingTime = files.Max(file => file.File.LastWriteTimeUtc),
            };
            foreach (var directory in directories)
            {
                builder.AddDirectory(directory);
            }

            foreach (var (name, file) in files)
            {
                builder.AddFile(name, file.FullName);
            }

            var image = new VirtualBlurayImage(builder, tag, clipCount);
            _cache.Set(key, image, new MemoryCacheEntryOptions { Size = 1 });
            return image;
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or ArgumentException
                        or OverflowException)
        {
            _logger.LogWarning(
                exception,
                "Unable to expose Blu-ray folder {Path} as a virtual ISO",
                path);
            return null;
        }
    }

    private static void AddDirectory(
        DirectoryInfo directory,
        string name,
        List<(string Name, FileInfo File)> files,
        List<string> directories)
    {
        // Follow library-root links, but never expose links from inside a disc to unrelated files.
        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException("A directory inside the disc is a symbolic link.");
        }

        directories.Add(name);
        foreach (
            var entry in directory
                .EnumerateFileSystemInfos()
                .OrderBy(entry => entry.Name, StringComparer.Ordinal))
        {
            if (entry.Name == "@eaDir")
            {
                continue;
            }

            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new IOException("A file inside the disc is a symbolic link.");
            }

            var relative = name + "/" + entry.Name;
            if (entry is DirectoryInfo child)
            {
                AddDirectory(child, relative, files, directories);
            }
            else
            {
                files.Add((relative, (FileInfo)entry));
            }
        }
    }
}
