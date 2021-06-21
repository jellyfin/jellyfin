#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.QuickConnect;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.QuickConnect
{
    /// <summary>
    /// Quick connect implementation.
    /// </summary>
    public class QuickConnectManager : IQuickConnect, IDisposable
    {
        private readonly RNGCryptoServiceProvider _rng = new ();
        private readonly ConcurrentDictionary<string, QuickConnectResult> _currentRequests = new ();
        private readonly ConcurrentDictionary<string, (string Token, Guid UserId)> _quickConnectTokens = new ();

        private readonly IServerConfigurationManager _config;
        private readonly ILogger<QuickConnectManager> _logger;
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickConnectManager"/> class.
        /// Should only be called at server startup when a singleton is created.
        /// </summary>
        /// <param name="config">The server configuration manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">The session manager.</param>
        public QuickConnectManager(IServerConfigurationManager config, ILogger<QuickConnectManager> logger, ISessionManager sessionManager)
        {
            _config = config;
            _logger = logger;
            _sessionManager = sessionManager;

            ReloadConfiguration();
        }

        /// <inheritdoc/>
        public int CodeLength { get; set; } = 6;

        /// <inheritdoc/>
        public string TokenName { get; set; } = "QuickConnect";

        /// <inheritdoc/>
        public QuickConnectState State { get; private set; } = QuickConnectState.Unavailable;

        /// <inheritdoc/>
        public int Timeout { get; set; } = 5;

        private DateTime DateActivated { get; set; }

        /// <inheritdoc/>
        public void AssertActive()
        {
            if (State != QuickConnectState.Active)
            {
                throw new ArgumentException("Quick connect is not active on this server");
            }
        }

        /// <inheritdoc/>
        public void Activate()
        {
            DateActivated = DateTime.UtcNow;
            SetState(QuickConnectState.Active);
        }

        /// <inheritdoc/>
        public void SetState(QuickConnectState newState)
        {
            _logger.LogDebug("Changed quick connect state from {State} to {newState}", State, newState);

            ExpireRequests(true);

            State = newState;
            _config.Configuration.QuickConnectAvailable = newState == QuickConnectState.Available || newState == QuickConnectState.Active;
            _config.SaveConfiguration();

            _logger.LogDebug("Configuration saved");
        }

        /// <inheritdoc/>
        public QuickConnectResult TryConnect()
        {
            ExpireRequests();

            if (State != QuickConnectState.Active)
            {
                _logger.LogDebug("Refusing quick connect initiation request, current state is {State}", State);
                throw new AuthenticationException("Quick connect is not active on this server");
            }

            var code = GenerateCode();
            var result = new QuickConnectResult()
            {
                Secret = GenerateSecureRandom(),
                DateAdded = DateTime.UtcNow,
                Code = code
            };

            _currentRequests[code] = result;
            return result;
        }

        /// <inheritdoc/>
        public QuickConnectResult CheckRequestStatus(string secret)
        {
            ExpireRequests();
            AssertActive();

            string code = _currentRequests.Where(x => x.Value.Secret == secret).Select(x => x.Value.Code).DefaultIfEmpty(string.Empty).First();

            if (!_currentRequests.TryGetValue(code, out QuickConnectResult result))
            {
                throw new ResourceNotFoundException("Unable to find request with provided secret");
            }

            return result;
        }

        /// <inheritdoc/>
        public void AuthenticateRequest(AuthenticationRequest request, string token)
        {
            if (!_quickConnectTokens.TryGetValue(token, out var entry))
            {
                throw new SecurityException("Unknown quick connect token");
            }

            request.UserId = entry.UserId;
            _quickConnectTokens.Remove(token, out _);

            _sessionManager.AuthenticateQuickConnect(request, token);
        }

        /// <inheritdoc/>
        public string GenerateCode()
        {
            Span<byte> raw = stackalloc byte[4];

            int min = (int)Math.Pow(10, CodeLength - 1);
            int max = (int)Math.Pow(10, CodeLength);

            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                _rng.GetBytes(raw);
                scale = BitConverter.ToUInt32(raw);
            }

            int code = (int)(min + ((max - min) * (scale / (double)uint.MaxValue)));
            return code.ToString(CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public bool AuthorizeRequest(Guid userId, string code)
        {
            ExpireRequests();
            AssertActive();

            if (!_currentRequests.TryGetValue(code, out QuickConnectResult result))
            {
                throw new ResourceNotFoundException("Unable to find request");
            }

            if (result.Authenticated)
            {
                throw new InvalidOperationException("Request is already authorized");
            }

            result.Authentication = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            // Change the time on the request so it expires one minute into the future. It can't expire immediately as otherwise some clients wouldn't ever see that they have been authenticated.
            var added = result.DateAdded ?? DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(Timeout));
            result.DateAdded = added.Subtract(TimeSpan.FromMinutes(Timeout - 1));

            _quickConnectTokens[result.Authentication] = (TokenName, userId);

            _logger.LogDebug("Authorizing device with code {Code} to login as user {userId}", code, userId);

            return true;
        }

        /// <inheritdoc/>
        public int DeleteAllDevices(Guid user)
        {
            var tokens = _quickConnectTokens
                .Where(entry => entry.Value.Token.StartsWith(TokenName, StringComparison.Ordinal) && entry.Value.UserId == user)
                .ToList();

            var removed = 0;
            foreach (var token in tokens)
            {
                _quickConnectTokens.Remove(token.Key, out _);
                _logger.LogDebug("Deleted token {AccessToken}", token.Key);
                removed++;
            }

            return removed;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Dispose unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rng?.Dispose();
            }
        }

        private string GenerateSecureRandom(int length = 32)
        {
            Span<byte> bytes = stackalloc byte[length];
            _rng.GetBytes(bytes);

            return Convert.ToHexString(bytes);
        }

        /// <inheritdoc/>
        public void ExpireRequests(bool expireAll = false)
        {
            // Check if quick connect should be deactivated
            if (State == QuickConnectState.Active && DateTime.UtcNow > DateActivated.AddMinutes(Timeout) && !expireAll)
            {
                _logger.LogDebug("Quick connect time expired, deactivating");
                SetState(QuickConnectState.Available);
                expireAll = true;
            }

            // Expire stale connection requests
            foreach (var (_, currentRequest) in _currentRequests)
            {
                var added = currentRequest.DateAdded ?? DateTime.UnixEpoch;
                if (expireAll || DateTime.UtcNow > added.AddMinutes(Timeout))
                {
                    var code = currentRequest.Code;
                    _logger.LogDebug("Removing expired request {Code}", code);

                    if (!_currentRequests.TryRemove(code, out _))
                    {
                        _logger.LogWarning("Request {Code} already expired", code);
                    }
                }
            }
        }

        private void ReloadConfiguration()
        {
            State = _config.Configuration.QuickConnectAvailable ? QuickConnectState.Available : QuickConnectState.Unavailable;
        }
    }
}
