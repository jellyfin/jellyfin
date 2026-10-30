using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Moq;
using Xunit;
using ServerLibraryManager = Emby.Server.Implementations.Library.LibraryManager;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class LibraryManagerScanTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void CanRemoveMissingItem_RequiresEnumerableLibraryRoot(bool canEnumerate, bool expected)
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var fileSystem = fixture.Freeze<Mock<IFileSystem>>();
        fileSystem.Setup(fs => fs.ContainsSubPath("/media", "/media/deleted.mkv")).Returns(true);
        fileSystem.Setup(fs => fs.CanEnumerateDirectory("/media")).Returns(canEnumerate);
        var manager = fixture.Create<ServerLibraryManager>();

        Assert.Equal(expected, manager.CanRemoveMissingItem("/media/deleted.mkv", "/media"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StartScanInBackground_QueuesOnlyWhenIdle(bool scanRunning)
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configuration = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configuration.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        configuration.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        var tasks = fixture.Freeze<Mock<ITaskManager>>();
        var manager = fixture.Create<ServerLibraryManager>();
        typeof(ServerLibraryManager).GetProperty(nameof(ServerLibraryManager.IsScanRunning))!.SetValue(manager, scanRunning);

        await manager.StartScanInBackground().ConfigureAwait(true);

        tasks.Verify(t => t.QueueScheduledTask<RefreshMediaLibraryTask>(), scanRunning ? Times.Never() : Times.Once());
        tasks.Verify(t => t.CancelIfRunningAndQueue<RefreshMediaLibraryTask>(), Times.Never());
    }

    [Fact]
    public async Task ValidateMediaLibrary_RestartsScheduledScan()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configuration = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configuration.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        configuration.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        var tasks = fixture.Freeze<Mock<ITaskManager>>();
        var manager = fixture.Create<ServerLibraryManager>();

        await manager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None).ConfigureAwait(true);

        tasks.Verify(t => t.CancelIfRunningAndQueue<RefreshMediaLibraryTask>(), Times.Once());
        tasks.Verify(t => t.QueueScheduledTask<RefreshMediaLibraryTask>(), Times.Never());
    }
}
