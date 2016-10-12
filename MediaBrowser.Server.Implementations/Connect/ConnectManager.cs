using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
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
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.Extensions;

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
        private readonly ISecurityManager _securityManager;
        private readonly IFileSystem _fileSystem;

        private ConnectData _data = new ConnectData();

        public string ConnectServerId
        {
            get { return _data.ServerId; }
        }
        public string ConnectAccessKey
        {
            get { return _data.AccessKey; }
        }

        private IPAddress DiscoveredWanIpAddress { get; set; }

        public string WanIpAddress
        {
            get
            {
                var address = _config.Configuration.WanDdns;

                if (!string.IsNullOrWhiteSpace(address))
                {
                    Uri newUri;

                    if (Uri.TryCreate(address, UriKind.Absolute, out newUri))
                    {
                        address = newUri.Host;
                    }
                }

                if (string.IsNullOrWhiteSpace(address) && DiscoveredWanIpAddress != null)
                {
                    if (DiscoveredWanIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        address = "[" + DiscoveredWanIpAddress + "]";
                    }
                    else
                    {
                        address = DiscoveredWanIpAddress.ToString();
                    }
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
                        ip = (_appHost.EnableHttps ? "https://" : "http://") + ip;
                    }

                    ip += ":";
                    ip += _appHost.EnableHttps ? _config.Configuration.PublicHttpsPort.ToString(CultureInfo.InvariantCulture) : _config.Configuration.PublicPort.ToString(CultureInfo.InvariantCulture);

                    return ip;
                }

                return null;
            }
        }

        private string XApplicationValue
        {
            get { return _appHost.Name + "/" + _appHost.ApplicationVersion; }
        }

        public ConnectManager(ILogger logger,
            IApplicationPaths appPaths,
            IJsonSerializer json,
            IEncryptionManager encryption,
            IHttpClient httpClient,
            IServerApplicationHost appHost,
            IServerConfigurationManager config, IUserManager userManager, IProviderManager providerManager, ISecurityManager securityManager, IFileSystem fileSystem)
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
            _securityManager = securityManager;
            _fileSystem = fileSystem;

            LoadCachedData();
        }

        internal void Start()
        {
            _config.ConfigurationUpdated += _config_ConfigurationUpdated;
        }

        internal void OnWanAddressResolved(IPAddress address)
        {
            DiscoveredWanIpAddress = address;

            var task = UpdateConnectInfo();
        }

        private async Task UpdateConnectInfo()
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
                _logger.Warn("Cannot update Emby Connect information without a WanApiAddress");
                return;
            }

            try
            {
                var localAddress = await _appHost.GetLocalApiUrl().ConfigureAwait(false);

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
                        if (!ex.StatusCode.HasValue || !new[] { HttpStatusCode.NotFound, HttpStatusCode.Unauthorized }.Contains(ex.StatusCode.Value))
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

                _lastReportedIdentifier = GetConnectReportingIdentifier(localAddress, wanApiAddress);

                await RefreshAuthorizationsInternal(true, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error registering with Connect", ex);
            }
        }

        private string _lastReportedIdentifier;
        private async Task<string> GetConnectReportingIdentifier()
        {
            var url = await _appHost.GetLocalApiUrl().ConfigureAwait(false);
            return GetConnectReportingIdentifier(url, WanApiAddress);
        }
        private string GetConnectReportingIdentifier(string localAddress, string remoteAddress)
        {
            return (remoteAddress ?? string.Empty) + (localAddress ?? string.Empty);
        }

        async void _config_ConfigurationUpdated(object sender, EventArgs e)
        {
            // If info hasn't changed, don't report anything
            var connectIdentifier = await GetConnectReportingIdentifier().ConfigureAwait(false);
            if (string.Equals(_lastReportedIdentifier, connectIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await UpdateConnectInfo().ConfigureAwait(false);
        }

        private async Task CreateServerRegistration(string wanApiAddress, string localAddress)
        {
            if (string.IsNullOrWhiteSpace(wanApiAddress))
            {
                throw new ArgumentNullException("wanApiAddress");
            }

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

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None,
                BufferContent = false
            };

            options.SetPostData(postData);
            SetApplicationHeader(options);

            using (var response = await _httpClient.Post(options).ConfigureAwait(false))
            {
                var data = _json.DeserializeFromStream<ServerRegistrationResponse>(response.Content);

                _data.ServerId = data.Id;
                _data.AccessKey = data.AccessKey;

                CacheData();
            }
        }

        private async Task UpdateServerRegistration(string wanApiAddress, string localAddress)
        {
            if (string.IsNullOrWhiteSpace(wanApiAddress))
            {
                throw new ArgumentNullException("wanApiAddress");
            }

            if (string.IsNullOrWhiteSpace(ConnectServerId))
            {
                throw new ArgumentNullException("ConnectServerId");
            }

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
                CancellationToken = CancellationToken.None,
                BufferContent = false
            };

            options.SetPostData(postData);

            SetServerAccessToken(options);
            SetApplicationHeader(options);

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
                _fileSystem.CreateDirectory(Path.GetDirectoryName(path));

                var json = _json.SerializeToString(_data);

                var encrypted = _encryption.EncryptString(json);

                lock (_dataFileLock)
                {
                    _fileSystem.WriteAllText(path, encrypted, Encoding.UTF8);
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

            _logger.Info("Loading data from {0}", path);

            try
            {
                lock (_dataFileLock)
                {
                    var encrypted = _fileSystem.ReadAllText(path, Encoding.UTF8);

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
            return "https://connect.emby.media/service/" + handler;
        }

        public async Task<UserLinkResult> LinkUser(string userId, string connectUsername)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrWhiteSpace(connectUsername))
            {
                throw new ArgumentNullException("connectUsername");
            }
            if (string.IsNullOrWhiteSpace(ConnectServerId))
            {
                await UpdateConnectInfo().ConfigureAwait(false);
            }

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
            if (string.IsNullOrWhiteSpace(ConnectServerId))
            {
                throw new ArgumentNullException("ConnectServerId");
            }

            var connectUser = await GetConnectUser(new ConnectUserQuery
            {
                NameOrEmail = connectUsername

            }, CancellationToken.None).ConfigureAwait(false);

            if (!connectUser.IsActive)
            {
                throw new ArgumentException("The Emby account has been disabled.");
            }

            var existingUser = _userManager.Users.FirstOrDefault(i => string.Equals(i.ConnectUserId, connectUser.Id) && !string.IsNullOrWhiteSpace(i.ConnectAccessKey));
            if (existingUser != null)
            {
                throw new InvalidOperationException("This connect user is already linked to local user " + existingUser.Name);
            }

            var user = GetUser(userId);

            if (!string.IsNullOrWhiteSpace(user.ConnectUserId))
            {
                await RemoveConnect(user, user.ConnectUserId).ConfigureAwait(false);
            }

            var url = GetConnectUrl("ServerAuthorizations");

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None,
                BufferContent = false
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
            SetApplicationHeader(options);

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

            await _userManager.UpdateConfiguration(user.Id.ToString("N"), user.Configuration);

            await RefreshAuthorizationsInternal(false, CancellationToken.None).ConfigureAwait(false);

            return result;
        }

        public async Task<UserLinkResult> InviteUser(ConnectAuthorizationRequest request)
        {
            if (string.IsNullOrWhiteSpace(ConnectServerId))
            {
                await UpdateConnectInfo().ConfigureAwait(false);
            }

            await _operationLock.WaitAsync().ConfigureAwait(false);

            try
            {
                return await InviteUserInternal(request).ConfigureAwait(false);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        private async Task<UserLinkResult> InviteUserInternal(ConnectAuthorizationRequest request)
        {
            var connectUsername = request.ConnectUserName;
            var sendingUserId = request.SendingUserId;

            if (string.IsNullOrWhiteSpace(connectUsername))
            {
                throw new ArgumentNullException("connectUsername");
            }
            if (string.IsNullOrWhiteSpace(ConnectServerId))
            {
                throw new ArgumentNullException("ConnectServerId");
            }

            var sendingUser = GetUser(sendingUserId);
            var requesterUserName = sendingUser.ConnectUserName;

            if (string.IsNullOrWhiteSpace(requesterUserName))
            {
                throw new ArgumentException("A Connect account is required in order to send invitations.");
            }

            string connectUserId = null;
            var result = new UserLinkResult();

            try
            {
                var connectUser = await GetConnectUser(new ConnectUserQuery
                {
                    NameOrEmail = connectUsername

                }, CancellationToken.None).ConfigureAwait(false);

                if (!connectUser.IsActive)
                {
                    throw new ArgumentException("The Emby account is not active. Please ensure the account has been activated by following the instructions within the email confirmation.");
                }

                connectUserId = connectUser.Id;
                result.GuestDisplayName = connectUser.Name;
            }
            catch (HttpException ex)
            {
                if (!ex.StatusCode.HasValue)
                {
                    throw;
                }

                // If they entered a username, then whatever the error is just throw it, for example, user not found
                if (!Validator.EmailIsValid(connectUsername))
                {
                    if (ex.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        throw new ResourceNotFoundException();
                    }
                    throw;
                }

                if (ex.StatusCode.Value != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            if (string.IsNullOrWhiteSpace(connectUserId))
            {
                return await SendNewUserInvitation(requesterUserName, connectUsername).ConfigureAwait(false);
            }

            var url = GetConnectUrl("ServerAuthorizations");

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None,
                BufferContent = false
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
            SetApplicationHeader(options);

            // No need to examine the response
            using (var stream = (await _httpClient.Post(options).ConfigureAwait(false)).Content)
            {
                var response = _json.DeserializeFromStream<ServerUserAuthorizationResponse>(stream);

                result.IsPending = string.Equals(response.AcceptStatus, "waiting", StringComparison.OrdinalIgnoreCase);

                _data.PendingAuthorizations.Add(new ConnectAuthorizationInternal
                {
                    ConnectUserId = response.UserId,
                    Id = response.Id,
                    ImageUrl = response.UserImageUrl,
                    UserName = response.UserName,
                    EnabledLibraries = request.EnabledLibraries,
                    EnabledChannels = request.EnabledChannels,
                    EnableLiveTv = request.EnableLiveTv,
                    AccessToken = accessToken
                });

                CacheData();
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
                CancellationToken = CancellationToken.None,
                BufferContent = false
            };

            var postData = new Dictionary<string, string>
            {
                {"email", email},
                {"requesterUserName", fromName}
            };

            options.SetPostData(postData);
            SetApplicationHeader(options);

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
            else if (!string.IsNullOrWhiteSpace(query.NameOrEmail))
            {
                url = url + "?nameOrEmail=" + WebUtility.UrlEncode(query.NameOrEmail);
            }
            else if (!string.IsNullOrWhiteSpace(query.Name))
            {
                url = url + "?name=" + WebUtility.UrlEncode(query.Name);
            }
            else if (!string.IsNullOrWhiteSpace(query.Email))
            {
                url = url + "?name=" + WebUtility.UrlEncode(query.Email);
            }
            else
            {
                throw new ArgumentException("Empty ConnectUserQuery supplied");
            }

            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                BufferContent = false
            };

            SetServerAccessToken(options);
            SetApplicationHeader(options);

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

        private void SetApplicationHeader(HttpRequestOptions options)
        {
            options.RequestHeaders.Add("X-Application", XApplicationValue);
        }

        private void SetServerAccessToken(HttpRequestOptions options)
        {
            if (string.IsNullOrWhiteSpace(ConnectAccessKey))
            {
                throw new ArgumentNullException("ConnectAccessKey");
            }

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
            if (string.IsNullOrWhiteSpace(ConnectServerId))
            {
                throw new ArgumentNullException("ConnectServerId");
            }

            var url = GetConnectUrl("ServerAuthorizations");

            url += "?serverId=" + ConnectServerId;

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                BufferContent = false
            };

            SetServerAccessToken(options);
            SetApplicationHeader(options);

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

            var currentPendingList = _data.PendingAuthorizations.ToList();
            var newPendingList = new List<ConnectAuthorizationInternal>();

            foreach (var connectEntry in list)
            {
                if (string.Equals(connectEntry.UserType, "guest", StringComparison.OrdinalIgnoreCase))
                {
                    var currentPendingEntry = currentPendingList.FirstOrDefault(i => string.Equals(i.Id, connectEntry.Id, StringComparison.OrdinalIgnoreCase));

                    if (string.Equals(connectEntry.AcceptStatus, "accepted", StringComparison.OrdinalIgnoreCase))
                    {
                        var user = _userManager.Users
                            .FirstOrDefault(i => string.Equals(i.ConnectUserId, connectEntry.UserId, StringComparison.OrdinalIgnoreCase));

                        if (user == null)
                        {
                            // Add user
                            user = await _userManager.CreateUser(_userManager.MakeValidUsername(connectEntry.UserName)).ConfigureAwait(false);

                            user.ConnectUserName = connectEntry.UserName;
                            user.ConnectUserId = connectEntry.UserId;
                            user.ConnectLinkType = UserLinkType.Guest;
                            user.ConnectAccessKey = connectEntry.AccessToken;

                            await _userManager.UpdateUser(user).ConfigureAwait(false);

                            user.Policy.IsHidden = true;
                            user.Policy.EnableLiveTvManagement = false;
                            user.Policy.EnableContentDeletion = false;
                            user.Policy.EnableRemoteControlOfOtherUsers = false;
                            user.Policy.EnableSharedDeviceControl = false;
                            user.Policy.IsAdministrator = false;

                            if (currentPendingEntry != null)
                            {
                                user.Policy.EnabledFolders = currentPendingEntry.EnabledLibraries;
                                user.Policy.EnableAllFolders = false;

                                user.Policy.EnabledChannels = currentPendingEntry.EnabledChannels;
                                user.Policy.EnableAllChannels = false;

                                user.Policy.EnableLiveTvAccess = currentPendingEntry.EnableLiveTv;
                            }

                            await _userManager.UpdateConfiguration(user.Id.ToString("N"), user.Configuration);
                        }
                    }
                    else if (string.Equals(connectEntry.AcceptStatus, "waiting", StringComparison.OrdinalIgnoreCase))
                    {
                        currentPendingEntry = currentPendingEntry ?? new ConnectAuthorizationInternal();

                        currentPendingEntry.ConnectUserId = connectEntry.UserId;
                        currentPendingEntry.ImageUrl = connectEntry.UserImageUrl;
                        currentPendingEntry.UserName = connectEntry.UserName;
                        currentPendingEntry.Id = connectEntry.Id;
                        currentPendingEntry.AccessToken = connectEntry.AccessToken;

                        newPendingList.Add(currentPendingEntry);
                    }
                }
            }

            _data.PendingAuthorizations = newPendingList;
            CacheData();

            await RefreshGuestNames(list, refreshImages).ConfigureAwait(false);
        }

        private async Task RefreshGuestNames(List<ServerUserAuthorizationResponse> list, bool refreshImages)
        {
            var users = _userManager.Users
                .Where(i => !string.IsNullOrEmpty(i.ConnectUserId) && i.ConnectLinkType.HasValue && i.ConnectLinkType.Value == UserLinkType.Guest)
                    .ToList();

            foreach (var user in users)
            {
                var authorization = list.FirstOrDefault(i => string.Equals(i.UserId, user.ConnectUserId, StringComparison.Ordinal));

                if (authorization == null)
                {
                    _logger.Warn("Unable to find connect authorization record for user {0}", user.Name);
                    continue;
                }

                var syncConnectName = true;
                var syncConnectImage = true;

                if (syncConnectName)
                {
                    var changed = !string.Equals(authorization.UserName, user.Name, StringComparison.OrdinalIgnoreCase);

                    if (changed)
                    {
                        await user.Rename(authorization.UserName).ConfigureAwait(false);
                    }
                }

                if (syncConnectImage)
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

                                if (length != _fileSystem.GetFileInfo(user.GetImageInfo(ImageType.Primary, 0).Path).Length)
                                {
                                    changed = true;
                                }
                            }
                        }

                        if (changed)
                        {
                            await _providerManager.SaveImage(user, imageUrl, _connectImageSemaphore, ImageType.Primary, null, CancellationToken.None).ConfigureAwait(false);

                            await user.RefreshMetadata(new MetadataRefreshOptions(_fileSystem)
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
                catch (Exception ex)
                {
                    _logger.ErrorException("Error refreshing authorization", ex);
                }
                finally
                {
                    _operationLock.Release();
                }
            }

            return _data.PendingAuthorizations.Select(i => new ConnectAuthorization
            {
                ConnectUserId = i.ConnectUserId,
                EnableLiveTv = i.EnableLiveTv,
                EnabledChannels = i.EnabledChannels,
                EnabledLibraries = i.EnabledLibraries,
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                UserName = i.UserName

            }).ToList();
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
            if (string.IsNullOrWhiteSpace(connectUserId))
            {
                throw new ArgumentNullException("connectUserId");
            }
            if (string.IsNullOrWhiteSpace(ConnectServerId))
            {
                throw new ArgumentNullException("ConnectServerId");
            }

            var url = GetConnectUrl("ServerAuthorizations");

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = CancellationToken.None,
                BufferContent = false
            };

            var postData = new Dictionary<string, string>
                {
                    {"serverId", ConnectServerId},
                    {"userId", connectUserId}
                };

            options.SetPostData(postData);

            SetServerAccessToken(options);
            SetApplicationHeader(options);

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
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException("username");
            }

            if (string.IsNullOrWhiteSpace(passwordMd5))
            {
                throw new ArgumentNullException("passwordMd5");
            }

            var options = new HttpRequestOptions
            {
                Url = GetConnectUrl("user/authenticate"),
                BufferContent = false
            };

            options.SetPostData(new Dictionary<string, string>
                {
                    {"userName",username},
                    {"password",passwordMd5}
                });

            SetApplicationHeader(options);

            // No need to examine the response
            using (var response = (await _httpClient.SendAsync(options, "POST").ConfigureAwait(false)).Content)
            {
            }
        }

        public async Task<User> GetLocalUser(string connectUserId)
        {
            var user = _userManager.Users
                .FirstOrDefault(i => string.Equals(i.ConnectUserId, connectUserId, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                await RefreshAuthorizations(CancellationToken.None).ConfigureAwait(false);
            }

            return _userManager.Users
                .FirstOrDefault(i => string.Equals(i.ConnectUserId, connectUserId, StringComparison.OrdinalIgnoreCase));
        }

        public User GetUserFromExchangeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException("token");
            }

            return _userManager.Users.FirstOrDefault(u => string.Equals(token, u.ConnectAccessKey, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsAuthorizationTokenValid(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException("token");
            }

            return _userManager.Users.Any(u => string.Equals(token, u.ConnectAccessKey, StringComparison.OrdinalIgnoreCase)) ||
                _data.PendingAuthorizations.Select(i => i.AccessToken).Contains(token, StringComparer.OrdinalIgnoreCase);
        }
    }
}
