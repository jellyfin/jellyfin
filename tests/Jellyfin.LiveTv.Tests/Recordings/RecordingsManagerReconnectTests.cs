using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Recordings;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Recordings
{
    public class RecordingsManagerReconnectTests
    {
        private const int MaxEmptyReconnects = 5;
        private static readonly TimeSpan _fastDelay = TimeSpan.FromMilliseconds(1);
        private static readonly TimeSpan _minRemaining = TimeSpan.FromSeconds(10);

        [Fact]
        public async Task RecordWithReconnectAsync_CompletesFirstConnection_RecordsOnceWithoutAppend()
        {
            var appends = new List<bool>();
            var done = false;

            await RecordingsManager.RecordWithReconnectAsync(
                (append, remaining, ct) =>
                {
                    appends.Add(append);
                    done = true;
                    return Task.CompletedTask;
                },
                () => done ? TimeSpan.Zero : TimeSpan.FromMinutes(30),
                () => 1,
                MaxEmptyReconnects,
                _minRemaining,
                _fastDelay,
                _fastDelay,
                NullLogger.Instance,
                CancellationToken.None);

            Assert.Equal(new[] { false }, appends);
        }

        [Fact]
        public async Task RecordWithReconnectAsync_StreamDropsBeforeEnd_ReconnectsAndAppendsUntilEnd()
        {
            var appends = new List<bool>();
            var elapsedSeconds = 0;
            var bytes = 0L;
            const int TotalSeconds = 100;

            await RecordingsManager.RecordWithReconnectAsync(
                (append, remaining, ct) =>
                {
                    appends.Add(append);

                    // Simulate each connection recording ~30s of data before the source drops.
                    elapsedSeconds += 30;
                    bytes += 1_000_000;
                    return Task.CompletedTask;
                },
                () => TimeSpan.FromSeconds(TotalSeconds - elapsedSeconds),
                () => bytes,
                MaxEmptyReconnects,
                _minRemaining,
                _fastDelay,
                _fastDelay,
                NullLogger.Instance,
                CancellationToken.None);

            // First connection creates the file; every reconnect appends until the scheduled end.
            Assert.Equal(new[] { false, true, true }, appends);
        }

        [Fact]
        public async Task RecordWithReconnectAsync_NoTimeRemaining_DoesNotRecord()
        {
            var calls = 0;

            await RecordingsManager.RecordWithReconnectAsync(
                (append, remaining, ct) =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                () => TimeSpan.FromSeconds(5),
                () => 1,
                MaxEmptyReconnects,
                _minRemaining,
                _fastDelay,
                _fastDelay,
                NullLogger.Instance,
                CancellationToken.None);

            Assert.Equal(0, calls);
        }

        [Fact]
        public async Task RecordWithReconnectAsync_SegmentCancelled_PropagatesAndStops()
        {
            var calls = 0;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                RecordingsManager.RecordWithReconnectAsync(
                    (append, remaining, ct) =>
                    {
                        calls++;
                        throw new OperationCanceledException();
                    },
                    () => TimeSpan.FromMinutes(30),
                    () => 1,
                    MaxEmptyReconnects,
                    _minRemaining,
                    _fastDelay,
                    _fastDelay,
                    NullLogger.Instance,
                    CancellationToken.None));

            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task RecordWithReconnectAsync_CancelledDuringReconnect_Stops()
        {
            var calls = 0;
            using var cts = new CancellationTokenSource();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                RecordingsManager.RecordWithReconnectAsync(
                    async (append, remaining, ct) =>
                    {
                        calls++;
                        if (calls >= 2)
                        {
                            await cts.CancelAsync();
                        }
                    },
                    () => TimeSpan.FromMinutes(30),
                    () => calls,
                    MaxEmptyReconnects,
                    _minRemaining,
                    _fastDelay,
                    _fastDelay,
                    NullLogger.Instance,
                    cts.Token));

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task RecordWithReconnectAsync_NeverCapturesData_AbortsAfterMaxEmptyReconnects()
        {
            var calls = 0;

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                RecordingsManager.RecordWithReconnectAsync(
                    (append, remaining, ct) =>
                    {
                        calls++;
                        return Task.CompletedTask;
                    },
                    () => TimeSpan.FromMinutes(30),
                    () => 0,
                    MaxEmptyReconnects,
                    _minRemaining,
                    _fastDelay,
                    _fastDelay,
                    NullLogger.Instance,
                    CancellationToken.None));

            // Aborts once MaxEmptyReconnects consecutive connections have each captured no data.
            Assert.Equal(MaxEmptyReconnects, calls);
        }
    }
}
