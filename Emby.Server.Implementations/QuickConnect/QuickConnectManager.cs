using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.QuickConnect;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.QuickConnect
{
    /// <summary>
    /// Quick connect implementation.
    /// </summary>
    public class QuickConnectManager : IQuickConnect, IDisposable
    {
        private readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private readonly ConcurrentDictionary<string, QuickConnectResult> _currentRequests = new ConcurrentDictionary<string, QuickConnectResult>();

        private readonly IServerConfigurationManager _config;
        private readonly ILogger<QuickConnectManager> _logger;
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly IAuthorizationContext _authContext;
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickConnectManager"/> class.
        /// Should only be called at server startup when a singleton is created.
        /// </summary>
        /// <param name="config">Configuration.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="appHost">Application host.</param>
        /// <param name="authContext">Authentication context.</param>
        /// <param name="authenticationRepository">Authentication repository.</param>
        public QuickConnectManager(
            IServerConfigurationManager config,
            ILogger<QuickConnectManager> logger,
            IServerApplicationHost appHost,
            IAuthorizationContext authContext,
            IAuthenticationRepository authenticationRepository)
        {
            _config = config;
            _logger = logger;
            _appHost = appHost;
            _authContext = authContext;
            _authenticationRepository = authenticationRepository;

            ReloadConfiguration();
        }

        /// <inheritdoc/>
        public int CodeLength { get; set; } = 6;

        /// <inheritdoc/>
        public string TokenNamePrefix { get; set; } = "QuickConnect-";

        /// <inheritdoc/>
        public QuickConnectState State { get; private set; } = QuickConnectState.Unavailable;

        /// <inheritdoc/>
        public int RequestExpiry { get; set; } = 30;

        private bool TemporaryActivation { get; set; } = false;

        private DateTime DateActivated { get; set; }

        /// <inheritdoc/>
        public void AssertActive()
        {
            if (State != QuickConnectState.Active)
            {
                throw new InvalidOperationException("Quick connect is not active on this server");
            }
        }

        /// <inheritdoc/>
        public QuickConnectResult Activate()
        {
            // This should not call SetEnabled since that would persist the "temporary" activation to the configuration file
            State = QuickConnectState.Active;
            DateActivated = DateTime.Now;
            TemporaryActivation = true;

            return new QuickConnectResult();
        }

        /// <inheritdoc/>
        public void SetEnabled(QuickConnectState newState)
        {
            _logger.LogDebug("Changed quick connect state from {0} to {1}", State, newState);

            ExpireRequests(true);
            State = newState;

            _config.SaveConfiguration("quickconnect", new QuickConnectConfiguration()
            {
                State = State
            });

            _logger.LogDebug("Configuration saved");
        }

        /// <inheritdoc/>
        public QuickConnectResult TryConnect(string friendlyName)
        {
            ExpireRequests();

            if (State != QuickConnectState.Active)
            {
                _logger.LogDebug("Refusing quick connect initiation request, current state is {0}", State);

                return new QuickConnectResult()
                {
                    Error = "Quick connect is not active on this server"
                };
            }

            _logger.LogDebug("Got new quick connect request from {friendlyName}", friendlyName);

            var lookup = GenerateSecureRandom();
            var result = new QuickConnectResult()
            {
                Lookup = lookup,
                Secret = GenerateSecureRandom(),
                FriendlyName = friendlyName,
                DateAdded = DateTime.Now,
                Code = GenerateCode()
            };

            _currentRequests[lookup] = result;
            return result;
        }

        /// <inheritdoc/>
        public QuickConnectResult CheckRequestStatus(string secret)
        {
            ExpireRequests();
            AssertActive();

            string lookup = _currentRequests.Where(x => x.Value.Secret == secret).Select(x => x.Value.Lookup).DefaultIfEmpty(string.Empty).First();

            if (!_currentRequests.TryGetValue(lookup, out QuickConnectResult result))
            {
                throw new KeyNotFoundException("Unable to find request with provided identifier");
            }

            return result;
        }

        /// <inheritdoc/>
        public List<QuickConnectResultDto> GetCurrentRequests()
        {
            return GetCurrentRequestsInternal().Select(x => (QuickConnectResultDto)x).ToList();
        }

        /// <inheritdoc/>
        public List<QuickConnectResult> GetCurrentRequestsInternal()
        {
            ExpireRequests();
            AssertActive();
            return _currentRequests.Values.ToList();
        }

        /// <inheritdoc/>
        public string GenerateCode()
        {
            int min = (int)Math.Pow(10, CodeLength - 1);
            int max = (int)Math.Pow(10, CodeLength);

            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                byte[] raw = new byte[4];
                _rng.GetBytes(raw);
                scale = BitConverter.ToUInt32(raw, 0);
            }

            int code = (int)(min + ((max - min) * (scale / (double)uint.MaxValue)));
            return code.ToString(CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public bool AuthorizeRequest(IRequest request, string lookup)
        {
            ExpireRequests();
            AssertActive();

            var auth = _authContext.GetAuthorizationInfo(request);

            if (!_currentRequests.TryGetValue(lookup, out QuickConnectResult result))
            {
                throw new KeyNotFoundException("Unable to find request");
            }

            if (result.Authenticated)
            {
                throw new InvalidOperationException("Request is already authorized");
            }

            result.Authentication = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            // Advance the time on the request so it expires sooner as the client will pick up the changes in a few seconds
            var added = result.DateAdded ?? DateTime.Now.Subtract(new TimeSpan(0, RequestExpiry, 0));
            result.DateAdded = added.Subtract(new TimeSpan(0, RequestExpiry - 1, 0));

            _authenticationRepository.Create(new AuthenticationInfo
            {
                AppName = TokenNamePrefix + result.FriendlyName,
                AccessToken = result.Authentication,
                DateCreated = DateTime.UtcNow,
                DeviceId = _appHost.SystemId,
                DeviceName = _appHost.FriendlyName,
                AppVersion = _appHost.ApplicationVersionString,
                UserId = auth.UserId
            });

            _logger.LogInformation("Allowing device {0} to login as user {1} with quick connect code {2}", result.FriendlyName, auth.User.Name, result.Code);

            return true;
        }

        /// <inheritdoc/>
        public int DeleteAllDevices(Guid user)
        {
            var raw = _authenticationRepository.Get(new AuthenticationInfoQuery()
            {
                DeviceId = _appHost.SystemId,
                UserId = user
            });

            var tokens = raw.Items.Where(x => x.AppName.StartsWith(TokenNamePrefix, StringComparison.CurrentCulture));

            foreach (var token in tokens)
            {
                _authenticationRepository.Delete(token);
                _logger.LogDebug("Deleted token {0}", token.AccessToken);
            }

            return tokens.Count();
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
            var bytes = new byte[length];
            _rng.GetBytes(bytes);

            return string.Join(string.Empty, bytes.Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Expire quick connect requests that are over the time limit. If <paramref name="expireAll"/> is true, all requests are unconditionally expired.
        /// </summary>
        /// <param name="expireAll">If true, all requests will be expired.</param>
        private void ExpireRequests(bool expireAll = false)
        {
            // Check if quick connect should be deactivated
            if (TemporaryActivation && DateTime.Now > DateActivated.AddMinutes(10) && State == QuickConnectState.Active && !expireAll)
            {
                _logger.LogDebug("Quick connect time expired, deactivating");
                SetEnabled(QuickConnectState.Available);
                expireAll = true;
                TemporaryActivation = false;
            }

            // Expire stale connection requests
            var delete = new List<string>();
            var values = _currentRequests.Values.ToList();

            for (int i = 0; i < values.Count; i++)
            {
                var added = values[i].DateAdded ?? DateTime.UnixEpoch;
                if (DateTime.Now > added.AddMinutes(RequestExpiry) || expireAll)
                {
                    delete.Add(values[i].Lookup);
                }
            }

            foreach (var lookup in delete)
            {
                _logger.LogDebug("Removing expired request {lookup}", lookup);

                if (!_currentRequests.TryRemove(lookup, out _))
                {
                    _logger.LogWarning("Request {lookup} already expired", lookup);
                }
            }
        }

        private void ReloadConfiguration()
        {
            var config = _config.GetQuickConnectConfiguration();

            State = config.State;
        }
    }
}
