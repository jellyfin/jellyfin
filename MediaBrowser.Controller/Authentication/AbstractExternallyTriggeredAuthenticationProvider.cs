using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.EntityFrameworkCore;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Helper class for an externally triggered authentication provider (i.e., one that does not take any meaningful payload data for its actual authenticate request).
    /// Automatically handles time-based expiry, event handlers, disposal and reuse attack prevention (i.e., invalidates data after 1 authentication has been performed).
    /// </summary>
    /// <typeparam name="TGlobalData">Global data that your authentication provider wants to store.</typeparam>
    /// <typeparam name="TUserData">User-specific data that your authentication provider wants to store.</typeparam>
    /// <typeparam name="TAttemptData">Attempt-specific data that your authentication provider wants to store.</typeparam>
    public abstract class AbstractExternallyTriggeredAuthenticationProvider<TGlobalData, TUserData, TAttemptData>
        : AbstractAuthenticationProvider<ExternallyTriggeredAuthenticationData, TGlobalData, TUserData>,
        IKeyedMonitorable<TAttemptData>,
        IDisposable
        where TUserData : struct
        where TGlobalData : struct
        where TAttemptData : class
    {
        private readonly ConcurrentDictionary<string, MonitorEntry<TAttemptData>> _monitorMap = new();
        private readonly ConcurrentDictionary<string, string> _updateToMonitorKeyMap = new();
        private Timer _cleanupTask;
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractExternallyTriggeredAuthenticationProvider{TGlobalData, TUserData, TAttemptData}"/> class.
        /// </summary>
        /// <param name="contextFactory">Pass to base class.</param>
        /// <param name="userManager">Pass to base class too.</param>
        protected AbstractExternallyTriggeredAuthenticationProvider(
        IDbContextFactory<JellyfinDbContext> contextFactory,
        IUserManager userManager) : base(contextFactory, userManager)
        {
            _cleanupTask = new Timer(
                (_) =>
                {
                    foreach (var (monitorKey, entry) in _monitorMap)
                    {
                        if ((DateTimeOffset.UtcNow - entry.Start).TotalSeconds > AttemptValidity)
                        {
                            Remove(monitorKey);
                        }
                    }
                },
                null,
                AttemptValidity,
                AttemptValidity);
        }

        /// <inheritdoc/>
        public abstract override string? AuthenticationType { get; }

        /// <summary>
        /// Gets the number of seconds after initiation an attempt is considered valid. Defaults to 600 seconds (10 minutes).
        /// </summary>
        protected virtual int AttemptValidity { get => 600; }

        /// <inheritdoc/>
        public override async Task<AuthenticationResult> Authenticate(ExternallyTriggeredAuthenticationData authenticationData)
        {
            var data = await GetData(authenticationData.MonitorKey).ConfigureAwait(false);
            if (data is null)
            {
                return AuthenticationResult.AnonymousFailure();
            }

            var authenticationResult = await AuthenticateAttempt(data).ConfigureAwait(false);

            // TODO: does not invalidate upon unsuccessful authentication, should it? or maybe configurable?
            if (authenticationResult.Authenticated)
            {
                Remove(authenticationData.MonitorKey);
            }

            return authenticationResult;
        }

        private void Remove(string monitorKey)
        {
            _monitorMap.Remove(monitorKey, out var entry);
            if (entry is not null)
            {
                _updateToMonitorKeyMap.TryRemove(entry.UpdateKey, out _);
                entry.UpdateEvent.Dispose();
            }
        }

        /// <summary>
        /// Authenticates a given attempt.
        /// </summary>
        /// <param name="attemptData">The attempt to authenticate.</param>
        /// <returns>A User if authentication was successful, or null if not.</returns>
        protected abstract Task<AuthenticationResult> AuthenticateAttempt(TAttemptData attemptData);

        /// <inheritdoc/>
        public async Task<MonitorData> Initiate(TAttemptData data)
        {
            var monitorKey = await GenerateMonitorKey(data).ConfigureAwait(false);
            var updateKey = await GenerateUpdateKey(data).ConfigureAwait(false);

            _updateToMonitorKeyMap[updateKey] = monitorKey;
            _monitorMap[monitorKey] = new MonitorEntry<TAttemptData>(data, updateKey, new(0, 5), DateTimeOffset.Now);

            return new MonitorData(monitorKey, updateKey);
        }

        /// <summary>
        /// Generates a new monitor key used to monitor progress for this authentication provider.
        /// </summary>
        /// <param name="data">The attempt data for which to generate the monitor key.</param>
        /// <returns>The new monitor key.</returns>
        public virtual Task<string> GenerateMonitorKey(TAttemptData data)
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);

            return Task.FromResult(Convert.ToHexString(bytes));
        }

        /// <summary>
        /// Generates a new update key used to update progress for this authentication provider.
        /// </summary>
        /// <param name="data">The attempt data for which to generate the update key.</param>
        /// <returns>The new update key.</returns>
        public abstract Task<string> GenerateUpdateKey(TAttemptData data);

        /// <inheritdoc/>
        public async Task<TAttemptData?> GetData(string monitorKey, bool waitForUpdate = false, int millisecondsTimeout = 60000)
        {
            var entry = _monitorMap[monitorKey];

            if (entry is null)
            {
                return null;
            }

            if ((DateTimeOffset.UtcNow - entry.Start).TotalSeconds > AttemptValidity)
            {
                Remove(monitorKey);
                return null;
            }

            if (!waitForUpdate)
            {
                return entry.Data;
            }

            var didUpdate = await entry.UpdateEvent.WaitAsync(millisecondsTimeout).ConfigureAwait(false);

            if ((DateTimeOffset.UtcNow - entry.Start).TotalSeconds > AttemptValidity)
            {
                Remove(monitorKey);
                return null;
            }

            return entry.Data;
        }

        /// <inheritdoc/>
        public Task<bool> Update(string updateKey, Action<TAttemptData> updater)
        {
            ArgumentNullException.ThrowIfNull(updater);

            var exists = _monitorMap.TryGetValue(_updateToMonitorKeyMap[updateKey], out var entry);
            if (!exists || entry is null)
            {
                return Task.FromResult(false);
            }

            updater(entry.Data);
            entry.UpdateEvent.Release(5);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Disposal logic, disposes all semaphores.
        /// </summary>
        /// <param name="disposing">Whether or not we want to dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var monitor in _monitorMap.Values)
                    {
                        monitor.UpdateEvent.Dispose();
                    }

                    _cleanupTask?.Dispose();
                }

                _disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private record MonitorEntry<TData>(TData Data, string UpdateKey, SemaphoreSlim UpdateEvent, DateTimeOffset Start);
    }

    /// <summary>
    /// Payload data used for an externally triggered authentication request.
    /// </summary>
#pragma warning disable SA1402, SA1201 // File may only contain a single type, Elements should appear in the correct order
    public record struct ExternallyTriggeredAuthenticationData(string MonitorKey);
#pragma warning restore SA1402, SA1201 // File may only contain a single type, Elements should appear in the correct order
}
