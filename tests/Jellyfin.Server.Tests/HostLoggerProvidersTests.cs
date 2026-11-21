using Jellyfin.Server.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests;

public class HostLoggerProvidersTests
{
    [Fact]
    public void CreateLogger_AttachedHost_ForwardsToItsProviders()
    {
        var hostLoggerProviders = new HostLoggerProviders();
        var logger = new Mock<ILogger>();
        var provider = new Mock<ILoggerProvider>();
        provider.Setup(p => p.CreateLogger("Category")).Returns(logger.Object);

        hostLoggerProviders.Attach().AddProvider(provider.Object);

        Assert.Same(logger.Object, hostLoggerProviders.CreateLogger("Category"));
    }

    [Fact]
    public void CreateLogger_AfterRestart_DropsTheProvidersOfTheStoppedHost()
    {
        var hostLoggerProviders = new HostLoggerProviders();
        var stoppedProvider = new Mock<ILoggerProvider>();
        var stopped = hostLoggerProviders.Attach();
        stopped.AddProvider(stoppedProvider.Object);
        hostLoggerProviders.Detach(stopped);

        Assert.Same(NullLogger.Instance, hostLoggerProviders.CreateLogger("Category"));

        var runningLogger = new Mock<ILogger>();
        var runningProvider = new Mock<ILoggerProvider>();
        runningProvider.Setup(p => p.CreateLogger("Category")).Returns(runningLogger.Object);
        hostLoggerProviders.Attach().AddProvider(runningProvider.Object);

        Assert.Same(runningLogger.Object, hostLoggerProviders.CreateLogger("Category"));
        stoppedProvider.Verify(p => p.CreateLogger(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Detach_OfAnEarlierHost_KeepsTheCurrentHostAttached()
    {
        var hostLoggerProviders = new HostLoggerProviders();
        var earlier = hostLoggerProviders.Attach();
        var current = hostLoggerProviders.Attach();
        var provider = new Mock<ILoggerProvider>();
        provider.Setup(p => p.CreateLogger("Category")).Returns(Mock.Of<ILogger>());
        current.AddProvider(provider.Object);

        hostLoggerProviders.Detach(earlier);

        Assert.NotSame(NullLogger.Instance, hostLoggerProviders.CreateLogger("Category"));
    }
}
