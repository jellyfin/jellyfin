using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public sealed class TranscodeTestClipCacheTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "transcode-test-cache-" + Guid.NewGuid().ToString("N"));

        public TranscodeTestClipCacheTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose() => Directory.Delete(_folder, true);

        [Fact]
        public async Task GetAsync_CancelledGenerationLeavesNoCacheEntryAndNextRunSucceeds()
        {
            var path = Path.Combine(_folder, "h264.mkv");
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TranscodeTestClipCache.GetAsync<string>(
                path,
                async (newTemporary, token) =>
                {
                    var temporary = newTemporary();
                    Assert.EndsWith(".mkv", temporary, StringComparison.Ordinal);
                    await File.WriteAllBytesAsync(temporary, [1], token);
                    Assert.False(File.Exists(path));
                    await cancellation.CancelAsync();
                    token.ThrowIfCancellationRequested();
                    return (temporary, null);
                },
                Valid,
                cancellation.Token));

            Assert.Empty(Directory.GetFiles(_folder));
            var (value, error) = await TranscodeTestClipCache.GetAsync(path, Generate, Valid, TestContext.Current.CancellationToken);
            Assert.Equal("video", value);
            Assert.Null(error);
            Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            Assert.Single(Directory.GetFiles(_folder));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetAsync_ReplacesEmptyOrUnprobeableExistingClip(bool empty)
        {
            var path = Path.Combine(_folder, "hevc.mkv");
            await File.WriteAllBytesAsync(path, empty ? [] : [9], TestContext.Current.CancellationToken);
            var generated = false;
            var (value, error) = await TranscodeTestClipCache.GetAsync<string>(
                path,
                async (newTemporary, token) =>
                {
                    generated = true;
                    return await Generate(newTemporary, token);
                },
                async (candidate, token) =>
                {
                    var data = await File.ReadAllBytesAsync(candidate, token);
                    return data.Length == 3 ? "video" : null;
                },
                TestContext.Current.CancellationToken);

            Assert.True(generated);
            Assert.Equal("video", value);
            Assert.Null(error);
            Assert.Single(Directory.GetFiles(_folder));
        }

        [Fact]
        public async Task GetAsync_ValidCachedClipIsNotRegenerated()
        {
            var path = Path.Combine(_folder, "vp9.webm");
            await File.WriteAllBytesAsync(path, [1], TestContext.Current.CancellationToken);
            var (value, error) = await TranscodeTestClipCache.GetAsync<string>(
                path,
                (_, _) => throw new InvalidOperationException("must not regenerate"),
                Valid,
                TestContext.Current.CancellationToken);
            Assert.Equal("video", value);
            Assert.Null(error);
        }

        [Fact]
        public async Task GetAsync_HeaderValidButShortSoftwareDecode_RegeneratesAndNextRunReusesTheClip()
        {
            var path = Path.Combine(_folder, "h264.mkv");
            await File.WriteAllBytesAsync(path, [84], TestContext.Current.CancellationToken);
            var generations = 0;
            async Task<string?> Probe(string candidate, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                // Both entries retain valid four-second metadata; only the full software decode tells them apart.
                var frames = (await File.ReadAllBytesAsync(candidate, token))[0];
                return TranscodeTestClipValidation.IsCompleteSoftwareDecode(0, frames, string.Empty) == true ? "video" : null;
            }

            async Task<(string? Output, string? Error)> GenerateComplete(Func<string> newTemporary, CancellationToken token)
            {
                generations++;
                var temporary = newTemporary();
                await File.WriteAllBytesAsync(temporary, [96], token);
                return (temporary, null);
            }

            var result = await TranscodeTestClipCache.GetAsync(path, GenerateComplete, Probe, TestContext.Current.CancellationToken);
            Assert.Equal("video", result.Value);
            Assert.Null(result.Error);
            result = await TranscodeTestClipCache.GetAsync(path, GenerateComplete, Probe, TestContext.Current.CancellationToken);
            Assert.Equal("video", result.Value);
            Assert.Equal(1, generations);
            Assert.Equal([96], await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            Assert.Single(Directory.GetFiles(_folder));
        }

        [Fact]
        public async Task GetAsync_InconclusiveVerificationPreservesTheExistingCacheEntry()
        {
            var path = Path.Combine(_folder, "h264.mkv");
            await File.WriteAllBytesAsync(path, [96], TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<IOException>(() => TranscodeTestClipCache.GetAsync<string>(
                path,
                (_, _) => throw new InvalidOperationException("must not regenerate"),
                (_, _) => throw new IOException("Software verification could not run."),
                TestContext.Current.CancellationToken));
            Assert.Equal([96], await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            Assert.Single(Directory.GetFiles(_folder));
        }

        [Fact]
        public async Task GetAsync_UnprobeableGeneratedClipIsNotPublished()
        {
            var path = Path.Combine(_folder, "av1.mkv");
            var (value, error) = await TranscodeTestClipCache.GetAsync<string>(
                path,
                Generate,
                (_, _) => Task.FromResult<string?>(null),
                TestContext.Current.CancellationToken);
            Assert.Null(value);
            Assert.NotNull(error);
            Assert.Empty(Directory.GetFiles(_folder));
        }

        [Fact]
        public async Task GetAsync_CancelledProbeIsNeverPublished()
        {
            var path = Path.Combine(_folder, "h264.mkv");
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TranscodeTestClipCache.GetAsync<string>(
                path,
                Generate,
                async (_, _) =>
                {
                    await cancellation.CancelAsync();
                    return "video";
                },
                cancellation.Token));
            Assert.Empty(Directory.GetFiles(_folder));
        }

        [Fact]
        public async Task GenerateAsync_RunTimeout_RetriesSmallIntoItsOwnFile()
        {
            var path = Path.Combine(_folder, "hevc10.mkv");
            var attempts = new System.Collections.Generic.List<(string Path, bool Small)>();
            var (value, error) = await TranscodeTestClipCache.GetAsync(
                path,
                (newTemporary, token) => TranscodeTestClipCache.GenerateAsync(
                    newTemporary,
                    async (temporary, small, attemptToken) =>
                    {
                        attempts.Add((temporary, small));
                        if (!small)
                        {
                            // Started, wrote part of its file, then was killed after the run timeout.
                            await File.WriteAllBytesAsync(temporary, [7], attemptToken);
                            return (false, true, "timed out after 60 s");
                        }

                        await File.WriteAllBytesAsync(temporary, [1, 2, 3], attemptToken);
                        return (true, true, string.Empty);
                    },
                    token),
                Valid,
                TestContext.Current.CancellationToken);

            Assert.Equal("video", value);
            Assert.Null(error);
            Assert.Equal([false, true], attempts.ConvertAll(a => a.Small));
            Assert.NotEqual(attempts[0].Path, attempts[1].Path);
            Assert.EndsWith(".partial.mkv", attempts[1].Path, StringComparison.Ordinal);
            Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
            Assert.Single(Directory.GetFiles(_folder));
        }

        [Fact]
        public async Task GenerateAsync_LaunchNeverStarted_DoesNotRetry()
        {
            var path = Path.Combine(_folder, "h264.mkv");
            var attempts = 0;
            var (value, error) = await TranscodeTestClipCache.GetAsync<string>(
                path,
                (newTemporary, token) => TranscodeTestClipCache.GenerateAsync(
                    newTemporary,
                    (_, _, _) =>
                    {
                        attempts++;
                        return Task.FromResult((false, false, "ffmpeg did not start within 30 s"));
                    },
                    token),
                Valid,
                TestContext.Current.CancellationToken);

            Assert.Null(value);
            Assert.Equal("ffmpeg did not start within 30 s", error);
            Assert.Equal(1, attempts);
            Assert.Empty(Directory.GetFiles(_folder));
        }

        [Fact]
        public async Task GenerateAsync_BothAttemptsFail_LeaveNoPartialFiles()
        {
            var path = Path.Combine(_folder, "vp9.webm");
            var (value, error) = await TranscodeTestClipCache.GetAsync<string>(
                path,
                (newTemporary, token) => TranscodeTestClipCache.GenerateAsync(
                    newTemporary,
                    async (temporary, small, attemptToken) =>
                    {
                        await File.WriteAllBytesAsync(temporary, [small ? (byte)2 : (byte)1], attemptToken);
                        return (false, true, small ? "small failed" : "full failed");
                    },
                    token),
                Valid,
                TestContext.Current.CancellationToken);

            Assert.Null(value);
            Assert.Equal("small failed", error);
            Assert.Empty(Directory.GetFiles(_folder));
        }

        [Fact]
        public async Task GenerateAsync_CancelledDuringTheRetry_LeavesNoPartialFiles()
        {
            var path = Path.Combine(_folder, "av1.mkv");
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TranscodeTestClipCache.GetAsync<string>(
                path,
                (newTemporary, token) => TranscodeTestClipCache.GenerateAsync(
                    newTemporary,
                    async (temporary, small, attemptToken) =>
                    {
                        await File.WriteAllBytesAsync(temporary, [1], attemptToken);
                        if (small)
                        {
                            await cancellation.CancelAsync();
                            attemptToken.ThrowIfCancellationRequested();
                        }

                        return (false, true, "failed");
                    },
                    token),
                Valid,
                cancellation.Token));

            Assert.Empty(Directory.GetFiles(_folder));
        }

        private static async Task<(string? Output, string? Error)> Generate(Func<string> newTemporary, CancellationToken token)
        {
            var path = newTemporary();
            await File.WriteAllBytesAsync(path, [1, 2, 3], token);
            return (path, null);
        }

        private static Task<string?> Valid(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<string?>(File.Exists(path) ? "video" : null);
        }
    }
}
