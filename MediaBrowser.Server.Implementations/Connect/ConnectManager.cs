using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Connect
{
    public class ConnectManager : IConnectManager
    {
        private readonly SemaphoreSlim _operationLock = new SemaphoreSlim(1, 1);

        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _json;
        private readonly IEncryptionManager _encryption;
        private readonly IHttpClient _httpClient;
        private readonly IServerApplicationHost _appHost;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;
        private readonly IProviderManager _providerManager;

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
            IServerConfigurationManager config, IUserManager userManager, IProviderManager providerManager)
        {
            _logger = logger;
            _appPaths = appPaths;
            _json = json;
            _encryption = encryption;
            _httpClient = httpClient;
            _appHost = appHost;
            _config = config;
            _userManager = userManager;
            _providerManager = providerManager;

            LoadCachedData();
        }

        internal void OnWanAddressResolved(string address)
        {
            DiscoveredWanIpAddress = address;

            UpdateConnectInfo();
        }

        private async void UpdateConnectInfo()
        {
            await _operationLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await UpdateConnectInfoInternal().ConfigureAwait(false);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private async Task UpdateConnectInfoInternal()
        {
            var wanApiAddress = WanApiAddress;

            if (string.IsNullOrWhiteSpace(wanApiAddress))
            {
                _logger.Warn("Cannot update Media Browser Connect information without a WanApiAddress");
                return;
            }

            try
            {
                var localAddress = _appHost.GetSystemInfo().LocalAddress;

                var hasExistingRecord = !string.IsNullOrWhiteSpace(ConnectServerId) &&
                                  !string.IsNullOrWhiteSpace(ConnectAccessKey);

                var createNewRegistration = !hasExistingRecord;

                if (hasExistingRecord)
                {
                    try
                    {
                        await UpdateServerRegistration(wanApiAddress, localAddress).ConfigureAwait(false);
                    }
                    catch (HttpException ex)
                    {
                        if (!ex.StatusCode.HasValue ||
                            !new[] { HttpStatusCode.NotFound, HttpStatusCode.Unauthorized }.Contains(ex.StatusCode.Value))
                        {
                            throw;
                        }

                        createNewRegistration = true;
                    }
                }

                if (createNewRegistration)
                {
                    await CreateServerRegistration(wanApiAddress, localAddress).ConfigureAwait(false);
                }

                await RefreshAuthorizationsInternal(true, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error registering with Connect", ex);
            }
        }

        private async Task CreateServerRegistration(string wanApiAddress, string localAddress)
        {
            var url = "Servers";
            url = GetConnectUrl(url);

            var postData = new Dictionary<string, string>
            {
                {"name", _appHost.FriendlyName}, 
                {"url", wanApiAddress}, 
                {"systemId", _appHost.SystemId}
            };

            if (!string.IsNullOrWhiteSpace(localAddress))
            {
                postData["localAddress"] = localAddress;
            }

            using (var stream = await _httpClient.Post(url, postData, CancellationToken.None).ConfigureAwait(false))
            {
                var data = _json.DeserializeFromStream<ServerRegistrationResponse>(stream);

                _data.ServerId = data.Id;
                _data.AccessKey = data.AccessKey;

                CacheData();
            }
        }

        private async Task UpdateServerRegistration(string wanApiAddress, string localAddress)
        {
            var url = "Servers";
            url = GetConnectUrl(url);
            url += "?id=" + ConnectServerId;

            var postData = new Dictionary<string, string>
            {
                {"name", _appHost.FriendlyName},
                {"url", wanApiAddress},
                {"systemId", _appHost.SystemId}
            };

            if (!string.IsNullOrWhiteSpace(localAddress))
            {
                postData["localAddress"] = localAddress;
            }

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None
            };

            options.SetPostData(postData);

            SetServerAccessToken(options);

            // No need to examine the response
            using (var stream = (await _httpClient.Post(options).ConfigureAwait(false)).Content)
            {
            }
        }

        private readonly object _dataFileLock = new object();
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

                lock (_dataFileLock)
                {
                    File.WriteAllText(path, encrypted, Encoding.UTF8);
                }
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
                lock (_dataFileLock)
                {
                    var encrypted = File.ReadAllText(path, Encoding.UTF8);

                    var json = _encryption.DecryptString(encrypted);

                    _data = _json.DeserializeFromString<ConnectData>(json);
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
            await _operationLock.WaitAsync().ConfigureAwait(false);

            try
            {
                return await LinkUserInternal(userId, connectUsername).ConfigureAwait(false);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private async Task<UserLinkResult> LinkUserInternal(string userId, string connectUsername)
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
                await RemoveConnect(user, connectUser.Id).ConfigureAwait(false);
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

            user.Configuration.SyncConnectImage = false;
            user.Configuration.SyncConnectName = false;
            _userManager.UpdateConfiguration(user, user.Configuration);

            await RefreshAuthorizationsInternal(false, CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        public async Task<UserLinkResult> InviteUser(string sendingUserId, string connectUsername)
        {
            await _operationLock.WaitAsync().ConfigureAwait(false);

            try
            {
                return await InviteUserInternal(sendingUserId, connectUsername).ConfigureAwait(false);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private async Task<UserLinkResult> InviteUserInternal(string sendingUserId, string connectUsername)
        {
            if (string.IsNullOrWhiteSpace(connectUsername))
            {
                throw new ArgumentNullException("connectUsername");
            }

            string connectUserId = null;
            var result = new UserLinkResult();

            try
            {
                var connectUser = await GetConnectUser(new ConnectUserQuery
                {
                    Name = connectUsername

                }, CancellationToken.None).ConfigureAwait(false);

                if (!connectUser.IsActive)
                {
                    throw new ArgumentException("The Media Browser account has been disabled.");
                }

                connectUserId = connectUser.Id;
                result.GuestDisplayName = connectUser.Name;
            }
            catch (HttpException ex)
            {
                if (!ex.StatusCode.HasValue ||
                    ex.StatusCode.Value != HttpStatusCode.NotFound ||
                    !Validator.EmailIsValid(connectUsername))
                {
                    throw;
                }
            }

            var sendingUser = GetUser(sendingUserId);
            var requesterUserName = sendingUser.ConnectUserName;

            if (string.IsNullOrWhiteSpace(requesterUserName))
            {
                requesterUserName = sendingUser.Name;
            }

            if (string.IsNullOrWhiteSpace(connectUserId))
            {
                return await SendNewUserInvitation(requesterUserName, connectUsername).ConfigureAwait(false);
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
                {"userId", connectUserId},
                {"userType", "Guest"},
                {"accessToken", accessToken},
                {"requesterUserName", requesterUserName}
            };

            options.SetPostData(postData);

            SetServerAccessToken(options);

            // No need to examine the response
            using (var stream = (await _httpClient.Post(options).ConfigureAwait(false)).Content)
            {
                var response = _json.DeserializeFromStream<ServerUserAuthorizationResponse>(stream);

                result.IsPending = string.Equals(response.AcceptStatus, "waiting", StringComparison.OrdinalIgnoreCase);
            }

            await RefreshAuthorizationsInternal(false, CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        private async Task<UserLinkResult> SendNewUserInvitation(string fromName, string email)
        {
            var url = GetConnectUrl("users/invite");

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None
            };

            var postData = new Dictionary<string, string>
            {
                {"email", email},
                {"requesterUserName", fromName}
            };

            options.SetPostData(postData);

            // No need to examine the response
            using (var stream = (await _httpClient.Post(options).ConfigureAwait(false)).Content)
            {
            }
            
            return new UserLinkResult
            {
                IsNewUserInvitation = true,
                GuestDisplayName = email
            };
        }

        public Task RemoveConnect(string userId)
        {
            var user = GetUser(userId);

            return RemoveConnect(user, user.ConnectUserId);
        }

        private async Task RemoveConnect(User user, string connectUserId)
        {
            if (!string.IsNullOrWhiteSpace(connectUserId))
            {
                await CancelAuthorizationByConnectUserId(connectUserId).ConfigureAwait(false);
            }

            user.ConnectAccessKey = null;
            user.ConnectUserName = null;
            user.ConnectUserId = null;
            user.ConnectLinkType = null;

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
                    IsActive = response.IsActive,
                    ImageUrl = response.ImageUrl
                };
            }
        }

        private void SetServerAccessToken(HttpRequestOptions options)
        {
            options.RequestHeaders.Add("X-Connect-Token", ConnectAccessKey);
        }

        public async Task RefreshAuthorizations(CancellationToken cancellationToken)
        {
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await RefreshAuthorizationsInternal(true, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private async Task RefreshAuthorizationsInternal(bool refreshImages, CancellationToken cancellationToken)
        {
            var url = GetConnectUrl("ServerAuthorizations");

            url += "?serverId=" + ConnectServerId;

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken
            };

            SetServerAccessToken(options);

            try
            {
                using (var stream = (await _httpClient.SendAsync(options, "GET").ConfigureAwait(false)).Content)
                {
                    var list = _json.DeserializeFromStream<List<ServerUserAuthorizationResponse>>(stream);

                    await RefreshAuthorizations(list, refreshImages).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error refreshing server authorizations.", ex);
            }
        }

        private readonly SemaphoreSlim _connectImageSemaphore = new SemaphoreSlim(5, 5);
        private async Task RefreshAuthorizations(List<ServerUserAuthorizationResponse> list, bool refreshImages)
        {
            var users = _userManager.Users.ToList();

            // Handle existing authorizations that were removed by the Connect server
            // Handle existing authorizations whose status may have been updated
            foreach (var user in users)
            {
                if (!string.IsNullOrWhiteSpace(user.ConnectUserId))
                {
                    var connectEntry = list.FirstOrDefault(i => string.Equals(i.UserId, user.ConnectUserId, StringComparison.OrdinalIgnoreCase));

                    if (connectEntry == null)
                    {
                        var deleteUser = user.ConnectLinkType.HasValue &&
                                         user.ConnectLinkType.Value == UserLinkType.Guest;

                        user.ConnectUserId = null;
                        user.ConnectAccessKey = null;
                        user.ConnectUserName = null;
                        user.ConnectLinkType = null;

                        await _userManager.UpdateUser(user).ConfigureAwait(false);

                        if (deleteUser)
                        {
                            _logger.Debug("Deleting guest user {0}", user.Name);
                            await _userManager.DeleteUser(user).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        var changed = !string.Equals(user.ConnectAccessKey, connectEntry.AccessToken, StringComparison.OrdinalIgnoreCase);

                        if (changed)
                        {
                            user.ConnectUserId = connectEntry.UserId;
                            user.ConnectAccessKey = connectEntry.AccessToken;

                            await _userManager.UpdateUser(user).ConfigureAwait(false);
                        }
                    }
                }
            }

            var pending = new List<ConnectAuthorization>();

            foreach (var connectEntry in list)
            {
                if (string.Equals(connectEntry.UserType, "guest", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(connectEntry.AcceptStatus, "accepted", StringComparison.OrdinalIgnoreCase))
                    {
                        var user = _userManager.Users
                            .FirstOrDefault(i => string.Equals(i.ConnectUserId, connectEntry.UserId, StringComparison.OrdinalIgnoreCase));

                        if (user == null)
                        {
                            // Add user
                            user = await _userManager.CreateUser(connectEntry.UserName).ConfigureAwait(false);

                            user.ConnectUserName = connectEntry.UserName;
                            user.ConnectUserId = connectEntry.UserId;
                            user.ConnectLinkType = UserLinkType.Guest;
                            user.ConnectAccessKey = connectEntry.AccessToken;

                            await _userManager.UpdateUser(user).ConfigureAwait(false);

                            user.Configuration.SyncConnectImage = true;
                            user.Configuration.SyncConnectName = true;
                            user.Configuration.IsHidden = true;
                            user.Configuration.EnableLiveTvManagement = false;
                            user.Configuration.IsAdministrator = false;

                            _userManager.UpdateConfiguration(user, user.Configuration);
                        }
                    }
                    else if (string.Equals(connectEntry.AcceptStatus, "waiting", StringComparison.OrdinalIgnoreCase))
                    {
                        pending.Add(new ConnectAuthorization
                        {
                            ConnectUserId = connectEntry.UserId,
                            ImageUrl = connectEntry.UserImageUrl,
                            UserName = connectEntry.UserName,
                            Id = connectEntry.Id
                        });
                    }
                }
            }

            _data.PendingAuthorizations = pending;
            CacheData();

            await RefreshGuestNames(list, refreshImages).ConfigureAwait(false);
        }

        private async Task RefreshGuestNames(List<ServerUserAuthorizationResponse> list, bool refreshImages)
        {
            var users = _userManager.Users
                .Where(i => !string.IsNullOrEmpty(i.ConnectUserId) &&
                    (i.Configuration.SyncConnectImage || i.Configuration.SyncConnectName))
                    .ToList();

            foreach (var user in users)
            {
                var authorization = list.FirstOrDefault(i => string.Equals(i.UserId, user.ConnectUserId, StringComparison.Ordinal));

                if (authorization == null)
                {
                    _logger.Warn("Unable to find connect authorization record for user {0}", user.Name);
                    continue;
                }

                if (user.Configuration.SyncConnectName)
                {
                    var changed = !string.Equals(authorization.UserName, user.Name, StringComparison.OrdinalIgnoreCase);

                    if (changed)
                    {
                        await user.Rename(authorization.UserName).ConfigureAwait(false);
                    }
                }

                if (user.Configuration.SyncConnectImage)
                {
                    var imageUrl = authorization.UserImageUrl;

                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        var changed = false;

                        if (!user.HasImage(ImageType.Primary))
                        {
                            changed = true;
                        }
                        else if (refreshImages)
                        {
                            using (var response = await _httpClient.SendAsync(new HttpRequestOptions
                            {
                                Url = imageUrl,
                                BufferContent = false

                            }, "HEAD").ConfigureAwait(false))
                            {
                                var length = response.ContentLength;

                                if (length != new FileInfo(user.GetImageInfo(ImageType.Primary, 0).Path).Length)
                                {
                                    changed = true;
                                }
                            }
                        }

                        if (changed)
                        {
                            await _providerManager.SaveImage(user, imageUrl, _connectImageSemaphore, ImageType.Primary, null, CancellationToken.None).ConfigureAwait(false);

                            await user.RefreshMetadata(new MetadataRefreshOptions
                            {
                                ForceSave = true,

                            }, CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        public async Task<List<ConnectAuthorization>> GetPendingGuests()
        {
            var time = DateTime.UtcNow - _data.LastAuthorizationsRefresh;

            if (time.TotalMinutes >= 5)
            {
                await _operationLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

                try
                {
                    await RefreshAuthorizationsInternal(false, CancellationToken.None).ConfigureAwait(false);

                    _data.LastAuthorizationsRefresh = DateTime.UtcNow;
                    CacheData();
                }
                finally
                {
                    _operationLock.Release();
                }
            }

            return _data.PendingAuthorizations.ToList();
        }

        public async Task CancelAuthorization(string id)
        {
            await _operationLock.WaitAsync().ConfigureAwait(false);

            try
            {
                await CancelAuthorizationInternal(id).ConfigureAwait(false);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private async Task CancelAuthorizationInternal(string id)
        {
            var connectUserId = _data.PendingAuthorizations
                .First(i => string.Equals(i.Id, id, StringComparison.Ordinal))
                .ConnectUserId;

            await CancelAuthorizationByConnectUserId(connectUserId).ConfigureAwait(false);

            await RefreshAuthorizationsInternal(false, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task CancelAuthorizationByConnectUserId(string connectUserId)
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

        public async Task Authenticate(string username, string passwordMd5)
        {
            var request = new HttpRequestOptions
            {
                Url = GetConnectUrl("user/authenticate")
            };

            request.SetPostData(new Dictionary<string, string>
                {
                    {"userName",username},
                    {"password",passwordMd5}
                });

            // No need to examine the response
            using (var stream = (await _httpClient.SendAsync(request, "POST").ConfigureAwait(false)).Content)
            {
            }
        }
    }
}
