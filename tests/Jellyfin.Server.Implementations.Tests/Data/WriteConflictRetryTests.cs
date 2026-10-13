using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public class WriteConflictRetryTests
{
    private readonly List<TimeSpan> _delays = [];
    private readonly CapturingLogger _logger = new();
    private readonly WriteConflictRetry _retry;

    public WriteConflictRetryTests()
    {
        var provider = new Mock<IJellyfinDatabaseProvider>();
        provider.Setup(p => p.IsRetryableWriteConflict(It.IsAny<Exception>()))
            .Returns((Exception e) => e is ConflictException);

        _retry = new WriteConflictRetry(
            provider.Object,
            _logger,
            (delay, _) =>
            {
                _delays.Add(delay);
                return Task.CompletedTask;
            },
            (delay, _) => _delays.Add(delay));
    }

    [Fact]
    public void Execute_RetryableError_RunsAgainAndSucceeds()
    {
        var attempts = 0;

        _retry.Execute(
            () =>
            {
                if (++attempts < 3)
                {
                    throw new ConflictException();
                }
            },
            CancellationToken.None);

        Assert.Equal(3, attempts);
        Assert.Equal(2, _delays.Count);
    }

    [Fact]
    public void Execute_RetryableErrorEveryTime_StopsAfterThreeAttemptsAndRethrowsTheLast()
    {
        var thrown = new List<ConflictException>();

        var exception = Assert.Throws<ConflictException>(() => _retry.Execute(
            () =>
            {
                thrown.Add(new ConflictException());
                throw thrown[^1];
            },
            CancellationToken.None));

        Assert.Equal(3, thrown.Count);
        Assert.Same(thrown[2], exception);
        Assert.Equal(2, _delays.Count);
    }

    [Fact]
    public void Execute_NonRetryableError_IsNotRetried()
    {
        var attempts = 0;
        var failure = new InvalidOperationException();

        var exception = Assert.Throws<InvalidOperationException>(() => _retry.Execute(
            () =>
            {
                attempts++;
                throw failure;
            },
            CancellationToken.None));

        Assert.Same(failure, exception);
        Assert.Equal(1, attempts);
        Assert.Empty(_delays);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public void Execute_Cancelled_DoesNotStartAnotherAttempt()
    {
        using var cts = new CancellationTokenSource();
        var attempts = 0;

        Assert.Throws<OperationCanceledException>(() => _retry.Execute(
            () =>
            {
                attempts++;
                cts.Cancel();
                throw new ConflictException();
            },
            cts.Token));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_RetryableError_RunsAgainAndSucceeds()
    {
        var attempts = 0;

        await _retry.ExecuteAsync(
            _ =>
            {
                if (++attempts < 3)
                {
                    throw new ConflictException();
                }

                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(3, attempts);
        Assert.Equal(2, _delays.Count);
    }

    [Fact]
    public async Task ExecuteAsync_RetryableErrorEveryTime_StopsAfterThreeAttemptsAndRethrowsTheLast()
    {
        var thrown = new List<ConflictException>();

        var exception = await Assert.ThrowsAsync<ConflictException>(() => _retry.ExecuteAsync(
            async _ =>
            {
                await Task.Yield();
                thrown.Add(new ConflictException());
                throw thrown[^1];
            },
            CancellationToken.None));

        Assert.Equal(3, thrown.Count);
        Assert.Same(thrown[2], exception);
        Assert.Equal(2, _delays.Count);
    }

    [Fact]
    public async Task ExecuteAsync_NonRetryableError_IsNotRetried()
    {
        var attempts = 0;
        var failure = new InvalidOperationException();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _retry.ExecuteAsync(
            _ =>
            {
                attempts++;
                throw failure;
            },
            CancellationToken.None));

        Assert.Same(failure, exception);
        Assert.Equal(1, attempts);
        Assert.Empty(_delays);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task ExecuteAsync_ErrorWrappedAroundAConflict_IsRetriedWhenTheProviderSaysSo()
    {
        var provider = new Mock<IJellyfinDatabaseProvider>();
        provider.Setup(p => p.IsRetryableWriteConflict(It.IsAny<Exception>()))
            .Returns((Exception e) => e.InnerException is ConflictException);
        var retry = new WriteConflictRetry(provider.Object, _logger, (_, _) => Task.CompletedTask, (_, _) => { });
        var attempts = 0;

        await retry.ExecuteAsync(
            _ =>
            {
                if (++attempts < 2)
                {
                    throw new InvalidOperationException("wrapper", new ConflictException());
                }

                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_Cancelled_DoesNotStartAnotherAttempt()
    {
        using var cts = new CancellationTokenSource();
        var attempts = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _retry.ExecuteAsync(
            async _ =>
            {
                attempts++;
                await cts.CancelAsync();
                throw new ConflictException();
            },
            cts.Token));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_Retry_WaitsWithTheCallersToken()
    {
        using var cts = new CancellationTokenSource();
        var provider = new Mock<IJellyfinDatabaseProvider>();
        provider.Setup(p => p.IsRetryableWriteConflict(It.IsAny<Exception>())).Returns(true);
        var tokens = new List<CancellationToken>();
        var retry = new WriteConflictRetry(
            provider.Object,
            _logger,
            (_, token) =>
            {
                tokens.Add(token);
                return Task.CompletedTask;
            },
            (_, _) => { });

        await Assert.ThrowsAsync<ConflictException>(() => retry.ExecuteAsync(
            _ => throw new ConflictException(),
            cts.Token));

        Assert.Equal([cts.Token, cts.Token], tokens);
    }

    [Fact]
    public void WaitForDelay_CancelledDuringTheWait_StopsWaiting()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var stopwatch = Stopwatch.StartNew();

        Assert.Throws<OperationCanceledException>(() => WriteConflictRetry.WaitForDelay(TimeSpan.FromMinutes(1), cts.Token));

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"The wait took {stopwatch.Elapsed}.");
    }

    [Fact]
    public void Execute_Retries_WaitBetweenFiftyAndTwoHundredFiftyMilliseconds()
    {
        for (var i = 0; i < 50; i++)
        {
            Assert.Throws<ConflictException>(() => _retry.Execute(() => throw new ConflictException(), CancellationToken.None));
        }

        Assert.Equal(100, _delays.Count);
        Assert.All(_delays, delay => Assert.InRange(delay, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250)));
        Assert.True(_delays.Distinct().Count() > 1, "The waits are expected to be jittered.");
    }

    [Fact]
    public async Task ExecuteAsync_Retries_LogOneWarningEachWithoutTheExceptionMessage()
    {
        await Assert.ThrowsAsync<ConflictException>(() => _retry.ExecuteAsync(
            _ => throw new ConflictException("duplicate key value (Type, Value)=(1, Rock)"),
            CancellationToken.None));

        Assert.Equal(2, _logger.Entries.Count);
        Assert.All(_logger.Entries, entry =>
        {
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Null(entry.Exception);
            Assert.Contains(typeof(ConflictException).FullName!, entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("Rock", entry.Message, StringComparison.Ordinal);
        });
        Assert.Contains("attempt 1 of 3", _logger.Entries[0].Message, StringComparison.Ordinal);
        Assert.Contains("attempt 2 of 3", _logger.Entries[1].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_Succeeds_LogsNothing()
    {
        _retry.Execute(() => { }, CancellationToken.None);

        Assert.Empty(_logger.Entries);
        Assert.Empty(_delays);
    }

    private sealed class ConflictException : Exception
    {
        public ConflictException()
            : this("conflict")
        {
        }

        public ConflictException(string message)
            : base(message)
        {
        }

        public ConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
