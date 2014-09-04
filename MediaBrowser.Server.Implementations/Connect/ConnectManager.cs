using System.Net;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Connect
{
    public class ConnectManager : IConnectManager
    {
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _json;
        private readonly IEncryptionManager _encryption;
        private readonly IHttpClient _httpClient;
        private readonly IServerApplicationHost _appHost;
        private readonly IServerConfigurationManager _config;

        public string ConnectServerId { get; set; }
        public string ConnectAccessKey { get; set; }

        public string WanIpAddress { get; private set; }

        public string WanApiAddress
        {
            get
            {
                var ip = WanIpAddress;

                if (!string.IsNullOrEmpty(ip))
                {
                    if (!ip.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !ip.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        ip = "http://" + ip;
                    }

                    return ip + ":" + _config.Configuration.HttpServerPortNumber.ToString(CultureInfo.InvariantCulture);
                }

                return null;
            }
        }

        public ConnectManager(ILogger logger,
            IApplicationPaths appPaths,
            IJsonSerializer json,
            IEncryptionManager encryption,
            IHttpClient httpClient,
            IServerApplicationHost appHost,
            IServerConfigurationManager config)
        {
            _logger = logger;
            _appPaths = appPaths;
            _json = json;
            _encryption = encryption;
            _httpClient = httpClient;
            _appHost = appHost;
            _config = config;

            LoadCachedData();
        }

        internal void OnWanAddressResolved(string address)
        {
            WanIpAddress = address;

            UpdateConnectInfo();
        }

        private async void UpdateConnectInfo()
        {
            var wanApiAddress = WanApiAddress;

            if (string.IsNullOrWhiteSpace(wanApiAddress))
            {
                _logger.Warn("Cannot update Media Browser Connect information without a WanApiAddress");
                return;
            }

            try
            {
                var hasExistingRecord = !string.IsNullOrWhiteSpace(ConnectServerId) &&
                                  !string.IsNullOrWhiteSpace(ConnectAccessKey);

                var createNewRegistration = !hasExistingRecord;

                if (hasExistingRecord)
                {
                    try
                    {
                        await UpdateServerRegistration(wanApiAddress).ConfigureAwait(false);
                    }
                    catch (HttpException ex)
                    {
                        if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound || ex.StatusCode.Value != HttpStatusCode.Unauthorized)
                        {
                            throw;
                        }

                        createNewRegistration = true;
                    }
                }

                if (createNewRegistration)
                {
                    await CreateServerRegistration(wanApiAddress).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error registering with Connect", ex);
            }
        }

        private async Task CreateServerRegistration(string wanApiAddress)
        {
            var url = "Servers";
            url = GetConnectUrl(url);

            var postData = new Dictionary<string, string>
            {
                {"name", _appHost.FriendlyName}, 
                {"url", wanApiAddress}
            };

            using (var stream = await _httpClient.Post(url, postData, CancellationToken.None).ConfigureAwait(false))
            {
                var data = _json.DeserializeFromStream<ServerRegistrationResponse>(stream);

                ConnectServerId = data.Id;
                ConnectAccessKey = data.AccessKey;

                CacheData();
            }
        }

        private async Task UpdateServerRegistration(string wanApiAddress)
        {
            var url = "Servers";
            url = GetConnectUrl(url);
            url += "?id=" + ConnectServerId;

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None
            };

            options.SetPostData(new Dictionary<string, string>
            {
                {"name", _appHost.FriendlyName}, 
                {"url", wanApiAddress}
            });

            options.RequestHeaders.Add("X-Connect-Token", ConnectAccessKey);

            // No need to examine the response
            using (var stream = (await _httpClient.Post(options).ConfigureAwait(false)).Content)
            {
            }
        }

        private string CacheFilePath
        {
            get { return Path.Combine(_appPaths.DataPath, "connect.txt"); }
        }

        private void CacheData()
        {
            var path = CacheFilePath;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var json = _json.SerializeToString(new ConnectData
                {
                    AccessKey = ConnectAccessKey,
                    ServerId = ConnectServerId
                });

                var encrypted = _encryption.EncryptString(json);

                File.WriteAllText(path, encrypted, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving data", ex);
            }
        }

        private void LoadCachedData()
        {
            var path = CacheFilePath;

            try
            {
                var encrypted = File.ReadAllText(path, Encoding.UTF8);

                var json = _encryption.DecryptString(encrypted);

                var data = _json.DeserializeFromString<ConnectData>(json);

                ConnectAccessKey = data.AccessKey;
                ConnectServerId = data.ServerId;
            }
            catch (IOException)
            {
                // File isn't there. no biggie
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error loading data", ex);
            }
        }

        private string GetConnectUrl(string handler)
        {
            return "http://mb3admin.com/test/connect/" + handler;
        }
    }
}
