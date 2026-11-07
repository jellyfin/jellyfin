using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// A disc reference: a <c>.strm</c> file whose entry uses the <c>dvd://</c> or <c>bluray://</c> scheme to point at
/// a DVD/Blu-ray ISO image or unpacked disc folder, optionally selecting one DVD title or Blu-ray playlist.
/// </summary>
/// <remarks>
/// <para>
/// The <c>.strm</c> file is resolved like any other video file, so the usual naming rules apply to it: it can be a
/// movie, an alternate version (<c>Movie (2000) - Director's Cut.strm</c>), an extra (<c>Movie-featurette.strm</c>
/// or a file in an <c>extras</c> folder) or an episode (<c>Show S01E02.strm</c>).
/// </para>
/// <para>
/// Blank lines and lines starting with <c>#</c> are ignored and the first remaining line is the entry. Entries using
/// any other scheme (<c>http://</c> and so on) are ordinary <c>.strm</c> files and are not affected.
/// </para>
/// <code>
/// # Director's cut is title 3 of the main disc
/// dvd://../Discs/MyDVD.iso?title=3
/// bluray://./MyMovie.iso?playlist=800
/// bluray:///mnt/media/Discs/MyMovie
/// </code>
/// <para>
/// The path after the scheme is relative to the <c>.strm</c> file's folder, or absolute (<c>dvd:///mnt/x.iso</c>,
/// <c>dvd://C:/Discs/x.iso</c>). It may point at an <c>.iso</c>/<c>.img</c> image, a folder containing
/// <c>VIDEO_TS</c> (for <c>dvd://</c>) or <c>BDMV</c> (for <c>bluray://</c>), or that folder itself, and must be inside
/// a library folder. The path is used literally, not URL-decoded.
/// </para>
/// <para>
/// The optional <c>index</c> parameter (also accepted as <c>title</c> or <c>playlist</c>) is the DVD title number or
/// Blu-ray playlist number (<c>00800.mpls</c> = 800). When omitted, the longest title is used. A <c>?</c> only starts
/// the parameters when it is followed by <c>name=value</c> pairs (or nothing), so file names containing <c>?</c> work.
/// </para>
/// </remarks>
public sealed class DiscReference
{
    /// <summary>
    /// The largest <c>.strm</c> file that is checked for a disc reference, in bytes.
    /// </summary>
    public const int MaxFileSize = 64 * 1024;

    private const string DvdScheme = "dvd://";

    private const string BluRayScheme = "bluray://";

    private static readonly StringComparison _pathComparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private DiscReference(string sourcePath, VideoType videoType, IsoType isoType, int? index)
    {
        SourcePath = sourcePath;
        VideoType = videoType;
        IsoType = isoType;
        Index = index;
    }

    /// <summary>
    /// Gets the full path of the referenced ISO image, or of the folder containing <c>VIDEO_TS</c> or <c>BDMV</c>.
    /// </summary>
    public string SourcePath { get; }

    /// <summary>
    /// Gets the video type of the referenced source: <see cref="VideoType.Iso"/>,
    /// <see cref="VideoType.Dvd"/> or <see cref="VideoType.BluRay"/>.
    /// </summary>
    public VideoType VideoType { get; }

    /// <summary>
    /// Gets the disc type, taken from the scheme.
    /// </summary>
    public IsoType IsoType { get; }

    /// <summary>
    /// Gets the DVD title number or Blu-ray playlist number to play, or <c>null</c> for the longest title.
    /// </summary>
    public int? Index { get; }

    /// <summary>
    /// Determines whether a path is a <c>.strm</c> file, which may hold a disc reference.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns><c>true</c> if the path has the <c>.strm</c> extension.</returns>
    public static bool IsStrmPath(string? path)
        => !string.IsNullOrEmpty(path)
            && Path.GetExtension(path.AsSpan()).Equals(".strm", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the folders of every library, which are the only places a disc reference may point into.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <returns>The library folders.</returns>
    public static IReadOnlyList<string> GetLibraryRoots(ILibraryManager libraryManager)
    {
        ArgumentNullException.ThrowIfNull(libraryManager);

        return libraryManager.GetUserRootFolder().Children
            .OfType<CollectionFolder>()
            .SelectMany(folder => folder.PhysicalLocations)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Reads a <c>.strm</c> file and, if its entry uses the <c>dvd://</c> or <c>bluray://</c> scheme, validates it.
    /// </summary>
    /// <param name="strmPath">The path of the <c>.strm</c> file.</param>
    /// <param name="allowedRoots">The folders the referenced source must be inside.</param>
    /// <param name="logger">The logger used to report invalid references.</param>
    /// <param name="reference">The disc reference, or <c>null</c> if the file is a disc reference but invalid.</param>
    /// <returns><c>true</c> if the file is a disc reference (valid or not); <c>false</c> for any other <c>.strm</c> file.</returns>
    public static bool TryRead(string strmPath, IEnumerable<string> allowedRoots, ILogger logger, out DiscReference? reference)
    {
        ArgumentException.ThrowIfNullOrEmpty(strmPath);
        ArgumentNullException.ThrowIfNull(logger);

        reference = null;
        string contents;
        try
        {
            var fileInfo = new FileInfo(strmPath);
            if (!fileInfo.Exists || fileInfo.Length > MaxFileSize)
            {
                return false;
            }

            contents = File.ReadAllText(strmPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Unable to read {Path}", strmPath);
            return false;
        }

        return TryParse(strmPath, contents, allowedRoots, logger, out reference);
    }

    /// <summary>
    /// Parses the contents of a <c>.strm</c> file and, if its entry uses the <c>dvd://</c> or <c>bluray://</c>
    /// scheme, validates it.
    /// </summary>
    /// <param name="strmPath">The path of the <c>.strm</c> file; relative paths are resolved against its folder.</param>
    /// <param name="contents">The contents of the file.</param>
    /// <param name="allowedRoots">The folders the referenced source must be inside.</param>
    /// <param name="logger">The logger used to report invalid references.</param>
    /// <param name="reference">The disc reference, or <c>null</c> if the file is a disc reference but invalid.</param>
    /// <returns><c>true</c> if the file is a disc reference (valid or not); <c>false</c> for any other <c>.strm</c> file.</returns>
    public static bool TryParse(string strmPath, string contents, IEnumerable<string> allowedRoots, ILogger logger, out DiscReference? reference)
    {
        ArgumentException.ThrowIfNullOrEmpty(strmPath);
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(allowedRoots);
        ArgumentNullException.ThrowIfNull(logger);

        reference = null;
        var entry = GetEntryLine(contents);
        if (entry is null)
        {
            return false;
        }

        IsoType discType;
        string location;
        if (entry.StartsWith(DvdScheme, StringComparison.OrdinalIgnoreCase))
        {
            discType = IsoType.Dvd;
            location = entry[DvdScheme.Length..];
        }
        else if (entry.StartsWith(BluRayScheme, StringComparison.OrdinalIgnoreCase))
        {
            discType = IsoType.BluRay;
            location = entry[BluRayScheme.Length..];
        }
        else
        {
            return false;
        }

        reference = Parse(strmPath, discType, location, allowedRoots, logger);
        return true;
    }

    private static DiscReference? Parse(string strmPath, IsoType discType, string location, IEnumerable<string> allowedRoots, ILogger logger)
    {
        SplitLocation(location, out var source, out var query);
        if (string.IsNullOrWhiteSpace(source))
        {
            logger.LogWarning("Disc reference {Path} has no path", strmPath);
            return null;
        }

        int? index = null;
        foreach (var parameter in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = parameter.IndexOf('=', StringComparison.Ordinal);
            var name = separator < 0 ? parameter : parameter[..separator].Trim();
            var value = separator < 0 ? string.Empty : Uri.UnescapeDataString(parameter[(separator + 1)..]).Trim();

            if (string.Equals(name, "index", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "title", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "playlist", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedIndex))
                {
                    logger.LogWarning("Disc reference {Path} has an invalid {Name} ({Value}); expected a non-negative number", strmPath, name, value);
                    return null;
                }

                index = parsedIndex;
            }
            else
            {
                logger.LogWarning("Disc reference {Path} has an unknown parameter ({Name}), ignoring it", strmPath, name);
            }
        }

        string sourcePath;
        try
        {
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(strmPath)) ?? string.Empty;
            sourcePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source, baseDirectory));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            logger.LogWarning(ex, "Disc reference {Path} has an invalid path ({Source})", strmPath, source);
            return null;
        }

        if (!IsInsideAnyRoot(sourcePath, allowedRoots))
        {
            logger.LogWarning("Disc reference {Path} points outside of the library folders ({Source}), ignoring it", strmPath, sourcePath);
            return null;
        }

        if (File.Exists(sourcePath))
        {
            var extension = Path.GetExtension(sourcePath.AsSpan());
            if (!extension.Equals(".iso", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".img", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Disc reference {Path} must point at an .iso/.img image or a disc folder, not {Source}", strmPath, sourcePath);
                return null;
            }

            return new DiscReference(sourcePath, VideoType.Iso, discType, index);
        }

        if (Directory.Exists(sourcePath))
        {
            // Accept the VIDEO_TS/BDMV folder itself as well as the folder containing it.
            var folderName = Path.GetFileName(sourcePath);
            if (string.Equals(folderName, "VIDEO_TS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(folderName, "BDMV", StringComparison.OrdinalIgnoreCase))
            {
                sourcePath = Path.GetDirectoryName(sourcePath) ?? sourcePath;
            }

            var discFolder = discType == IsoType.Dvd ? "VIDEO_TS" : "BDMV";
            if (HasSubdirectory(sourcePath, discFolder))
            {
                return new DiscReference(sourcePath, discType == IsoType.Dvd ? VideoType.Dvd : VideoType.BluRay, discType, index);
            }

            logger.LogWarning("Disc reference {Path} points at {Source}, which has no {DiscFolder} folder", strmPath, sourcePath, discFolder);
            return null;
        }

        logger.LogWarning("Disc reference {Path} points at {Source}, which does not exist", strmPath, sourcePath);
        return null;
    }

    // Gets the first line that isn't blank or a # comment.
    private static string? GetEntryLine(string contents)
    {
        foreach (var rawLine in contents.Split('\n'))
        {
            var line = rawLine.Trim().TrimStart('\uFEFF');
            if (line.Length > 0 && !line.StartsWith('#'))
            {
                return line;
            }
        }

        return null;
    }

    // Splits the part after the scheme into its path and query string. A '?' only starts the query string when it
    // is the last one and is followed by nothing or by name=value pairs.
    private static void SplitLocation(string location, out string path, out string query)
    {
        var queryStart = location.LastIndexOf('?');
        if (queryStart >= 0 && (queryStart == location.Length - 1 || location.IndexOf('=', queryStart) > queryStart))
        {
            path = location[..queryStart].Trim();
            query = location[(queryStart + 1)..];
            return;
        }

        path = location.Trim();
        query = string.Empty;
    }

    private static bool IsInsideAnyRoot(string path, IEnumerable<string> roots)
    {
        foreach (var root in roots)
        {
            if (string.IsNullOrEmpty(root))
            {
                continue;
            }

            var parent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            if (path.Length > parent.Length
                && path.StartsWith(parent, _pathComparison)
                && (Path.EndsInDirectorySeparator(parent)
                    || path[parent.Length] == Path.DirectorySeparatorChar
                    || path[parent.Length] == Path.AltDirectorySeparatorChar))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSubdirectory(string path, string name)
    {
        try
        {
            return Directory.EnumerateDirectories(path)
                .Any(directory => string.Equals(Path.GetFileName(directory), name, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
