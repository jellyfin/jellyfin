using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using AsyncKeyedLock;
using DiscUtils.Udf;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// Exposes Blu-ray folders as seekable UDF images backed by their original files.
/// </summary>
/// <param name="cache">The metadata cache.</param>
/// <param name="logger">The logger.</param>
/// <param name="shouldIgnore">The library's file ignore rules, applied to disc-relative paths.</param>
public sealed class VirtualBlurayImageManager(
    IMemoryCache cache,
    ILogger<VirtualBlurayImageManager> logger,
    Func<string, bool> shouldIgnore) : IVirtualBlurayImageManager, IDisposable
{
    private static readonly EnumerationOptions _ignoreCase = new()
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        AttributesToSkip = 0,
        IgnoreInaccessible = false,
    };

    private readonly AsyncKeyedLocker<string> _imageLocks = new();

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

        // Listing metadata may be stale until a streaming request validates the image.
        var image = GetImage(source.Path, validate: false);
        if (image is null)
        {
            return;
        }

        source.Container = "iso";
        source.VideoType = VideoType.Iso;
        source.IsoType = IsoType.BluRay;
        source.Path = Path.TrimEndingDirectorySeparator(source.Path) + ".iso";
        source.Size = image.Length;
        source.ETag = image.Tag;
    }

    /// <summary>
    /// Builds or reuses an image after checking the source paths, lengths and modification times.
    /// No media payload is read or copied to create the image.
    /// </summary>
    /// <param name="path">The disc folder or its BDMV directory.</param>
    /// <returns>The image, or null when the folder cannot be represented.</returns>
    public IVirtualBlurayImage? GetImage(string path) => GetImage(path, validate: true);

    /// <inheritdoc />
    public void Dispose() => _imageLocks.Dispose();

    private VirtualBlurayImage? GetImage(string path, bool validate)
    {
        try
        {
            var root = GetDiscRoot(path);
            var key = (typeof(VirtualBlurayImage), root.FullName);
            using var acquired = _imageLocks.Lock(root.FullName);
            if (!validate && cache.TryGetValue<VirtualBlurayImage>(key, out var metadata))
            {
                return metadata;
            }

            if (ReadDisc(root) is not { } entries)
            {
                return null;
            }

            var tag = GetTag(root, entries);
            if (cache.TryGetValue<VirtualBlurayImage>(key, out var cached) && cached?.Tag == tag)
            {
                return cached;
            }

            var image = new VirtualBlurayImage(CreateBuilder(entries, tag), tag);
            cache.Set(key, image, new MemoryCacheEntryOptions { Size = 1 });
            return image;
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or ArgumentException
                        or OverflowException)
        {
            logger.LogWarning(
                exception,
                "Unable to expose Blu-ray folder {Path} as a virtual ISO",
                path);
            return null;
        }
    }

    private static DirectoryInfo GetDiscRoot(string path)
    {
        var root = new DirectoryInfo(Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)));
        return root.Name.Equals("BDMV", StringComparison.OrdinalIgnoreCase)
            ? root.Parent
                ?? throw new DirectoryNotFoundException("The disc directory has no parent.")
            : root;
    }

    private static DirectoryInfo? GetBlurayDirectory(DirectoryInfo root)
    {
        if (!root.Exists)
        {
            return null;
        }

        var bdmv = root.EnumerateDirectories("BDMV", _ignoreCase).FirstOrDefault();
        return bdmv?.EnumerateFiles("index.bdmv", _ignoreCase).Any() == true ? bdmv : null;
    }

    private static UdfBuilder CreateBuilder(DiscEntry[] entries, string tag)
    {
        var builder = new UdfBuilder
        {
            VolumeIdentifier = "BLURAY",
            VolumeSetIdentifier = tag,
            RecordingTime = entries.Max(entry => entry.File?.LastWriteTimeUtc ?? DateTime.MinValue),
        };
        foreach (var entry in entries.Where(entry => entry.File is null))
        {
            builder.AddDirectory(entry.Name);
        }

        foreach (var (name, file) in entries)
        {
            if (file is null)
            {
                continue;
            }

            builder.AddFile(name, file.FullName);
        }

        return builder;
    }

    // The fingerprint covers names, link targets, lengths and modification times, not media payloads.
    private static string GetTag(DirectoryInfo root, DiscEntry[] entries)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, root.FullName);
        foreach (var (name, file) in entries)
        {
            Append(hash, name);
            if (file is null)
            {
                Append(hash, -1);
                continue;
            }

            Append(hash, file.FullName);
            Append(hash, file.Length);
            Append(hash, file.LastWriteTimeUtc.Ticks);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void Append(IncrementalHash hash, ReadOnlySpan<char> value)
    {
        // Length-prefix each value so adjacent fields cannot run together.
        Append(hash, value.Length);
        hash.AppendData(MemoryMarshal.AsBytes(value));
    }

    private static void Append(IncrementalHash hash, long value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        hash.AppendData(bytes);
    }

    private static FileInfo ResolveFile(FileInfo file)
    {
        if (file.LinkTarget is null)
        {
            return file;
        }

        var target = FileSystemHelper.ResolveLinkTarget(file, returnFinalTarget: true);
        return target is { Exists: true, LinkTarget: null }
            ? target
            : throw new IOException("A file inside the disc has an unresolved symbolic link.");
    }

    private DiscEntry[]? ReadDisc(DirectoryInfo root)
    {
        var bdmv = GetBlurayDirectory(root);
        if (bdmv is null)
        {
            return null;
        }

        var ancestors = new HashSet<string>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var certificate = root.EnumerateDirectories("CERTIFICATE", _ignoreCase).FirstOrDefault();
        return
        [
            .. EnumerateDisc(bdmv, "BDMV", ancestors),
            .. certificate is null
                ? Enumerable.Empty<DiscEntry>()
                : EnumerateDisc(certificate, "CERTIFICATE", ancestors),
        ];
    }

    private IEnumerable<DiscEntry> EnumerateDisc(
        DirectoryInfo directory,
        string name,
        HashSet<string> ancestors)
    {
        directory =
            (DirectoryInfo?)directory.ResolveLinkTarget(returnFinalTarget: true) ?? directory;
        if (!ancestors.Add(directory.FullName))
        {
            throw new IOException("A directory inside the disc contains a symbolic link cycle.");
        }

        yield return new DiscEntry(name, null);
        foreach (
            var entry in directory
                .EnumerateFileSystemInfos()
                .OrderBy(entry => entry.Name, StringComparer.Ordinal))
        {
            var relative = name + "/" + entry.Name;
            if (shouldIgnore(relative))
            {
                continue;
            }

            if (entry is not DirectoryInfo child)
            {
                yield return new DiscEntry(relative, ResolveFile((FileInfo)entry));
                continue;
            }

            foreach (var descendant in EnumerateDisc(child, relative, ancestors))
            {
                yield return descendant;
            }
        }

        ancestors.Remove(directory.FullName);
    }

    /// <summary>A disc-relative entry; directories have no file.</summary>
    private readonly record struct DiscEntry(string Name, FileInfo? File);
}
