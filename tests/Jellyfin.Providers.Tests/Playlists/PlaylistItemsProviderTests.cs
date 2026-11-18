using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Playlists;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Playlists;

public sealed class PlaylistItemsProviderTests : IDisposable
{
    private const string AccentedFolder = "Música épica";
    private const string AccentedSong = "Canción.mp3";
    private const string AsciiSong = "Song.mp3";

    private readonly string _libraryRoot;
    private readonly PlaylistItemsProvider _sut;

    public PlaylistItemsProviderTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        _libraryRoot = Path.Combine(Path.GetTempPath(), "jellyfin-playlist-tests", Guid.NewGuid().ToString("N"));
        var mediaFolder = Path.Combine(_libraryRoot, AccentedFolder);
        Directory.CreateDirectory(mediaFolder);
        File.WriteAllText(Path.Combine(mediaFolder, AccentedSong), string.Empty);
        File.WriteAllText(Path.Combine(mediaFolder, AsciiSong), string.Empty);

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.FindByPath(It.IsAny<string>(), It.IsAny<bool?>()))
            .Returns((string path, bool? _) => new Audio { Id = Guid.NewGuid(), Path = path });

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(m => m.MakeAbsolutePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string folderPath, string filePath) => Path.GetFullPath(Path.Combine(folderPath, filePath)));

        _sut = new PlaylistItemsProvider(NullLogger<PlaylistItemsProvider>.Instance, libraryManager.Object, fileSystem.Object);
    }

    [Fact]
    public void GetM3uItems_Utf8Entries_ResolvesAccentedPaths()
        => AssertResolved(Encoding.UTF8, [$"{AccentedFolder}/{AccentedSong}"], 1);

    /// <summary>
    /// Playlists written by Windows media players default to the local codepage rather than UTF-8.
    /// Decoding those as UTF-8 mangles every accented character and loses the entry.
    /// </summary>
    [Fact]
    public void GetM3uItems_LegacyCodepageEntries_ResolvesAccentedPaths()
        => AssertResolved(Encoding.GetEncoding(1252), [$"{AccentedFolder}/{AccentedSong}"], 1);

    /// <summary>
    /// The files on disk are stored precomposed (NFC), the playlist references them decomposed (NFD).
    /// </summary>
    [Fact]
    public void GetM3uItems_DecomposedEntries_ResolvesAccentedPaths()
        => AssertResolved(Encoding.UTF8, [$"{AccentedFolder}/{AccentedSong}".Normalize(NormalizationForm.FormD)], 1);

    [Fact]
    public void GetM3uItems_AsciiEntries_ResolvesPaths()
        => AssertResolved(Encoding.UTF8, [$"{AccentedFolder}/{AsciiSong}"], 1);

    [Fact]
    public void GetM3uItems_Utf16Entries_ResolvesAccentedPaths()
        => AssertResolved(Encoding.Unicode, [$"{AccentedFolder}/{AccentedSong}"], 1);

    [Fact]
    public void GetM3uItems_MixedEntries_ResolvesEveryEntry()
        => AssertResolved(
            Encoding.GetEncoding(1252),
            [$"{AccentedFolder}/{AccentedSong}", $"{AccentedFolder}/{AsciiSong}"],
            2);

    [Fact]
    public void GetM3uItems_MissingFile_ResolvesNothing()
        => AssertResolved(Encoding.UTF8, [$"{AccentedFolder}/Does not exist.mp3"], 0);

    public void Dispose()
    {
        if (Directory.Exists(_libraryRoot))
        {
            Directory.Delete(_libraryRoot, true);
        }
    }

    private void AssertResolved(Encoding encoding, string[] entries, int expected)
    {
        var playlistPath = Path.Combine(_libraryRoot, "playlist.m3u");
        var content = new StringBuilder("#EXTM3U\n");
        foreach (var entry in entries)
        {
            content.Append("#EXTINF:1,Title\n").Append(entry).Append('\n');
        }

        File.WriteAllBytes(
            playlistPath,
            [.. encoding.GetPreamble(), .. encoding.GetBytes(content.ToString())]);

        using var stream = File.OpenRead(playlistPath);
        var resolved = _sut.GetM3uItems(stream, playlistPath, [_libraryRoot]).ToList();

        Assert.Equal(expected, resolved.Count);
    }
}
