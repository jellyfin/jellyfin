using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class UsageEntryPoint
    /// </summary>
    public class UsageEntryPoint : IServerEntryPoint
    {
        private readonly IApplicationHost _applicationHost;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IUserManager _userManager;
        private readonly IServerConfigurationManager _config;

        private readonly ConcurrentDictionary<Guid, ClientInfo> _apps = new ConcurrentDictionary<Guid, ClientInfo>();

        public UsageEntryPoint(ILogger logger, IApplicationHost applicationHost, IHttpClient httpClient, ISessionManager sessionManager, IUserManager userManager, IServerConfigurationManager config)
        {
            _logger = logger;
            _applicationHost = applicationHost;
            _httpClient = httpClient;
            _sessionManager = sessionManager;
            _userManager = userManager;
            _config = config;

            _sessionManager.SessionStarted += _sessionManager_SessionStarted;
        }

        void _sessionManager_SessionStarted(object sender, SessionEventArgs e)
        {
            var session = e.SessionInfo;

            if (!string.IsNullOrEmpty(session.Client) &&
                !string.IsNullOrEmpty(session.DeviceName) &&
                !string.IsNullOrEmpty(session.DeviceId) &&
                !string.IsNullOrEmpty(session.ApplicationVersion))
            {
                var keys = new List<string>
                {
                    session.Client,
                    session.DeviceName,
                    session.DeviceId,
                    session.ApplicationVersion
                };

                var key = string.Join("_", keys.ToArray()).GetMD5();

                _apps.GetOrAdd(key, guid => GetNewClientInfo(session));
            }
        }

        private async void ReportNewSession(ClientInfo client)
        {
            if (!_config.Configuration.EnableAnonymousUsageReporting)
            {
                return;
            }

            try
            {
                await new UsageReporter(_applicationHost, _httpClient, _userManager, _logger)
                    .ReportAppUsage(client, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending anonymous usage statistics.", ex);
            }
        }

        private ClientInfo GetNewClientInfo(SessionInfo session)
        {
            var info = new ClientInfo
            {
                AppName = session.Client,
                AppVersion = session.ApplicationVersion,
                DeviceName = session.DeviceName,
                DeviceId = session.DeviceId
            };

            ReportNewSession(info);

            return info;
        }

        public async void Run()
        {
            await Task.Delay(5000).ConfigureAwait(false);
            OnTimerFired();
        }

        /// <summary>
        /// Called when [timer fired].
        /// </summary>
        private async void OnTimerFired()
        {
            if (!_config.Configuration.EnableAnonymousUsageReporting)
            {
                return;
            }

            try
            {
                await new UsageReporter(_applicationHost, _httpClient, _userManager, _logger)
                    .ReportServerUsage(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending anonymous usage statistics.", ex);
            }
        }

        public void Dispose()
        {
            _sessionManager.SessionStarted -= _sessionManager_SessionStarted;
        }
    }
}
