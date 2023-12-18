using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
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
    public class QuickConnectManager : IQuickConnect
    {
        /// <summary>
        /// The length of user facing codes.
        /// </summary>
        private const int CodeLength = 6;

        /// <summary>
        /// The time (in minutes) that the quick connect token is valid.
        /// </summary>
        private const int Timeout = 10;

        private readonly ConcurrentDictionary<string, QuickConnectResult> _currentRequests = new();
        private readonly ConcurrentDictionary<string, (DateTime Timestamp, AuthenticationResult AuthenticationResult)> _authorizedSecrets = new();

        private readonly IServerConfigurationManager _config;
        private readonly ILogger<QuickConnectManager> _logger;
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickConnectManager"/> class.
        /// Should only be called at server startup when a singleton is created.
        /// </summary>
        /// <param name="config">Configuration.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="sessionManager">Session Manager.</param>
        public QuickConnectManager(
            IServerConfigurationManager config,
            ILogger<QuickConnectManager> logger,
            ISessionManager sessionManager)
        {
            _config = config;
            _logger = logger;
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        public bool IsEnabled => _config.Configuration.QuickConnectAvailable;

        /// <summary>
        /// Assert that quick connect is currently active and throws an exception if it is not.
        /// </summary>
        private void AssertActive()
        {
            if (!IsEnabled)
            {
                throw new AuthenticationException("Quick connect is not active on this server");
            }
        }

        /// <inheritdoc/>
        public QuickConnectResult TryConnect(AuthorizationInfo authorizationInfo)
        {
            ArgumentException.ThrowIfNullOrEmpty(authorizationInfo.DeviceId);
            ArgumentException.ThrowIfNullOrEmpty(authorizationInfo.Device);
            ArgumentException.ThrowIfNullOrEmpty(authorizationInfo.Client);
            ArgumentException.ThrowIfNullOrEmpty(authorizationInfo.Version);

            AssertActive();
            ExpireRequests();

            var secret = GenerateSecureRandom();
            var code = GenerateCode();
            var result = new QuickConnectResult(
                secret,
                code,
                DateTime.UtcNow,
                authorizationInfo.DeviceId,
                authorizationInfo.Device,
                authorizationInfo.Client,
                authorizationInfo.Version);

            _currentRequests[code] = result;
            return result;
        }

        /// <inheritdoc/>
        public QuickConnectResult CheckRequestStatus(string secret)
        {
            AssertActive();
            ExpireRequests();

            string code = _currentRequests.Where(x => x.Value.Secret == secret).Select(x => x.Value.Code).DefaultIfEmpty(string.Empty).First();

            if (!_currentRequests.TryGetValue(code, out QuickConnectResult? result))
            {
                throw new ResourceNotFoundException("Unable to find request with provided secret");
            }

            return result;
        }

        /// <summary>
        /// Generates a short code to display to the user to uniquely identify this request.
        /// </summary>
        /// <returns>A short, unique alphanumeric string.</returns>
        private string GenerateCode()
        {
            Span<byte> raw = stackalloc byte[4];

            int min = (int)Math.Pow(10, CodeLength - 1);
            int max = (int)Math.Pow(10, CodeLength);

            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                RandomNumberGenerator.Fill(raw);
                scale = BitConverter.ToUInt32(raw);
            }

            int code = (int)(min + ((max - min) * (scale / (double)uint.MaxValue)));
            return code.ToString(CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public async Task<bool> AuthorizeRequest(Guid userId, string code)
        {
            AssertActive();
            ExpireRequests();

            if (!_currentRequests.TryGetValue(code, out QuickConnectResult? result))
            {
                throw new ResourceNotFoundException("Unable to find request");
            }

            if (result.Authenticated)
            {
                throw new InvalidOperationException("Request is already authorized");
            }

            // Change the time on the request so it expires one minute into the future. It can't expire immediately as otherwise some clients wouldn't ever see that they have been authenticated.
            result.DateAdded = DateTime.UtcNow.Add(TimeSpan.FromMinutes(1));

            var authenticationResult = await _sessionManager.AuthenticateDirect(new AuthenticationRequest
            {
                UserId = userId,
                DeviceId = result.DeviceId,
                DeviceName = result.DeviceName,
                App = result.AppName,
                AppVersion = result.AppVersion
            }).ConfigureAwait(false);

            _authorizedSecrets[result.Secret] = (DateTime.UtcNow, authenticationResult);
            result.Authenticated = true;
            _currentRequests[code] = result;

            _logger.LogDebug("Authorizing device with code {Code} to login as user {UserId}", code, userId);

            return true;
        }

        /// <inheritdoc/>
        public AuthenticationResult GetAuthorizedRequest(string secret)
        {
            AssertActive();
            ExpireRequests();

            if (!_authorizedSecrets.TryGetValue(secret, out var result))
            {
                throw new ResourceNotFoundException("Unable to find request");
            }

            return result.AuthenticationResult;
        }

        private string GenerateSecureRandom(int length = 32)
        {
            Span<byte> bytes = stackalloc byte[length];
            RandomNumberGenerator.Fill(bytes);

            return Convert.ToHexString(bytes);
        }

        /// <summary>
        /// Expire quick connect requests that are over the time limit. If <paramref name="expireAll"/> is true, all requests are unconditionally expired.
        /// </summary>
        /// <param name="expireAll">If true, all requests will be expired.</param>
        private void ExpireRequests(bool expireAll = false)
        {
            // All requests before this timestamp have expired
            var minTime = DateTime.UtcNow.AddMinutes(-Timeout);

            // Expire stale connection requests
            foreach (var (_, currentRequest) in _currentRequests)
            {
                if (expireAll || currentRequest.DateAdded < minTime)
                {
                    var code = currentRequest.Code;
                    _logger.LogDebug("Removing expired request {Code}", code);

                    if (!_currentRequests.TryRemove(code, out _))
                    {
                        _logger.LogWarning("Request {Code} already expired", code);
                    }
                }
            }

            foreach (var (secret, (timestamp, _)) in _authorizedSecrets)
            {
                if (expireAll || timestamp < minTime)
                {
                    _logger.LogDebug("Removing expired secret {Secret}", secret);
                    if (!_authorizedSecrets.TryRemove(secret, out _))
                    {
                        _logger.LogWarning("Secret {Secret} already expired", secret);
                    }
                }
            }
        }
    }
}
