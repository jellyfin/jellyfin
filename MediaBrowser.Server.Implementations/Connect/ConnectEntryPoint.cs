using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace MediaBrowser.Server.Implementations.Connect
{
    public class ConnectEntryPoint : IServerEntryPoint
    {
        private Timer _timer;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly IConnectManager _connectManager;

        private readonly INetworkManager _networkManager;

        public ConnectEntryPoint(IHttpClient httpClient, IApplicationPaths appPaths, ILogger logger, INetworkManager networkManager, IConnectManager connectManager)
        {
            _httpClient = httpClient;
            _appPaths = appPaths;
            _logger = logger;
            _networkManager = networkManager;
            _connectManager = connectManager;
        }

        public void Run()
        {
            LoadCachedAddress();

            _timer = new Timer(TimerCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromHours(3));
        }

        private async void TimerCallback(object state)
        {
            try
            {
                using (var stream = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = "http://bot.whatismyipaddress.com/"

                }).ConfigureAwait(false))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var address = await reader.ReadToEndAsync().ConfigureAwait(false);

                        if (IsValid(address))
                        {
                            ((ConnectManager) _connectManager).OnWanAddressResolved(address);
                            CacheAddress(address);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private string CacheFilePath
        {
            get { return Path.Combine(_appPaths.DataPath, "wan.txt"); }
        }

        private void CacheAddress(string address)
        {
            var path = CacheFilePath;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, address, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving data", ex);
            }
        }

        private void LoadCachedAddress()
        {
            var path = CacheFilePath;

            try
            {
                var endpoint = File.ReadAllText(path, Encoding.UTF8);

                if (IsValid(endpoint))
                {
                    ((ConnectManager)_connectManager).OnWanAddressResolved(endpoint);
                }
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

        private bool IsValid(string address)
        {
            IPAddress ipAddress;
            return IPAddress.TryParse(address, out ipAddress);
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
