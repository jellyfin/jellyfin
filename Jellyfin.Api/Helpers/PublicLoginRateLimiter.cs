using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Applies bounded, process-local progressive backoff to remote login attempts.
/// </summary>
internal sealed class PublicLoginRateLimiter
{
    private const int MaxTrackedKeys = 10_000;
    private const int CleanupInterval = 128;
    private const int CapacityEvictionBatch = 256;
    private const int MaxBackoffSeconds = 60;

    private readonly object _syncRoot = new();
    private readonly Dictionary<string, AttemptState> _attempts = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private int _operationCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicLoginRateLimiter"/> class.
    /// </summary>
    public PublicLoginRateLimiter()
        : this(TimeProvider.System)
    {
    }

    internal PublicLoginRateLimiter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    internal int TrackedKeyCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _attempts.Count;
            }
        }
    }

    /// <summary>
    /// Checks whether either the source address or account is currently backed off.
    /// </summary>
    /// <param name="remoteAddress">Normalized remote address.</param>
    /// <param name="username">Requested username.</param>
    /// <param name="includeAccount">Whether the account-wide key should be checked.</param>
    /// <param name="windowSeconds">Attempt retention window.</param>
    /// <param name="retryAfter">Remaining backoff duration.</param>
    /// <returns><see langword="true"/> when the attempt must be rejected.</returns>
    public bool IsBlocked(
        string remoteAddress,
        string username,
        bool includeAccount,
        int windowSeconds,
        out TimeSpan retryAfter)
    {
        var now = _timeProvider.GetUtcNow();
        var window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds));
        var ipKey = GetIpKey(remoteAddress);
        var accountKey = includeAccount ? GetAccountKey(username) : null;

        lock (_syncRoot)
        {
            CleanupExpiredEntries(now, window);

            var blockedUntil = GetBlockedUntil(ipKey);
            if (accountKey is not null)
            {
                var accountBlockedUntil = GetBlockedUntil(accountKey);
                if (accountBlockedUntil > blockedUntil)
                {
                    blockedUntil = accountBlockedUntil;
                }
            }

            retryAfter = blockedUntil > now ? blockedUntil - now : TimeSpan.Zero;
            return retryAfter > TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Records a failed authentication and applies progressive backoff.
    /// </summary>
    /// <param name="remoteAddress">Normalized remote address.</param>
    /// <param name="username">Requested username.</param>
    /// <param name="includeAccount">Whether an account-wide failure should be recorded.</param>
    /// <param name="maxFailedAttempts">Failures allowed before backoff starts.</param>
    /// <param name="windowSeconds">Attempt retention window.</param>
    public void RecordFailure(
        string remoteAddress,
        string username,
        bool includeAccount,
        int maxFailedAttempts,
        int windowSeconds)
    {
        var now = _timeProvider.GetUtcNow();
        var window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds));
        var threshold = Math.Max(1, maxFailedAttempts);
        var ipKey = GetIpKey(remoteAddress);
        var accountKey = includeAccount ? GetAccountKey(username) : null;

        lock (_syncRoot)
        {
            CleanupExpiredEntries(now, window);
            EnsureCapacity(accountKey is null ? 1 : 2);
            RecordFailure(ipKey, now, window, threshold);
            if (accountKey is not null)
            {
                RecordFailure(accountKey, now, window, threshold);
            }
        }
    }

    /// <summary>
    /// Clears account state after a successful authentication. Source state is
    /// retained so a valid public account cannot reset password-spraying limits.
    /// </summary>
    /// <param name="username">Authenticated username.</param>
    public void RecordSuccess(string username)
    {
        lock (_syncRoot)
        {
            _attempts.Remove(GetAccountKey(username));
        }
    }

    private static string GetIpKey(string remoteAddress)
        => "ip:" + remoteAddress;

    private static string GetAccountKey(string username)
    {
        var normalizedUsername = username.Trim().ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUsername));
        return "account:" + Convert.ToHexString(hash);
    }

    private DateTimeOffset GetBlockedUntil(string key)
        => _attempts.TryGetValue(key, out var state)
            ? state.BlockedUntil
            : DateTimeOffset.MinValue;

    private void RecordFailure(string key, DateTimeOffset now, TimeSpan window, int threshold)
    {
        var failures = 1;
        if (_attempts.TryGetValue(key, out var previous)
            && now - previous.LastFailure < window)
        {
            failures = previous.Failures + 1;
        }

        var backoff = TimeSpan.Zero;
        if (failures >= threshold)
        {
            var exponent = Math.Min(failures - threshold, 6);
            backoff = TimeSpan.FromSeconds(Math.Min(MaxBackoffSeconds, 1 << exponent));
        }

        _attempts[key] = new AttemptState(failures, now, now + backoff);
    }

    private void CleanupExpiredEntries(DateTimeOffset now, TimeSpan window)
    {
        _operationCount++;
        if (_operationCount % CleanupInterval != 0 && _attempts.Count < MaxTrackedKeys)
        {
            return;
        }

        var expiredKeys = new List<string>();
        foreach (var entry in _attempts)
        {
            if (now - entry.Value.LastFailure >= window
                && entry.Value.BlockedUntil <= now)
            {
                expiredKeys.Add(entry.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _attempts.Remove(key);
        }
    }

    private void EnsureCapacity(int requiredEntries)
    {
        var entriesToRemove = (_attempts.Count + requiredEntries) - MaxTrackedKeys;
        if (entriesToRemove <= 0)
        {
            return;
        }

        entriesToRemove = Math.Max(entriesToRemove, CapacityEvictionBatch);
        var keysToRemove = new List<string>(entriesToRemove);
        foreach (var key in _attempts.Keys)
        {
            keysToRemove.Add(key);
            if (keysToRemove.Count >= entriesToRemove)
            {
                break;
            }
        }

        foreach (var key in keysToRemove)
        {
            _attempts.Remove(key);
        }
    }

    private readonly record struct AttemptState(
        int Failures,
        DateTimeOffset LastFailure,
        DateTimeOffset BlockedUntil);
}
