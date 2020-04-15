using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.QuickConnect;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.QuickConnect
{
    /// <summary>
    /// Quick connect implementation.
    /// </summary>
    public class QuickConnectManager : IQuickConnect
    {
        private readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private Dictionary<string, QuickConnectResult> _currentRequests = new Dictionary<string, QuickConnectResult>();

        private ILogger _logger;
        private IUserManager _userManager;
        private ILocalizationManager _localizationManager;
        private IJsonSerializer _jsonSerializer;
        private IAuthenticationRepository _authenticationRepository;
        private IAuthorizationContext _authContext;
        private IServerApplicationHost _appHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickConnectManager"/> class.
        /// Should only be called at server startup when a singleton is created.
        /// </summary>
        /// <param name="loggerFactory">Logger.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="localization">Localization.</param>
        /// <param name="jsonSerializer">JSON serializer.</param>
        /// <param name="appHost">Application host.</param>
        /// <param name="authContext">Authentication context.</param>
        /// <param name="authenticationRepository">Authentication repository.</param>
        public QuickConnectManager(
            ILoggerFactory loggerFactory,
            IUserManager userManager,
            ILocalizationManager localization,
            IJsonSerializer jsonSerializer,
            IServerApplicationHost appHost,
            IAuthorizationContext authContext,
            IAuthenticationRepository authenticationRepository)
        {
            _logger = loggerFactory.CreateLogger(nameof(QuickConnectManager));
            _userManager = userManager;
            _localizationManager = localization;
            _jsonSerializer = jsonSerializer;
            _appHost = appHost;
            _authContext = authContext;
            _authenticationRepository = authenticationRepository;
        }

        /// <inheritdoc/>
        public int CodeLength { get; set; } = 6;

        /// <inheritdoc/>
        public string TokenNamePrefix { get; set; } = "QuickConnect-";

        /// <inheritdoc/>
        public QuickConnectState State { get; private set; } = QuickConnectState.Unavailable;

        /// <inheritdoc/>
        public int RequestExpiry { get; set; } = 30;

        /// <inheritdoc/>
        public void AssertActive()
        {
            if (State != QuickConnectState.Active)
            {
                throw new InvalidOperationException("Quick connect is not active on this server");
            }
        }

        /// <inheritdoc/>
        public void SetEnabled(QuickConnectState newState)
        {
            _logger.LogDebug("Changed quick connect state from {0} to {1}", State, newState);

            State = newState;
        }

        /// <inheritdoc/>
        public QuickConnectResult TryConnect(string friendlyName)
        {
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
            AssertActive();
            ExpireRequests();

            string lookup = _currentRequests.Where(x => x.Value.Secret == secret).Select(x => x.Value.Lookup).DefaultIfEmpty(string.Empty).First();

            _logger.LogDebug("Transformed private identifier {0} into public lookup {1}", secret, lookup);

            if (!_currentRequests.ContainsKey(lookup))
            {
                throw new KeyNotFoundException("Unable to find request with provided identifier");
            }

            return _currentRequests[lookup];
        }

        /// <inheritdoc/>
        public List<QuickConnectResultDto> GetCurrentRequests()
        {
            return GetCurrentRequestsInternal().Select(x => (QuickConnectResultDto)x).ToList();
        }

        /// <inheritdoc/>
        public List<QuickConnectResult> GetCurrentRequestsInternal()
        {
            AssertActive();
            ExpireRequests();
            return _currentRequests.Values.ToList();
        }

        /// <inheritdoc/>
        public string GenerateCode()
        {
            // TODO: output may be biased

            int min = (int)Math.Pow(10, CodeLength - 1);
            int max = (int)Math.Pow(10, CodeLength);

            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                byte[] raw = new byte[4];
                _rng.GetBytes(raw);
                scale = BitConverter.ToUInt32(raw, 0);
            }

            int code = (int)(min + (max - min) * (scale / (double)uint.MaxValue));
            return code.ToString(CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public bool AuthorizeRequest(IRequest request, string lookup)
        {
            AssertActive();

            var auth = _authContext.GetAuthorizationInfo(request);

            ExpireRequests();

            if (!_currentRequests.ContainsKey(lookup))
            {
                throw new KeyNotFoundException("Unable to find request");
            }

            QuickConnectResult result = _currentRequests[lookup];

            if (result.Authenticated)
            {
                throw new InvalidOperationException("Request is already authorized");
            }

            result.Authentication = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            // Advance the time on the request so it expires sooner as the client will pick up the changes in a few seconds
            result.DateAdded = result.DateAdded.Subtract(new TimeSpan(0, RequestExpiry - 1, 0));

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

        private string GenerateSecureRandom(int length = 32)
        {
            var bytes = new byte[length];
            _rng.GetBytes(bytes);

            return string.Join(string.Empty, bytes.Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private void ExpireRequests()
        {
            var delete = new List<string>();
            var values = _currentRequests.Values.ToList();

            for (int i = 0; i < _currentRequests.Count; i++)
            {
                if (DateTime.Now > values[i].DateAdded.AddMinutes(RequestExpiry))
                {
                    delete.Add(values[i].Lookup);
                }
            }

            foreach (var lookup in delete)
            {
                _logger.LogDebug("Removing expired request {0}", lookup);
                _currentRequests.Remove(lookup);
            }
        }
    }
}
