using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.LiveTv.TunerHosts;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests.TunerHosts;

public sealed class RollingChunkStreamTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ILogger _logger = Mock.Of<ILogger>();

    public RollingChunkStreamTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private string WriteChunk(string name, byte[] content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    [Fact]
    public async Task ReadAsync_ReturnsBytesFromInitialChunk()
    {
        var data = Encoding.UTF8.GetBytes("hello world");
        var path = WriteChunk("chunk_0.ts", data);
        var chunkPaths = new List<string> { path };

        using var stream = new RollingChunkStream(chunkPaths, new object(), _logger, seekToLiveEdge: false);

        var buffer = new byte[data.Length];
        int read = await stream.ReadAsync(buffer, TestContext.Current.CancellationToken);

        Assert.Equal(data.Length, read);
        Assert.Equal(data, buffer[..read]);
    }

    [Fact]
    public async Task ReadAsync_ReturnsZero_AtLiveEdge()
    {
        var data = Encoding.UTF8.GetBytes("live data");
        var path = WriteChunk("chunk_0.ts", data);
        var chunkPaths = new List<string> { path };

        using var stream = new RollingChunkStream(chunkPaths, new object(), _logger, seekToLiveEdge: false);

        // Drain the only (live-edge) chunk.
        var buf = new byte[data.Length];
        await stream.ReadExactlyAsync(buf, TestContext.Current.CancellationToken);

        // At EOF with no newer chunk in the list — must return 0.
        int read = await stream.ReadAsync(buf, TestContext.Current.CancellationToken);
        Assert.Equal(0, read);
    }

    [Fact]
    public async Task ReadAsync_AdvancesToSealedChunk_WhenCurrentExhausted()
    {
        var data0 = Encoding.UTF8.GetBytes("sealed chunk data");
        var data1 = Encoding.UTF8.GetBytes("live chunk data");
        var path0 = WriteChunk("chunk_0.ts", data0);
        var path1 = WriteChunk("chunk_1.ts", data1);

        var chunkLock = new object();
        // Start with only chunk_0 so the stream anchors there.
        var chunkPaths = new List<string> { path0 };

        using var stream = new RollingChunkStream(chunkPaths, chunkLock, _logger, seekToLiveEdge: false);

        // Drain chunk_0.
        var buf0 = new byte[data0.Length];
        int r0 = await stream.ReadAsync(buf0, TestContext.Current.CancellationToken);
        Assert.Equal(data0, buf0[..r0]);

        // Writer seals chunk_0 by adding chunk_1.
        lock (chunkLock)
        {
            chunkPaths.Add(path1);
        }

        // Next read: EOF on chunk_0 and chunk_1 now exists → advance.
        var buf1 = new byte[data1.Length];
        int r1 = await stream.ReadAsync(buf1, TestContext.Current.CancellationToken);
        Assert.Equal(data1, buf1[..r1]);
    }

    [Fact]
    public async Task ReadAsync_SkipsToOldestAvailableChunk_WhenCurrentChunkEvicted()
    {
        var dataEvicted = Encoding.UTF8.GetBytes("evicted");
        var data1 = Encoding.UTF8.GetBytes("oldest kept chunk");
        var data2 = Encoding.UTF8.GetBytes("live edge chunk");
        var path0 = WriteChunk("chunk_0.ts", dataEvicted);
        var path1 = WriteChunk("chunk_1.ts", data1);
        var path2 = WriteChunk("chunk_2.ts", data2);

        var chunkLock = new object();
        var chunkPaths = new List<string> { path0 };

        using var stream = new RollingChunkStream(chunkPaths, chunkLock, _logger, seekToLiveEdge: false);

        // Drain chunk_0 (file still on disk, but will be evicted from the list).
        var evictedBuf = new byte[dataEvicted.Length];
        int r0 = await stream.ReadAsync(evictedBuf, TestContext.Current.CancellationToken);
        Assert.Equal(dataEvicted.Length, r0);

        // Writer rotated past the keep window: add chunk_1 and chunk_2, evict chunk_0.
        lock (chunkLock)
        {
            chunkPaths.Add(path1);
            chunkPaths.Add(path2);
            chunkPaths.RemoveAt(0); // list is now [path1, path2]
        }

        // Next read: EOF on chunk_0, not in list → skip to oldest available (path1).
        var buf1 = new byte[data1.Length];
        int r1 = await stream.ReadAsync(buf1, TestContext.Current.CancellationToken);
        Assert.Equal(data1, buf1[..r1]);
    }

    [Fact]
    public async Task ReadAsync_SeeksNearEnd_WhenSeekToLiveEdgeRequested()
    {
        // Build a file larger than the 20 KB seek-back window so we can verify
        // the stream starts from near the end rather than the beginning.
        const int seekBackBytes = 20000;
        const int sentinelLength = 8;
        var prefix = new byte[seekBackBytes]; // zeros
        var sentinel = Encoding.UTF8.GetBytes("SENTINEL");
        var fileBytes = new byte[prefix.Length + sentinel.Length];
        prefix.CopyTo(fileBytes, 0);
        sentinel.CopyTo(fileBytes, prefix.Length);

        var path = WriteChunk("chunk_0.ts", fileBytes);
        var chunkPaths = new List<string> { path };

        using var stream = new RollingChunkStream(chunkPaths, new object(), _logger, seekToLiveEdge: true);

        // The stream should be positioned at end - 20000, so the sentinel is reachable.
        var buffer = new byte[fileBytes.Length];
        int totalRead = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(totalRead), TestContext.Current.CancellationToken)) > 0)
        {
            totalRead += read;
        }

        // The prefix bytes before the seek point should NOT be in the buffer;
        // only the sentinel (and possibly a few bytes of the prefix straddling the
        // seek point) should appear.
        Assert.Contains(sentinel, buffer[..totalRead]);
        Assert.True(totalRead <= seekBackBytes + sentinelLength);
    }
}
