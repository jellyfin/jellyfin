using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Recordings;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Recordings
{
    public class RecordingsManagerRetryTests
    {
        private static readonly TimeSpan _fastDelay = TimeSpan.FromMilliseconds(1);

        [Fact]
        public async Task OpenWithRetryAsync_SucceedsFirstTry_CallsOnce()
        {
            var calls = 0;

            var result = await RecordingsManager.OpenWithRetryAsync(
                _ =>
                {
                    calls++;
                    return Task.FromResult(42);
                },
                maxAttempts: 3,
                _fastDelay,
                _fastDelay,
                NullLogger.Instance,
                CancellationToken.None);

            Assert.Equal(42, result);
            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task OpenWithRetryAsync_FailsThenSucceeds_RetriesUntilSuccess()
        {
            var calls = 0;

            var result = await RecordingsManager.OpenWithRetryAsync(
                _ =>
                {
                    calls++;
                    if (calls < 3)
                    {
                        throw new InvalidOperationException("transient open failure");
                    }

                    return Task.FromResult("ok");
                },
                maxAttempts: 3,
                _fastDelay,
                _fastDelay,
                NullLogger.Instance,
                CancellationToken.None);

            Assert.Equal("ok", result);
            Assert.Equal(3, calls);
        }

        [Fact]
        public async Task OpenWithRetryAsync_AlwaysFails_ThrowsAfterMaxAttempts()
        {
            var calls = 0;

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                RecordingsManager.OpenWithRetryAsync<int>(
                    _ =>
                    {
                        calls++;
                        throw new InvalidOperationException("always fails");
                    },
                    maxAttempts: 4,
                    _fastDelay,
                    _fastDelay,
                    NullLogger.Instance,
                    CancellationToken.None));

            Assert.Equal("always fails", ex.Message);
            Assert.Equal(4, calls);
        }

        [Fact]
        public async Task OpenWithRetryAsync_Cancelled_DoesNotRetry()
        {
            var calls = 0;
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                RecordingsManager.OpenWithRetryAsync<int>(
                    _ =>
                    {
                        calls++;
                        throw new OperationCanceledException();
                    },
                    maxAttempts: 5,
                    _fastDelay,
                    _fastDelay,
                    NullLogger.Instance,
                    cts.Token));

            Assert.Equal(1, calls);
        }
    }
}
