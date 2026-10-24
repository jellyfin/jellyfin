using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Encoder;

public class ProcessWrapperTests
{
    [Fact]
    public async Task ExitedProcess_StaysUsableForTheCallerThatStartedIt()
    {
        using var process = CreateProcess();
        using var exitHandled = new ManualResetEventSlim(false);

        using (var wrapper = new MediaEncoder.ProcessWrapper(process, CreateEncoder()))
        {
            // Subscribed after the wrapper, so by the time this is set the wrapper's own handler has
            // already run: whatever it does to the process has happened.
            process.Exited += (_, _) => exitHandled.Set();

            process.Start();
            await process.WaitForExitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.True(exitHandled.Wait(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken), "The process never raised Exited.");

            // The caller still owns the process here. Disposing it from the exit handler handed
            // whoever exited quickest an ObjectDisposedException out of these three lines.
            var output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal("jellyfin", output.Trim());

            Assert.True(wrapper.HasExited);
            Assert.Equal(3, wrapper.ExitCode);
        }
    }

    [Fact]
    public async Task ExitState_IsReadableBeforeTheExitEventArrives()
    {
        using var process = CreateProcess();

        using (var wrapper = new MediaEncoder.ProcessWrapper(process, CreateEncoder()))
        {
            process.Start();
            await process.WaitForExitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            // The exit event is raised on the thread pool and can lag behind the wait that just
            // returned, so neither of these may depend on it having arrived.
            Assert.True(wrapper.HasExited);
            Assert.Equal(3, wrapper.ExitCode);
        }
    }

    [Fact]
    public async Task ExitCode_SurvivesDisposal()
    {
        using var process = CreateProcess();
        var wrapper = new MediaEncoder.ProcessWrapper(process, CreateEncoder());

        process.Start();
        await process.WaitForExitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var exitCode = wrapper.ExitCode;
        wrapper.Dispose();

        Assert.Equal(exitCode, wrapper.ExitCode);
        Assert.True(wrapper.HasExited);
    }

    private static MediaEncoder CreateEncoder()
        => new(
            Mock.Of<ILogger<MediaEncoder>>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<IFileSystem>(),
            Mock.Of<IBlurayExaminer>(),
            Mock.Of<ILocalizationManager>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<IServerConfigurationManager>());

    // Writes to stdout and exits immediately with a non-zero code, standing in for the ffprobe that
    // rejects a file outright - the process that used to win the race against its own caller.
    private static Process CreateProcess()
    {
        var startInfo = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe", "/c echo jellyfin & exit 3")
            : new ProcessStartInfo("/bin/sh", "-c \"printf 'jellyfin\\n'; exit 3\"");

        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;

        return new Process { StartInfo = startInfo, EnableRaisingEvents = true };
    }
}
