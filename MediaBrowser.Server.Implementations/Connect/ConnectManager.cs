using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
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
        private readonly IUserManager _userManager;

        private ConnectData _data = new ConnectData();

        public string ConnectServerId
        {
            get { return _data.ServerId; }
        }
        public string ConnectAccessKey
        {
            get { return _data.AccessKey; }
        }

        public string DiscoveredWanIpAddress { get; private set; }

        public string WanIpAddress
        {
            get
            {
                var address = _config.Configuration.WanDdns;

                if (string.IsNullOrWhiteSpace(address))
                {
                    address = DiscoveredWanIpAddress;
                }

                return address;
            }
        }

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

                    return ip + ":" + _config.Configuration.PublicPort.ToString(CultureInfo.InvariantCulture);
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
            IServerConfigurationManager config, IUserManager userManager)
        {
            _logger = logger;
            _appPaths = appPaths;
            _json = json;
            _encryption = encryption;
            _httpClient = httpClient;
            _appHost = appHost;
            _config = config;
            _userManager = userManager;

            LoadCachedData();
        }

        internal void OnWanAddressResolved(string address)
        {
            DiscoveredWanIpAddress = address;

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
                        if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound ||
                            ex.StatusCode.Value != HttpStatusCode.Unauthorized)
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
                {"url", wanApiAddress}, 
                {"systemid", _appHost.SystemId}
            };

            using (var stream = await _httpClient.Post(url, postData, CancellationToken.None).ConfigureAwait(false))
            {
                var data = _json.DeserializeFromStream<ServerRegistrationResponse>(stream);

                _data.ServerId = data.Id;
                _data.AccessKey = data.AccessKey;

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
                {"url", wanApiAddress}, 
                {"systemid", _appHost.SystemId}
            });

            SetServerAccessToken(options);

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

                var json = _json.SerializeToString(_data);

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

                _data = _json.DeserializeFromString<ConnectData>(json);
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

        private User GetUser(string id)
        {
            var user = _userManager.GetUserById(id);

            if (user == null)
            {
                throw new ArgumentException("User not found.");
            }

            return user;
        }

        private string GetConnectUrl(string handler)
        {
            return "https://connect.mediabrowser.tv/service/" + handler;
        }

        public async Task<UserLinkResult> LinkUser(string userId, string connectUsername)
        {
            if (string.IsNullOrWhiteSpace(connectUsername))
            {
                throw new ArgumentNullException("connectUsername");
            }

            var connectUser = await GetConnectUser(new ConnectUserQuery
            {
                Name = connectUsername

            }, CancellationToken.None).ConfigureAwait(false);

            if (!connectUser.IsActive)
            {
                throw new ArgumentException("The Media Browser account has been disabled.");
            }

            var user = GetUser(userId);

            if (!string.IsNullOrWhiteSpace(user.ConnectUserId))
            {
                await RemoveLink(user, connectUser.Id).ConfigureAwait(false);
            }

            var url = GetConnectUrl("ServerAuthorizations");

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None
            };

            var accessToken = Guid.NewGuid().ToString("N");

            var postData = new Dictionary<string, string>
            {
                {"serverId", ConnectServerId},
                {"userId", connectUser.Id},
                {"userType", "Linked"},
                {"accessToken", accessToken}
            };

            options.SetPostData(postData);

            SetServerAccessToken(options);

            var result = new UserLinkResult();

            // No need to examine the response
            using (var stream = (await _httpClient.Post(options).ConfigureAwait(false)).Content)
            {
                var response = _json.DeserializeFromStream<ServerUserAuthorizationResponse>(stream);

                result.IsPending = string.Equals(response.AcceptStatus, "waiting", StringComparison.OrdinalIgnoreCase);
            }

            user.ConnectAccessKey = accessToken;
            user.ConnectUserName = connectUser.Name;
            user.ConnectUserId = connectUser.Id;
            user.ConnectLinkType = UserLinkType.LinkedUser;

            await user.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        public Task RemoveLink(string userId)
        {
            var user = GetUser(userId);

            return RemoveLink(user, user.ConnectUserId);
        }

        private async Task RemoveLink(User user, string connectUserId)
        {
            if (!string.IsNullOrWhiteSpace(connectUserId))
            {
                var url = GetConnectUrl("ServerAuthorizations");

                var options = new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = CancellationToken.None
                };

                var postData = new Dictionary<string, string>
                {
                    {"serverId", ConnectServerId},
                    {"userId", connectUserId}
                };

                options.SetPostData(postData);

                SetServerAccessToken(options);

                try
                {
                    // No need to examine the response
                    using (var stream = (await _httpClient.SendAsync(options, "DELETE").ConfigureAwait(false)).Content)
                    {
                    }
                }
                catch (HttpException ex)
                {
                    // If connect says the auth doesn't exist, we can handle that gracefully since this is a remove operation

                    if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                    {
                        throw;
                    }

                    _logger.Debug("Connect returned a 404 when removing a user auth link. Handling it.");
                }
            }

            user.ConnectAccessKey = null;
            user.ConnectUserName = null;
            user.ConnectUserId = null;
            user.ConnectLinkType = UserLinkType.LinkedUser;

            await user.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<ConnectUser> GetConnectUser(ConnectUserQuery query, CancellationToken cancellationToken)
        {
            var url = GetConnectUrl("user");

            if (!string.IsNullOrWhiteSpace(query.Id))
            {
                url = url + "?id=" + WebUtility.UrlEncode(query.Id);
            }
            else if (!string.IsNullOrWhiteSpace(query.Name))
            {
                url = url + "?name=" + WebUtility.UrlEncode(query.Name);
            }
            else if (!string.IsNullOrWhiteSpace(query.Email))
            {
                url = url + "?name=" + WebUtility.UrlEncode(query.Email);
            }

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            };

            SetServerAccessToken(options);

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                var response = _json.DeserializeFromStream<GetConnectUserResponse>(stream);

                return new ConnectUser
                {
                    Email = response.Email,
                    Id = response.Id,
                    Name = response.Name,
                    IsActive = response.IsActive
                };
            }
        }

        private void SetServerAccessToken(HttpRequestOptions options)
        {
            options.RequestHeaders.Add("X-Connect-Token", ConnectAccessKey);
        }
    }
}
