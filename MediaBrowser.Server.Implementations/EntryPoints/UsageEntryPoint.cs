using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class UsageEntryPoint
    /// </summary>
    public class UsageEntryPoint : IServerEntryPoint
    {
        private readonly IApplicationHost _applicationHost;
        private readonly INetworkManager _networkManager;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly ISessionManager _sessionManager;

        private Timer _timer;
        private readonly TimeSpan _frequency = TimeSpan.FromHours(24);

        private readonly ConcurrentDictionary<Guid, ClientInfo> _apps = new ConcurrentDictionary<Guid, ClientInfo>();

        public UsageEntryPoint(ILogger logger, IApplicationHost applicationHost, INetworkManager networkManager, IHttpClient httpClient, ISessionManager sessionManager)
        {
            _logger = logger;
            _applicationHost = applicationHost;
            _networkManager = networkManager;
            _httpClient = httpClient;
            _sessionManager = sessionManager;

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
            try
            {
                await new UsageReporter(_applicationHost, _networkManager, _httpClient)
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

            // Report usage to remote server, except for web client, since we already have data on that
            if (!string.Equals(info.AppName, "Dashboard", StringComparison.OrdinalIgnoreCase))
            {
                ReportNewSession(info);
            }

            return info;
        }

        public void Run()
        {
            _timer = new Timer(OnTimerFired, null, TimeSpan.FromMilliseconds(5000), _frequency);
        }

        /// <summary>
        /// Called when [timer fired].
        /// </summary>
        /// <param name="state">The state.</param>
        private async void OnTimerFired(object state)
        {
            try
            {
                await new UsageReporter(_applicationHost, _networkManager, _httpClient)
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

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
