using MediaBrowser.Common.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.Security
{
    public class UsageReporter
    {
        private readonly IApplicationHost _applicationHost;
        private readonly INetworkManager _networkManager;
        private readonly IHttpClient _httpClient;

        public UsageReporter(IApplicationHost applicationHost, INetworkManager networkManager, IHttpClient httpClient)
        {
            _applicationHost = applicationHost;
            _networkManager = networkManager;
            _httpClient = httpClient;
        }

        public Task ReportServerUsage(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mac = _networkManager.GetMacAddress();

            var plugins = string.Join("|", _applicationHost.Plugins.Select(i => i.Name).ToArray());

            var data = new Dictionary<string, string>
            {
                { "feature", _applicationHost.Name }, 
                { "mac", mac }, 
                { "ver", _applicationHost.ApplicationVersion.ToString() }, 
                { "platform", Environment.OSVersion.VersionString }, 
                { "isservice", _applicationHost.IsRunningAsService.ToString().ToLower()}, 
                { "plugins", plugins}
            };

            return _httpClient.Post(Constants.Constants.MbAdminUrl + "service/registration/ping", data, cancellationToken);
        }

        public Task ReportAppUsage(ClientInfo app, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = new Dictionary<string, string>
            {
                { "feature", app.AppName ?? "Unknown App" }, 
                { "mac", app.DeviceId ?? _networkManager.GetMacAddress() }, 
                { "ver", app.AppVersion ?? "Unknown" }, 
                { "platform", app.DeviceName }, 
            };

            return _httpClient.Post(Constants.Constants.MbAdminUrl + "service/registration/ping", data, cancellationToken);
        }
    }

    public class ClientInfo
    {
        public string AppName { get; set; }
        public string AppVersion { get; set; }
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
    }
}
