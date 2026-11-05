using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Api.Helpers;

// Owns only a dedicated cache child. Never accepts a media path or a caller-selected output path.
internal sealed class OfflineDownloadJobs : IDisposable
{
    internal const long MaxFileBytes = 2L * 1024 * 1024 * 1024;

    internal const long EncoderFileLimit = MaxFileBytes - (16 * 1024 * 1024);

    internal static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private readonly object _sync = new();

    private readonly Dictionary<Guid, Entry> _entries = new();

    private readonly SemaphoreSlim _encoder = new(1, 1);

    private readonly string _root;

    private readonly Func<DateTimeOffset> _clock;

    private readonly Func<long> _freeBytes;

    private readonly Timer _timer;

    private readonly Action<string>? _warning;

    private bool _disposed;

    internal OfflineDownloadJobs(string transcodeRoot, Func<DateTimeOffset>? clock = null, Func<long>? freeBytes = null, Action<string>? warning = null)
    {
        _warning = warning;
        _root = Path.Combine(Path.GetFullPath(transcodeRoot), "offline-downloads-v2");
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _freeBytes = freeBytes ?? (() => DriveInfo.GetDrives().Where(drive => _root.StartsWith(drive.Name, StringComparison.Ordinal)).OrderByDescending(drive => drive.Name.Length).First().AvailableFreeSpace);
        Directory.CreateDirectory(_root);
        RejectLink(_root);
        PrivateDirectory(_root);
        Recover();
        _timer = new Timer(_ => SweepSafely(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    internal static bool CanAppend(long bytes, int count) => bytes >= 0 && count > 0 && bytes <= EncoderFileLimit - (1024 * 1024) - count;

    internal OfflineDownloadJobInfo Create(Guid owner, Guid item, string fingerprint, string filename, string quality, int? audio, int? subtitle, string mode)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Sweep();
            foreach (var failed in _entries.Values.Where(e => e.Data.Owner.Equals(owner) && e.Data.Status == "Failed").ToArray())
            {
                DeleteEntry(failed);
            }

            var existing = _entries.Values.FirstOrDefault(e => !e.Deleted && e.Data.Owner.Equals(owner) && e.Data.Item.Equals(item) && e.Data.Fingerprint == fingerprint && e.Data.Quality == quality && e.Data.AudioStreamIndex == audio && e.Data.SubtitleStreamIndex == subtitle && e.Data.SubtitleMode == mode && e.Data.Status != "Failed");
            if (existing is not null)
            {
                return Info(existing);
            }

            // Reserve a complete two-GiB slot, including queued jobs and deletion deferred by readers.
            if (_entries.Count >= 2)
            {
                throw new InvalidOperationException("The temporary download cache is full. Finish or discard another download first.");
            }

            var data = new OfflineDownloadRecord
            {
                Id = Guid.NewGuid(), Owner = owner, Item = item, Fingerprint = fingerprint,
                Filename = filename, Quality = quality, AudioStreamIndex = audio, SubtitleStreamIndex = subtitle,
                SubtitleMode = mode, ExpiresAt = _clock().Add(Retention), Status = "Queued"
            };
            var entry = new Entry(data);
            Directory.CreateDirectory(DirectoryFor(data.Id));
            PrivateDirectory(DirectoryFor(data.Id));
            Save(data);
            _entries.Add(data.Id, entry);
            return Info(entry);
        }
    }

    internal OfflineDownloadJobInfo[] List(Guid owner, Guid item)
    {
        lock (_sync)
        {
            Sweep();
            return _entries.Values.Where(e => !e.Deleted && e.Data.Owner.Equals(owner) && e.Data.Item.Equals(item)).Select(Info).ToArray();
        }
    }

    internal OfflineDownloadJobInfo? Get(Guid owner, Guid item, Guid id, string? fingerprint = null)
    {
        lock (_sync)
        {
            Sweep();
            var entry = Find(owner, item, id);
            if (entry is null)
            {
                return null;
            }

            if (fingerprint is not null && fingerprint != entry.Data.Fingerprint)
            {
                DeleteEntry(entry);
                return null;
            }

            return Info(entry);
        }
    }

    internal bool Delete(Guid owner, Guid item, Guid id)
    {
        lock (_sync)
        {
            var entry = Find(owner, item, id);
            if (entry is null)
            {
                return false;
            }

            DeleteEntry(entry);
            return true;
        }
    }

    // Called by an awaited HTTP handler. A lost connection does not dispose its scope prematurely.
    internal async Task Prepare(Guid owner, Guid item, Guid id, Func<CancellationToken, Task<Stream>> open)
    {
        Entry entry;
        lock (_sync)
        {
            entry = Find(owner, item, id) ?? throw new FileNotFoundException();
            if (entry.Data.Status is "Ready" or "Preparing")
            {
                return;
            }

            if (entry.Data.Status != "Queued")
            {
                throw new InvalidOperationException("Discard the failed download before retrying.");
            }

            entry.Data.Status = "Preparing";
            entry.Working = true;
            entry.Cancellation.CancelAfter(TimeSpan.FromHours(2));
        }

        var acquired = false;
        try
        {
            await _encoder.WaitAsync(entry.Cancellation.Token).ConfigureAwait(false);
            acquired = true;
            entry.Cancellation.Token.ThrowIfCancellationRequested();
            if (_freeBytes() < 5L * 1024 * 1024 * 1024)
            {
                throw new IOException("Insufficient temporary disk space.");
            }

            await using (var input = await open(entry.Cancellation.Token).ConfigureAwait(false))
            {
                entry.Cancellation.Token.ThrowIfCancellationRequested();
                await using var output = new FileStream(PartialFor(id), FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous);
                PrivateFile(PartialFor(id));
                var buffer = new byte[65536];
                while (true)
                {
                    var count = await input.ReadAsync(buffer, entry.Cancellation.Token).ConfigureAwait(false);
                    if (count == 0)
                    {
                        break;
                    }

                    if (!CanAppend(entry.Bytes, count))
                    {
                        throw new IOException("Offline output reached its safety size limit; choose a smaller preset.");
                    }

                    await output.WriteAsync(buffer.AsMemory(0, count), entry.Cancellation.Token).ConfigureAwait(false);
                    entry.AddBytes(count);
                }

                await output.FlushAsync(entry.Cancellation.Token).ConfigureAwait(false);
                await Task.Run(() => output.Flush(true), entry.Cancellation.Token).ConfigureAwait(false);
            }

            if (entry.Bytes == 0)
            {
                throw new IOException("Empty offline output.");
            }

            string hash;
            await using (var file = File.OpenRead(PartialFor(id)))
            {
                hash = Convert.ToHexString(await SHA256.HashDataAsync(file, entry.Cancellation.Token).ConfigureAwait(false));
            }

            lock (_sync)
            {
                entry.Cancellation.Token.ThrowIfCancellationRequested();
                if (entry.Deleted || entry.Data.ExpiresAt <= _clock())
                {
                    throw new OperationCanceledException();
                }

                File.Move(PartialFor(id), FileFor(id));
                entry.Data.Size = entry.Bytes;
                entry.Data.ETag = "\"" + hash + "\"";
                entry.Data.Status = "Ready";
                try
                {
                    Save(entry.Data);
                }
                catch
                {
                    entry.Data.Status = "Failed";
                    throw;
                }
            }
        }
        catch (Exception error)
        {
            lock (_sync)
            {
                entry.Data.Status = "Failed";
                entry.Data.Error = error is OperationCanceledException ? "Preparation was cancelled or expired." : "Preparation failed. Check source tracks, disk space and size limits; discard this job before retrying.";
                File.Delete(PartialFor(id));
                File.Delete(FileFor(id));
            }
        }
        finally
        {
            if (acquired)
            {
                _encoder.Release();
            }

            lock (_sync)
            {
                entry.Cancellation.CancelAfter(Timeout.InfiniteTimeSpan);
                entry.Working = false;
                if (entry.Deleted)
                {
                    Remove(entry);
                }
            }
        }
    }

    internal Stream Open(Guid owner, Guid item, Guid id, Action? abortTransfer = null)
    {
        lock (_sync)
        {
            Sweep();
            var entry = Find(owner, item, id) ?? throw new FileNotFoundException();
            if (entry.Data.Status != "Ready" || !ValidFile(entry.Data))
            {
                throw new InvalidOperationException("The offline file is not ready.");
            }

            RejectLink(DirectoryFor(id));
            RejectLink(FileFor(id));
            var file = new FileStream(FileFor(id), FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.RandomAccess);
            entry.Readers++;
            var registration = abortTransfer is null ? default : entry.Cancellation.Token.Register(abortTransfer);
            return new OfflineDownloadStream(file, async () =>
            {
                await registration.DisposeAsync().ConfigureAwait(false);
                lock (_sync)
                {
                    entry.Readers--;
                    if (entry.Deleted && !entry.Working && entry.Readers == 0)
                    {
                        Remove(entry);
                    }
                }
            });
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer.Dispose();
            foreach (var entry in _entries.Values)
            {
                entry.Cancellation.Cancel();
            }
        }
    }

    private Entry? Find(Guid owner, Guid item, Guid id)
        => _entries.TryGetValue(id, out var entry) && !entry.Deleted && entry.Data.Owner.Equals(owner) && entry.Data.Item.Equals(item) ? entry : null;

    private static OfflineDownloadJobInfo Info(Entry e)
        => new(e.Data.Id, e.Data.Status, e.Data.Size, e.Data.Filename, e.Data.ETag, e.Data.ExpiresAt, e.Data.Quality, e.Data.AudioStreamIndex, e.Data.SubtitleStreamIndex, e.Data.SubtitleMode, e.Bytes, e.Data.Error);

    private void SweepSafely()
    {
        try
        {
            lock (_sync)
            {
                if (!_disposed)
                {
                    Sweep();
                }
            }
        }
        catch (IOException)
        {
            // Retry next tick. A failed unlink continues to occupy its reserved slot.
        }
        catch (UnauthorizedAccessException)
        {
            // Do not widen permissions to remove an inaccessible file.
        }
    }

    private void Sweep()
    {
        foreach (var entry in _entries.Values.ToArray())
        {
            if (entry.Data.ExpiresAt <= _clock() || (entry.Data.Status == "Ready" && !ValidFile(entry.Data)))
            {
                DeleteEntry(entry);
            }
        }
    }

    private void DeleteEntry(Entry entry)
    {
        entry.Deleted = true;
        entry.Cancellation.Cancel();
        if (!entry.Working && entry.Readers == 0)
        {
            Remove(entry);
        }
    }

    private void Remove(Entry entry)
    {
        var path = DirectoryFor(entry.Data.Id);
        if (Directory.Exists(path))
        {
            RejectLink(path);
            Directory.Delete(path, true);
        }

        _entries.Remove(entry.Data.Id);
        entry.Cancellation.Dispose();
    }

    private void Save(OfflineDownloadRecord data)
    {
        var path = Path.Combine(DirectoryFor(data.Id), "job.json");
        var temp = path + ".tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            PrivateFile(temp);
            JsonSerializer.Serialize(file, data);
            file.Flush(true);
        }

        File.Move(temp, path, true);
    }

    private void Recover()
    {
        foreach (var directory in Directory.EnumerateDirectories(_root).OrderBy(p => p, StringComparer.Ordinal))
        {
            if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out var id) || new DirectoryInfo(directory).LinkTarget is not null)
            {
                continue;
            }

            try
            {
                var manifest = Path.Combine(directory, "job.json");
                RejectLink(manifest);
                using var input = File.OpenRead(manifest);
                var data = JsonSerializer.Deserialize<OfflineDownloadRecord>(input);
                if (data is null || !data.Id.Equals(id) || data.Owner.Equals(Guid.Empty) || data.Item.Equals(Guid.Empty) || data.Status != "Ready" || data.ExpiresAt <= _clock() || data.ExpiresAt > _clock().Add(Retention) || data.Size <= 0 || data.Size >= EncoderFileLimit || _entries.Count >= 2 || OfflineDownloadProfiles.Get(data.Quality) is null || !ValidFile(data))
                {
                    throw new InvalidDataException("metadata: null=" + (data is null) + ", id=" + data?.Id.Equals(id) + ", owner=" + data?.Owner.Equals(Guid.Empty) + ", item=" + data?.Item.Equals(Guid.Empty) + ", status=" + data?.Status + ", expired=" + (data?.ExpiresAt <= _clock()) + ", future=" + (data?.ExpiresAt > _clock().Add(Retention)) + ", size=" + data?.Size + ", slots=" + _entries.Count + ", file=" + (data is not null && ValidFile(data)));
                }

                using var file = File.OpenRead(FileFor(id));
                var hash = "\"" + Convert.ToHexString(SHA256.HashData(file)) + "\"";
                if (data.ETag != hash)
                {
                    throw new InvalidDataException("checksum");
                }

                _entries.Add(id, new Entry(data));
            }
            catch (Exception error) when (error is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
            {
                _warning?.Invoke(error.ToString());
                // No partially prepared file is advertised as resumable after a server crash.
                Directory.Delete(directory, true);
            }
        }
    }

    private bool ValidFile(OfflineDownloadRecord data)
    {
        var path = FileFor(data.Id);
        return File.Exists(path) && new FileInfo(path).LinkTarget is null && new FileInfo(path).Length == data.Size;
    }

    private string DirectoryFor(Guid id) => Path.Combine(_root, id.ToString("N"));

    private string FileFor(Guid id) => Path.Combine(DirectoryFor(id), "download.mp4");

    private string PartialFor(Guid id) => Path.Combine(DirectoryFor(id), "download.part");

    private static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new IOException("Offline cache links are not allowed.");
        }
    }

    private static void PrivateDirectory(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static void PrivateFile(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private sealed class Entry(OfflineDownloadRecord data)
    {
        private long _bytes = data.Size;

        internal OfflineDownloadRecord Data { get; } = data;

        internal CancellationTokenSource Cancellation { get; } = new();

        internal long Bytes => Interlocked.Read(ref _bytes);

        internal int Readers { get; set; }

        internal bool Working { get; set; }

        internal bool Deleted { get; set; }

        internal void AddBytes(int bytes) => Interlocked.Add(ref _bytes, bytes);
    }
}
