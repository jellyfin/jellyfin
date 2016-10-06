using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Connect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class UsageReporter
    {
        private readonly IApplicationHost _applicationHost;
        private readonly IHttpClient _httpClient;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;
        private const string MbAdminUrl = "https://www.mb3admin.com/admin/";

        public UsageReporter(IApplicationHost applicationHost, IHttpClient httpClient, IUserManager userManager, ILogger logger)
        {
            _applicationHost = applicationHost;
            _httpClient = httpClient;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task ReportServerUsage(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var data = new Dictionary<string, string>
            {
                { "feature", _applicationHost.Name }, 
                { "mac", _applicationHost.SystemId }, 
                { "serverid", _applicationHost.SystemId }, 
                { "deviceid", _applicationHost.SystemId }, 
                { "ver", _applicationHost.ApplicationVersion.ToString() }, 
                { "platform", _applicationHost.OperatingSystemDisplayName }, 
                { "isservice", _applicationHost.IsRunningAsService.ToString().ToLower()}
            };

            var users = _userManager.Users.ToList();

            data["localusers"] = users.Count(i => !i.ConnectLinkType.HasValue).ToString(CultureInfo.InvariantCulture);
            data["guests"] = users.Count(i => i.ConnectLinkType.HasValue && i.ConnectLinkType.Value == UserLinkType.Guest).ToString(CultureInfo.InvariantCulture);
            data["linkedusers"] = users.Count(i => i.ConnectLinkType.HasValue && i.ConnectLinkType.Value == UserLinkType.LinkedUser).ToString(CultureInfo.InvariantCulture);

            data["plugins"] = string.Join(",", _applicationHost.Plugins.Select(i => i.Id).ToArray());

            var logErrors = false;
#if DEBUG
            logErrors = true;
#endif
            var options = new HttpRequestOptions
            {
                Url = MbAdminUrl + "service/registration/ping",
                CancellationToken = cancellationToken,

                // Seeing block length errors
                EnableHttpCompression = false,

                LogRequest = false,
                LogErrors = logErrors,
                BufferContent = false
            };

            options.SetPostData(data);

            using (var response = await _httpClient.SendAsync(options, "POST").ConfigureAwait(false))
            {
                
            }
        }

        public async Task ReportAppUsage(ClientInfo app, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(app.DeviceId))
            {
                throw new ArgumentException("Client info must have a device Id");
            }

            _logger.Info("App Activity: app: {0}, version: {1}, deviceId: {2}, deviceName: {3}",
                app.AppName ?? "Unknown App",
                app.AppVersion ?? "Unknown",
                app.DeviceId,
                app.DeviceName ?? "Unknown");

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

            var logErrors = false;

#if DEBUG
            logErrors = true;
#endif
            var options = new HttpRequestOptions
            {
                Url = MbAdminUrl + "service/registration/ping",
                CancellationToken = cancellationToken,

                // Seeing block length errors
                EnableHttpCompression = false,

                LogRequest = false,
                LogErrors = logErrors,
                BufferContent = false
            };

            options.SetPostData(data);

            using (var response = await _httpClient.SendAsync(options, "POST").ConfigureAwait(false))
            {

            }
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
