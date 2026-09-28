using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.EntryPoints;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EntryPoints;

public class LibraryChangedNotifierTests
{
    // How long a test waits for the notifier's timer callback to run. Generous: the assertions are
    // about a batch being sent at all, not about how promptly.
    private static readonly TimeSpan _flushTimeout = TimeSpan.FromSeconds(15);

    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IServerConfigurationManager> _configurationManager = new();
    private readonly Mock<ISessionManager> _sessionManager = new();
    private readonly Mock<IUserManager> _userManager = new();
    private readonly Mock<IProviderManager> _providerManager = new();
    private readonly ServerConfiguration _configuration = new();

    private int _flushCount;

    public LibraryChangedNotifierTests()
    {
        _configurationManager.SetupGet(e => e.Configuration).Returns(_configuration);

        // Reading the session list is the first thing a flush does, so it stands in for "a batch was
        // sent" without having to mock a whole user library behind it.
        _sessionManager.SetupGet(e => e.Sessions)
            .Returns(() =>
            {
                Interlocked.Increment(ref _flushCount);
                return [];
            });
    }

    [Fact]
    public async Task OnLibraryItemUpdated_BatchSizeCapReached_SendsWithoutWaitingForWindow()
    {
        // Long enough that only the size cap can close the batch.
        _configuration.LibraryUpdateDuration = 3600;

        var notifier = CreateNotifier();
        await notifier.StartAsync(TestContext.Current.CancellationToken);

        for (var i = 0; i < LibraryChangedNotifier.MaxBatchSize; i++)
        {
            RaiseItemUpdated();
        }

        Assert.True(await WaitForFlushAsync(1), "The batch was not sent once it hit the size cap.");

        await notifier.StopAsync(TestContext.Current.CancellationToken);
        notifier.Dispose();
    }

    [Fact]
    public async Task OnLibraryItemUpdated_ChangesNeverPause_StillSendsOnTheWindow()
    {
        // A scan changes items continuously. The window must run from the first change of a batch, or
        // the batch never closes and holds every item it named alive for the length of the scan.
        _configuration.LibraryUpdateDuration = 1;

        var notifier = CreateNotifier();
        await notifier.StartAsync(TestContext.Current.CancellationToken);

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < _flushTimeout && Volatile.Read(ref _flushCount) == 0)
        {
            // Well below the window, and well below the size cap over the whole loop.
            RaiseItemUpdated();
            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        Assert.True(Volatile.Read(ref _flushCount) > 0, "The batch was never sent while changes kept arriving.");

        await notifier.StopAsync(TestContext.Current.CancellationToken);
        notifier.Dispose();
    }

    private LibraryChangedNotifier CreateNotifier()
        => new(
            _libraryManager.Object,
            _configurationManager.Object,
            _sessionManager.Object,
            _userManager.Object,
            NullLogger<LibraryChangedNotifier>.Instance,
            _providerManager.Object);

    // A folder passes the notifier's item filter without needing any of BaseItem's static services.
    private void RaiseItemUpdated()
        => _libraryManager.Raise(
            e => e.ItemUpdated += null,
            _libraryManager.Object,
            new ItemChangeEventArgs { Item = new Folder { Id = Guid.NewGuid() } });

    private async Task<bool> WaitForFlushAsync(int expected)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < _flushTimeout)
        {
            if (Volatile.Read(ref _flushCount) >= expected)
            {
                return true;
            }

            await Task.Delay(25, TestContext.Current.CancellationToken);
        }

        return false;
    }
}
