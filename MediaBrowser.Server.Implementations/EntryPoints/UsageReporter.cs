using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class UsageReporter
    {
        private readonly IApplicationHost _applicationHost;
        private readonly INetworkManager _networkManager;
        private readonly IHttpClient _httpClient;
        private const string MbAdminUrl = "https://www.mb3admin.com/admin/";

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

            var data = new Dictionary<string, string>
            {
                { "feature", _applicationHost.Name }, 
                { "mac", mac }, 
                { "serverid", _applicationHost.SystemId }, 
                { "deviceid", _applicationHost.SystemId }, 
                { "ver", _applicationHost.ApplicationVersion.ToString() }, 
                { "platform", _applicationHost.OperatingSystemDisplayName }, 
                { "isservice", _applicationHost.IsRunningAsService.ToString().ToLower()}
            };

            return _httpClient.Post(MbAdminUrl + "service/registration/ping", data, cancellationToken);
        }

        public Task ReportAppUsage(ClientInfo app, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(app.DeviceId))
            {
                throw new ArgumentException("Client info must have a device Id");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var data = new Dictionary<string, string>
            {
                { "feature", app.AppName ?? "Unknown App" }, 
                { "serverid", _applicationHost.SystemId }, 
                { "deviceid", app.DeviceId }, 
                { "mac", app.DeviceId }, 
                { "ver", app.AppVersion ?? "Unknown" }, 
                { "platform", app.DeviceName }, 
            };

            return _httpClient.Post(MbAdminUrl + "service/registration/ping", data, cancellationToken);
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
