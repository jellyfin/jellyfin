using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.Threading;

namespace MediaBrowser.Server.Implementations.Connect
{
    public class ConnectEntryPoint : IServerEntryPoint
    {
        private PeriodicTimer _timer;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly IConnectManager _connectManager;

        private readonly INetworkManager _networkManager;
        private readonly IApplicationHost _appHost;
        private readonly IFileSystem _fileSystem;

        public ConnectEntryPoint(IHttpClient httpClient, IApplicationPaths appPaths, ILogger logger, INetworkManager networkManager, IConnectManager connectManager, IApplicationHost appHost, IFileSystem fileSystem)
        {
            _httpClient = httpClient;
            _appPaths = appPaths;
            _logger = logger;
            _networkManager = networkManager;
            _connectManager = connectManager;
            _appHost = appHost;
            _fileSystem = fileSystem;
        }

        public void Run()
        {
            LoadCachedAddress();

            _timer = new PeriodicTimer(TimerCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromHours(1));
            ((ConnectManager)_connectManager).Start();
        }

        private readonly string[] _ipLookups =
        {
            "http://bot.whatismyipaddress.com",
            "https://connect.emby.media/service/ip"
        };

        private async void TimerCallback(object state)
        {
            IPAddress validIpAddress = null;

            foreach (var ipLookupUrl in _ipLookups)
            {
                try
                {
                    validIpAddress = await GetIpAddress(ipLookupUrl).ConfigureAwait(false);

                    // Try to find the ipv4 address, if present
                    if (validIpAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        break;
                    }
                }
                catch (HttpException)
                {
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting connection info", ex);
                }
            }

            // If this produced an ipv6 address, try again
            if (validIpAddress != null && validIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                foreach (var ipLookupUrl in _ipLookups)
                {
                    try
                    {
                        var newAddress = await GetIpAddress(ipLookupUrl, true).ConfigureAwait(false);

                        // Try to find the ipv4 address, if present
                        if (newAddress.AddressFamily == AddressFamily.InterNetwork)
                        {
                            validIpAddress = newAddress;
                            break;
                        }
                    }
                    catch (HttpException)
                    {
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting connection info", ex);
                    }
                }
            }

            if (validIpAddress != null)
            {
                ((ConnectManager)_connectManager).OnWanAddressResolved(validIpAddress);
                CacheAddress(validIpAddress);
            }
        }

        private async Task<IPAddress> GetIpAddress(string lookupUrl, bool preferIpv4 = false)
        {
            // Sometimes whatismyipaddress might fail, but it won't do us any good having users raise alarms over it.
            var logErrors = false;

#if DEBUG
            logErrors = true;
#endif
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = lookupUrl,
                UserAgent = "Emby/" + _appHost.ApplicationVersion,
                LogErrors = logErrors,

                // Seeing block length errors with our server
                EnableHttpCompression = false,
                PreferIpv4 = preferIpv4,
                BufferContent = false

            }).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    var addressString = await reader.ReadToEndAsync().ConfigureAwait(false);

                    return IPAddress.Parse(addressString);
                }
            }
        }

        private string CacheFilePath
        {
            get { return Path.Combine(_appPaths.DataPath, "wan.txt"); }
        }

        private void CacheAddress(IPAddress address)
        {
            var path = CacheFilePath;

            try
            {
                _fileSystem.CreateDirectory(Path.GetDirectoryName(path));
                _fileSystem.WriteAllText(path, address.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving data", ex);
            }
        }

        private void LoadCachedAddress()
        {
            var path = CacheFilePath;

            _logger.Info("Loading data from {0}", path);

            try
            {
                var endpoint = _fileSystem.ReadAllText(path, Encoding.UTF8);
                IPAddress ipAddress;

                if (IPAddress.TryParse(endpoint, out ipAddress))
                {
                    ((ConnectManager)_connectManager).OnWanAddressResolved(ipAddress);
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
