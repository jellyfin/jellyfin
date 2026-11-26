using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Model.Dto;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// One clip whose cache entry cannot be verified, or whose probe times out, must not end the whole compatibility
    /// check: it becomes a skipped row, the other clips are still checked and its cache entry is kept.
    /// </summary>
    public sealed class TranscodeTestCompatibilityClipTests : IDisposable
    {
        private static readonly string[] _names = ["h264.mkv", "hevc8.mkv", "vp9.webm"];

        private readonly string _folder = Path.Combine(Path.GetTempPath(), "transcode-test-clips-" + Guid.NewGuid().ToString("N"));

        public TranscodeTestCompatibilityClipTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose() => Directory.Delete(_folder, true);

        [Theory]
        [InlineData("Could not verify test clip hevc8.mkv; its cache entry was preserved: [error] Cannot allocate memory")]
        [InlineData("Probing test clip hevc8.mkv timed out after 30 s; its cache entry was preserved.")]
        public async Task GetClipsAsync_OneInconclusiveCachedClipIsSkippedAndTheOthersAreChecked(string message)
        {
            await CacheAllAsync();
            var skipped = new List<(string Clip, string? Error, bool NotCreated)>();
            var clips = await TranscodeTestManager.GetClipsAsync(
                _names,
                (name, token) => GetAsync(name, name == "hevc8.mkv" ? message : null, token),
                (name, error, notCreated) => skipped.Add((name, error, notCreated)),
                TestContext.Current.CancellationToken);

            Assert.Equal(["h264.mkv", "vp9.webm"], clips.Select(c => c.Clip));
            Assert.Equal(["h264.mkv", "vp9.webm"], clips.Select(c => c.Source.Id));
            var row = Assert.Single(skipped);
            Assert.Equal("hevc8.mkv", row.Clip);
            Assert.Equal(message, row.Error);
            Assert.False(row.NotCreated);

            // An inconclusive verification says nothing about the clip, so the next run can still use it.
            Assert.Equal(_names.Length, Directory.GetFiles(_folder).Length);
        }

        [Fact]
        public async Task GetClipsAsync_AllClipsInconclusive_EveryClipIsSkippedAndTheCacheIsKept()
        {
            await CacheAllAsync();
            var skipped = new List<string>();
            var clips = await TranscodeTestManager.GetClipsAsync(
                _names,
                (name, token) => GetAsync(name, $"Could not verify test clip {name}; its cache entry was preserved: no error message", token),
                (name, error, notCreated) =>
                {
                    Assert.False(notCreated);
                    Assert.Contains(name, error, StringComparison.Ordinal);
                    skipped.Add(name);
                },
                TestContext.Current.CancellationToken);

            Assert.Empty(clips);
            Assert.Equal(_names, skipped);
            Assert.Equal(_names.Length, Directory.GetFiles(_folder).Length);
        }

        [Fact]
        public async Task GetClipsAsync_ClipThatCouldNotBeCreatedIsSkippedWithFfmpegsError()
        {
            var skipped = new List<(string Clip, string? Error, bool NotCreated)>();
            var clips = await TranscodeTestManager.GetClipsAsync(
                _names,
                (name, token) => Task.FromResult<(MediaSourceInfo? Source, string? Error)>(
                    name == "h264.mkv" ? (null, "exit code 1: Unknown encoder 'libx264'") : (new MediaSourceInfo { Id = name }, null)),
                (name, error, notCreated) => skipped.Add((name, error, notCreated)),
                TestContext.Current.CancellationToken);

            Assert.Equal(["hevc8.mkv", "vp9.webm"], clips.Select(c => c.Clip));
            Assert.Equal(("h264.mkv", "exit code 1: Unknown encoder 'libx264'", true), Assert.Single(skipped));
        }

        [Fact]
        public async Task GetClipsAsync_CancellationIsNotASkippedClip()
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var asked = new List<string>();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TranscodeTestManager.GetClipsAsync<string>(
                _names,
                async (name, token) =>
                {
                    asked.Add(name);
                    await cancellation.CancelAsync();
                    // A probe that was stopped by the user's cancellation may surface as an I/O error.
                    throw new IOException("Probing test clip " + name + " was stopped.");
                },
                (_, _, _) => Assert.Fail("A cancelled run must not record skipped clips."),
                cancellation.Token));

            Assert.Equal(["h264.mkv"], asked);
        }

        private async Task CacheAllAsync()
        {
            foreach (var name in _names)
            {
                await File.WriteAllBytesAsync(Path.Combine(_folder, name), [1, 2, 3], TestContext.Current.CancellationToken);
            }
        }

        // Uses the real cache with a probe that verifies the clip, or throws as an inconclusive verification does.
        private async Task<(MediaSourceInfo? Source, string? Error)> GetAsync(string name, string? inconclusive, CancellationToken cancellationToken)
            => await TranscodeTestClipCache.GetAsync<MediaSourceInfo>(
                Path.Combine(_folder, name),
                (_, _) => throw new InvalidOperationException("A cached clip must not be generated again."),
                (candidate, _) => inconclusive is null
                    ? Task.FromResult<MediaSourceInfo?>(new MediaSourceInfo { Id = Path.GetFileName(candidate) })
                    : throw new IOException(inconclusive),
                cancellationToken);
    }
}
