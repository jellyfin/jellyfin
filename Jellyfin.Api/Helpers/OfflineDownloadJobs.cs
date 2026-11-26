using System;
using System.Collections.Generic;
using System.Globalization;
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

    internal const string StatusQueued = "Queued";

    internal const string StatusPreparing = "Preparing";

    internal const string StatusReady = "Ready";

    internal const string StatusFailed = "Failed";

    internal const int MaxSlotsPerUser = 2;

    internal const int MaxTotalSlots = 4;

    internal static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    internal static readonly TimeSpan DefaultSweepInterval = TimeSpan.FromSeconds(30);

    private readonly object _sync = new();

    private readonly Dictionary<Guid, Entry> _entries = new();

    private readonly SemaphoreSlim _encoder = new(1, 1);

    private readonly string _root;

    private readonly Func<DateTimeOffset> _clock;

    private readonly Func<long> _freeBytes;

    private readonly Timer _timer;

    private readonly Action<string>? _warning;

    private bool _disposed;

    internal OfflineDownloadJobs(string transcodeRoot, Func<DateTimeOffset>? clock = null, Func<long>? freeBytes = null, Action<string>? warning = null, TimeSpan? sweepInterval = null)
    {
        _warning = warning;
        _root = Path.Combine(Path.GetFullPath(transcodeRoot), "offline-downloads-v2");
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _freeBytes = freeBytes ?? (() => GetAvailableFreeSpace(_root));
        Directory.CreateDirectory(_root);
        RejectLink(_root);
        PrivateDirectory(_root);
        Recover();
        var interval = sweepInterval ?? DefaultSweepInterval;
        _timer = new Timer(_ => SweepSafely(), null, interval, interval);
    }

    internal static long GetAvailableFreeSpace(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath);
        if (!string.IsNullOrEmpty(root) && !root.StartsWith(@"\\", StringComparison.Ordinal))
        {
            try
            {
                return new DriveInfo(root).AvailableFreeSpace;
            }
            catch (Exception error) when (error is ArgumentException or DriveNotFoundException or IOException)
            {
                // Fall back to longest mount-point prefix match below (e.g. Linux nested mounts).
            }
        }

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        DriveInfo? best = null;
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!fullPath.StartsWith(drive.Name, comparison))
            {
                continue;
            }

            if (best is null || drive.Name.Length > best.Name.Length)
            {
                best = drive;
            }
        }

        if (best is null)
        {
            throw new IOException("Unable to determine available temporary disk space.");
        }

        return best.AvailableFreeSpace;
    }

    internal static bool CanAppend(long bytes, int count) => bytes >= 0 && count > 0 && bytes <= EncoderFileLimit - (1024 * 1024) - count;

    internal OfflineDownloadJobInfo Create(Guid owner, Guid item, string fingerprint, string filename, string quality, (int? Audio, int? Subtitle, string Mode) tracks)
    {
        List<Entry>? toCancel = null;
        OfflineDownloadJobInfo created;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Sweep(ref toCancel);

            // Reclaim any failed jobs belonging to this user first.
            foreach (var failed in _entries.Values.Where(e => e.Data.Owner.Equals(owner) && e.Data.Status == StatusFailed).ToArray())
            {
                DeleteEntry(failed, ref toCancel);
            }

            var existing = _entries.Values.FirstOrDefault(e =>
                !e.Deleted
                && e.Data.Owner.Equals(owner)
                && e.Data.Item.Equals(item)
                && e.Data.Fingerprint == fingerprint
                && e.Data.Quality == quality
                && e.Data.AudioStreamIndex == tracks.Audio
                && e.Data.SubtitleStreamIndex == tracks.Subtitle
                && e.Data.SubtitleMode == tracks.Mode
                && e.Data.Status != StatusFailed);
            if (existing is not null)
            {
                created = Info(existing);
            }
            else
            {
                // If the global cache is full, reclaim failed jobs from any user so failed entries cannot starve others.
                if (_entries.Count >= MaxTotalSlots)
                {
                    foreach (var failed in _entries.Values.Where(e => e.Data.Status == StatusFailed).ToArray())
                    {
                        DeleteEntry(failed, ref toCancel);
                    }
                }

                var userCount = _entries.Values.Count(e => e.Data.Owner.Equals(owner));
                if (userCount >= MaxSlotsPerUser || _entries.Count >= MaxTotalSlots)
                {
                    throw new InvalidOperationException("The temporary download cache is full. Finish or discard another download first.");
                }

                var data = new OfflineDownloadRecord
                {
                    Id = Guid.NewGuid(),
                    Owner = owner,
                    Item = item,
                    Fingerprint = fingerprint,
                    Filename = filename,
                    Quality = quality,
                    AudioStreamIndex = tracks.Audio,
                    SubtitleStreamIndex = tracks.Subtitle,
                    SubtitleMode = tracks.Mode,
                    ExpiresAt = _clock().Add(Retention),
                    Status = StatusQueued
                };
                var entry = new Entry(data);
                Directory.CreateDirectory(DirectoryFor(data.Id));
                PrivateDirectory(DirectoryFor(data.Id));
                Save(data);
                _entries.Add(data.Id, entry);
                created = Info(entry);
            }
        }

        CancelDeferred(toCancel);
        return created;
    }

    internal OfflineDownloadJobInfo[] List(Guid owner, Guid item)
    {
        List<Entry>? toCancel = null;
        OfflineDownloadJobInfo[] result;
        lock (_sync)
        {
            Sweep(ref toCancel);
            result = _entries.Values.Where(e => !e.Deleted && e.Data.Owner.Equals(owner) && e.Data.Item.Equals(item)).Select(Info).ToArray();
        }

        CancelDeferred(toCancel);
        return result;
    }

    internal OfflineDownloadJobInfo? Get(Guid owner, Guid item, Guid id, string? fingerprint = null)
    {
        List<Entry>? toCancel = null;
        OfflineDownloadJobInfo? result = null;
        lock (_sync)
        {
            Sweep(ref toCancel);
            var entry = Find(owner, item, id);
            if (entry is not null)
            {
                if (fingerprint is not null && fingerprint != entry.Data.Fingerprint)
                {
                    DeleteEntry(entry, ref toCancel);
                }
                else
                {
                    result = Info(entry);
                }
            }
        }

        CancelDeferred(toCancel);
        return result;
    }

    internal bool Delete(Guid owner, Guid item, Guid id)
    {
        List<Entry>? toCancel = null;
        bool deleted;
        lock (_sync)
        {
            var entry = Find(owner, item, id);
            if (entry is null)
            {
                deleted = false;
            }
            else
            {
                DeleteEntry(entry, ref toCancel);
                deleted = true;
            }
        }

        CancelDeferred(toCancel);
        return deleted;
    }

    // Called by an awaited HTTP handler. A lost connection does not dispose its scope prematurely.
    internal async Task Prepare(Guid owner, Guid item, Guid id, Func<CancellationToken, Task<Stream>> open)
    {
        var entry = TryBeginPrepare(owner, item, id);
        if (entry is null)
        {
            return;
        }

        var acquired = false;
        try
        {
            await _encoder.WaitAsync(entry.Cancellation.Token).ConfigureAwait(false);
            acquired = true;
            var hash = await WriteAndHashPartialAsync(id, entry, open).ConfigureAwait(false);
            CompletePreparation(id, entry, hash);
        }
        catch (Exception error)
        {
            FailPreparation(id, entry, error);
        }
        finally
        {
            FinishPreparation(entry, acquired);
        }
    }

    internal Stream Open(Guid owner, Guid item, Guid id, Action? abortTransfer = null)
    {
        List<Entry>? toCancel = null;
        try
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                Sweep(ref toCancel);
                var entry = Find(owner, item, id);
                if (entry is null)
                {
                    throw new FileNotFoundException();
                }

                if (entry.Data.Status != StatusReady || !ValidFile(entry.Data))
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
                        else if (_disposed && !entry.Working && entry.Readers == 0)
                        {
                            entry.Dispose();
                        }
                    }
                });
            }
        }
        finally
        {
            CancelDeferred(toCancel);
        }
    }

    public void Dispose()
    {
        Entry[] entries;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer.Dispose();
            entries = _entries.Values.ToArray();
        }

        foreach (var entry in entries)
        {
            entry.CancelSafe();
        }

        lock (_sync)
        {
            foreach (var entry in entries)
            {
                if (!entry.Working && entry.Readers == 0)
                {
                    entry.Dispose();
                }
            }
        }

        _encoder.Dispose();
    }

    private Entry? TryBeginPrepare(Guid owner, Guid item, Guid id)
    {
        lock (_sync)
        {
            var entry = Find(owner, item, id) ?? throw new FileNotFoundException();
            if (entry.Data.Status is StatusReady or StatusPreparing)
            {
                return null;
            }

            if (entry.Data.Status != StatusQueued)
            {
                throw new InvalidOperationException("Discard the failed download before retrying.");
            }

            entry.Data.Status = StatusPreparing;
            entry.Working = true;
            entry.Cancellation.CancelAfter(TimeSpan.FromHours(2));
            return entry;
        }
    }

    private async Task<string> WriteAndHashPartialAsync(Guid id, Entry entry, Func<CancellationToken, Task<Stream>> open)
    {
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

        await using var file = File.OpenRead(PartialFor(id));
        return Convert.ToHexString(await SHA256.HashDataAsync(file, entry.Cancellation.Token).ConfigureAwait(false));
    }

    private void CompletePreparation(Guid id, Entry entry, string hash)
    {
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
            entry.Data.Status = StatusReady;
            try
            {
                Save(entry.Data);
            }
            catch
            {
                entry.Data.Status = StatusFailed;
                throw;
            }
        }
    }

    private void FailPreparation(Guid id, Entry entry, Exception error)
    {
        lock (_sync)
        {
            entry.Data.Status = StatusFailed;
            entry.Data.Error = error is OperationCanceledException
                ? "Preparation was cancelled or expired."
                : "Preparation failed. Check source tracks, disk space and size limits; discard this job before retrying.";
            TryDeleteFile(PartialFor(id));
            TryDeleteFile(FileFor(id));
            if (!entry.Deleted && Directory.Exists(DirectoryFor(id)))
            {
                try
                {
                    Save(entry.Data);
                }
                catch (Exception saveError) when (saveError is IOException or UnauthorizedAccessException)
                {
                    _warning?.Invoke(saveError.ToString());
                }
            }
        }
    }

    private void FinishPreparation(Entry entry, bool acquired)
    {
        if (acquired)
        {
            try
            {
                _encoder.Release();
            }
            catch (ObjectDisposedException)
            {
                // Store was disposed while preparation was winding down.
            }
        }

        lock (_sync)
        {
            entry.ClearCancelAfterSafe();
            entry.Working = false;
            if (entry.Deleted && entry.Readers == 0)
            {
                Remove(entry);
            }
            else if (_disposed && entry.Readers == 0)
            {
                entry.Dispose();
            }
        }
    }

    private Entry? Find(Guid owner, Guid item, Guid id)
        => _entries.TryGetValue(id, out var entry) && !entry.Deleted && entry.Data.Owner.Equals(owner) && entry.Data.Item.Equals(item) ? entry : null;

    private static OfflineDownloadJobInfo Info(Entry e)
        => new(e.Data.Id, e.Data.Status, e.Data.Size, e.Data.Filename, e.Data.ETag, e.Data.ExpiresAt, e.Data.Quality, e.Data.AudioStreamIndex, e.Data.SubtitleStreamIndex, e.Data.SubtitleMode, e.Bytes, e.Data.Error);

    private void SweepSafely()
    {
        List<Entry>? toCancel = null;
        try
        {
            lock (_sync)
            {
                if (!_disposed)
                {
                    Sweep(ref toCancel);
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

        CancelDeferred(toCancel);
    }

    private void Sweep(ref List<Entry>? toCancel)
    {
        foreach (var entry in _entries.Values.ToArray())
        {
            if (entry.Data.ExpiresAt <= _clock() || (entry.Data.Status == StatusReady && !ValidFile(entry.Data)))
            {
                DeleteEntry(entry, ref toCancel);
            }
        }
    }

    private void DeleteEntry(Entry entry, ref List<Entry>? toCancel)
    {
        entry.Deleted = true;
        toCancel ??= new List<Entry>();
        toCancel.Add(entry);
        if (!entry.Working && entry.Readers == 0)
        {
            Remove(entry, disposeCancellation: false);
        }
    }

    private void CancelDeferred(List<Entry>? entries)
    {
        if (entries is null)
        {
            return;
        }

        foreach (var entry in entries)
        {
            entry.CancelSafe();
        }

        lock (_sync)
        {
            foreach (var entry in entries)
            {
                if (entry.Deleted && !entry.Working && entry.Readers == 0)
                {
                    entry.Dispose();
                }
            }
        }
    }

    private void Remove(Entry entry, bool disposeCancellation = true)
    {
        var path = DirectoryFor(entry.Data.Id);
        if (Directory.Exists(path))
        {
            RejectLink(path);
            Directory.Delete(path, true);
        }

        _entries.Remove(entry.Data.Id);
        if (disposeCancellation)
        {
            entry.Dispose();
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception error) when (error is DirectoryNotFoundException or IOException or UnauthorizedAccessException)
        {
            // Directory may already be removed or file locked; slot cleanup will retry.
        }
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
                if (data is null || !data.Id.Equals(id) || data.Owner.Equals(Guid.Empty) || data.Item.Equals(Guid.Empty) || data.Status != StatusReady || data.ExpiresAt <= _clock() || data.ExpiresAt > _clock().Add(Retention) || data.Size <= 0 || data.Size >= EncoderFileLimit || _entries.Count >= MaxTotalSlots || OfflineDownloadProfiles.Get(data.Quality) is null || !ValidFile(data))
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

    private string DirectoryFor(Guid id) => Path.Combine(_root, id.ToString("N", CultureInfo.InvariantCulture));

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

    private sealed class Entry(OfflineDownloadRecord data) : IDisposable
    {
        private long _bytes = data.Size;
        private int _disposed;

        internal OfflineDownloadRecord Data { get; } = data;

        internal CancellationTokenSource Cancellation { get; } = new();

        internal long Bytes => Interlocked.Read(ref _bytes);

        internal int Readers { get; set; }

        internal bool Working { get; set; }

        internal bool Deleted { get; set; }

        internal void AddBytes(int bytes) => Interlocked.Add(ref _bytes, bytes);

        internal void CancelSafe()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            try
            {
                Cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed concurrently.
            }
        }

        internal void ClearCancelAfterSafe()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            try
            {
                Cancellation.CancelAfter(Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // Already disposed concurrently.
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Cancellation.Dispose();
            }
        }
    }
}
