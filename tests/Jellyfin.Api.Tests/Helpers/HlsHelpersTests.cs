using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32.SafeHandles;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public sealed class HlsHelpersTests : IDisposable
{
    private const string PlaylistText = "#EXTM3U\n#EXTINF:3.0,Café\nplaylist0.ts\n#EXTINF:3.0,\nplaylist1.ts\n";
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

    private string PlaylistPath => Path.Combine(_directory.FullName, "playlist.m3u8");

    [Fact]
    public void GetLivePlaylistText_PreservesText()
    {
        File.WriteAllText(PlaylistPath, PlaylistText);
        using var state = CreateState("ts");

        Assert.Equal(PlaylistText, HlsHelpers.GetLivePlaylistText(PlaylistPath, state));
    }

    [Fact]
    public void GetLivePlaylistText_ReadsWhileReplacementHandleIsOpen_Windows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requires Windows file sharing semantics and CreateFileW.");
        File.WriteAllText(PlaylistPath, "#EXTM3U\n");
        var temporaryPath = PlaylistPath + ".tmp";
        File.WriteAllText(temporaryPath, PlaylistText);
        using var replacementHandle = OpenDeleteHandle(temporaryPath);
        File.Move(temporaryPath, PlaylistPath, true);
        using var state = CreateState("ts");

        Assert.Equal(PlaylistText, HlsHelpers.GetLivePlaylistText(PlaylistPath, state));
    }

    [Fact]
    public void GetLivePlaylistText_DoesNotShareWithInPlaceWriter_Windows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requires Windows file sharing semantics.");
        File.WriteAllText(PlaylistPath, PlaylistText);
        using var writer = new FileStream(PlaylistPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        using var state = CreateState("ts");

        Assert.Throws<IOException>(() => HlsHelpers.GetLivePlaylistText(PlaylistPath, state));
    }

    [Fact]
    public async Task WaitForMinimumSegmentCount_ReadsWhileReplacementHandleIsOpen_Windows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Requires Windows file sharing semantics and CreateFileW.");
        await File.WriteAllTextAsync(PlaylistPath, "#EXTM3U\n", TestContext.Current.CancellationToken);
        var temporaryPath = PlaylistPath + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, PlaylistText, TestContext.Current.CancellationToken);
        using var replacementHandle = OpenDeleteHandle(temporaryPath);
        File.Move(temporaryPath, PlaylistPath, true);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(10));

        // The DELETE-access handle stays open for the entire wait; no overlap timing is required.
        await HlsHelpers.WaitForMinimumSegmentCount(PlaylistPath, 2, NullLogger.Instance, cancellation.Token);
        Assert.False(cancellation.IsCancellationRequested);
    }

    public void Dispose()
    {
        _directory.Delete(true);
    }

    private static StreamState CreateState(string container)
    {
        return new StreamState(Mock.Of<IMediaSourceManager>(), TranscodingJobType.Hls, Mock.Of<ITranscodeManager>())
        {
            Request = new StreamingRequestDto { SegmentContainer = container },
            MediaSource = new MediaSourceInfo()
        };
    }

    private static SafeFileHandle OpenDeleteHandle(string path)
    {
        // DELETE access without FILE_FLAG_DELETE_ON_CLOSE models FFmpeg's retained rename handle.
        const uint DeleteAccess = 0x00010000;
        const uint OpenExisting = 3;
        var handle = CreateFile(path, DeleteAccess, FileShare.ReadWrite | FileShare.Delete, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        Assert.False(handle.IsInvalid, new Win32Exception(Marshal.GetLastPInvokeError()).Message);
        return handle;
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        FileShare shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);
}
