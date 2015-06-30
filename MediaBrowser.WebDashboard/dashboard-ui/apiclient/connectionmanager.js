(function (globalScope) {

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.ConnectionState = {
        Unavailable: 0,
        ServerSelection: 1,
        ServerSignIn: 2,
        SignedIn: 3,
        ConnectSignIn: 4
    };

    globalScope.MediaBrowser.ConnectionMode = {
        Local: 0,
        Remote: 1,
        Manual: 2
    };

    globalScope.MediaBrowser.ServerInfo = {

        getServerAddress: function (server, mode) {

            switch (mode) {
                case MediaBrowser.ConnectionMode.Local:
                    return server.LocalAddress;
                case MediaBrowser.ConnectionMode.Manual:
                    return server.ManualAddress;
                case MediaBrowser.ConnectionMode.Remote:
                    return server.RemoteAddress;
                default:
                    return server.ManualAddress || server.LocalAddress || server.RemoteAddress;
            }
        }
    };

    globalScope.MediaBrowser.ConnectionManager = function (logger, credentialProvider, appName, appVersion, deviceName, deviceId, capabilities) {

        logger.log('Begin MediaBrowser.ConnectionManager constructor');

        var self = this;
        var apiClients = [];
        var defaultTimeout = 15000;

        function mergeServers(list1, list2) {

            for (var i = 0, length = list2.length; i < length; i++) {
                credentialProvider.addOrUpdateServer(list1, list2[i]);
            }

            return list1;
        }

        function resolveWithFailure(deferred) {

            deferred.resolveWith(null, [
            {
                State: MediaBrowser.ConnectionState.Unavailable,
                ConnectUser: self.connectUser()
            }]);
        }

        function updateServerInfo(server, systemInfo) {

            server.Name = systemInfo.ServerName;
            server.Id = systemInfo.Id;

            if (systemInfo.LocalAddress) {
                server.LocalAddress = systemInfo.LocalAddress;
            }
            if (systemInfo.WanAddress) {
                server.RemoteAddress = systemInfo.WanAddress;
            }
            if (systemInfo.MacAddress) {
                server.WakeOnLanInfos = [
                        { MacAddress: systemInfo.MacAddress }
                ];
            }
        }

        function tryConnect(url, timeout) {

            url += "/system/info/public";

            logger.log('tryConnect url: ' + url);

            return HttpClient.send({

                type: "GET",
                url: url,
                dataType: "json",

                timeout: timeout || defaultTimeout

            });
        }

        var connectUser;
        self.connectUser = function () {
            return connectUser;
        };

        self.appVersion = function () {
            return appVersion;
        };

        self.capabilities = function () {
            return capabilities;
        };

        self.deviceId = function () {
            return deviceId;
        };

        self.credentialProvider = function () {
            return credentialProvider;
        };

        self.connectUserId = function () {
            return credentialProvider.credentials().ConnectUserId;
        };

        self.connectToken = function () {

            return credentialProvider.credentials().ConnectAccessToken;
        };

        self.getLastUsedServer = function () {

            var servers = credentialProvider.credentials().Servers;

            servers.sort(function (a, b) {
                return (b.DateLastAccessed || 0) - (a.DateLastAccessed || 0);
            });

            if (!servers.length) {
                return null;
            }

            return servers[0];
        };

        self.getLastUsedApiClient = function () {

            var servers = credentialProvider.credentials().Servers;

            servers.sort(function (a, b) {
                return (b.DateLastAccessed || 0) - (a.DateLastAccessed || 0);
            });

            if (!servers.length) {
                return null;
            }

            var server = servers[0];

            return getOrAddApiClient(server, server.LastConnectionMode);
        };

        self.addApiClient = function (apiClient) {

            apiClients.push(apiClient);

            var existingServers = credentialProvider.credentials().Servers.filter(function (s) {

                return stringEqualsIgnoreCase(s.ManualAddress, apiClient.serverAddress()) ||
                    stringEqualsIgnoreCase(s.LocalAddress, apiClient.serverAddress()) ||
                    stringEqualsIgnoreCase(s.RemoteAddress, apiClient.serverAddress());

            });

            var existingServer = existingServers.length ? existingServers[0] : {};
            existingServer.DateLastAccessed = new Date().getTime();
            existingServer.LastConnectionMode = MediaBrowser.ConnectionMode.Manual;
            existingServer.ManualAddress = apiClient.serverAddress();
            apiClient.serverInfo(existingServer);

            Events.on(apiClient, 'authenticated', function (e, result) {
                onAuthenticated(this, result, {}, true);
            });

            if (!existingServers.length) {
                var credentials = credentialProvider.credentials();
                credentials.Servers = [existingServer];
                credentialProvider.credentials(credentials);
            }

            Events.trigger(self, 'apiclientcreated', [apiClient]);

            if (existingServer.Id) {
                return;
            }

            apiClient.getPublicSystemInfo().done(function (systemInfo) {

                var credentials = credentialProvider.credentials();
                existingServer.Id = systemInfo.Id;
                apiClient.serverInfo(existingServer);

                credentials.Servers = [existingServer];
                credentialProvider.credentials(credentials);
            });
        };

        self.clearData = function () {

            logger.log('connection manager clearing data');

            connectUser = null;
            var credentials = credentialProvider.credentials();
            credentials.ConnectAccessToken = null;
            credentials.ConnectUserId = null;
            credentials.Servers = [];
            credentialProvider.credentials(credentials);
        };

        function onConnectUserSignIn(user) {

            connectUser = user;
            Events.trigger(self, 'connectusersignedin', [user]);
        }

        function getOrAddApiClient(server, connectionMode) {

            var apiClient = self.getApiClient(server.Id);

            if (!apiClient) {

                var url = MediaBrowser.ServerInfo.getServerAddress(server, connectionMode);

                apiClient = new MediaBrowser.ApiClient(logger, url, appName, appVersion, deviceName, deviceId);

                apiClients.push(apiClient);

                apiClient.serverInfo(server);

                Events.on(apiClient, 'authenticated', function (e, result) {
                    onAuthenticated(this, result, {}, true);
                });

                Events.trigger(self, 'apiclientcreated', [apiClient]);
            }

            if (server.AccessToken && server.UserId) {

                apiClient.setAuthenticationInfo(server.AccessToken, server.UserId);
            }
            else {

                apiClient.clearAuthenticationInfo();
            }

            logger.log('returning instance from getOrAddApiClient');
            return apiClient;
        }

        self.getOrCreateApiClient = function (serverId) {

            var apiClient = self.getApiClient(serverId);

            if (apiClient) {
                return apiClient;
            }

            var credentials = credentialProvider.credentials();
            var servers = credentials.Servers.filter(function (s) {
                return stringEqualsIgnoreCase(s.Id, serverId);

            });

            if (!servers.length) {
                throw new Error('Server not found: ' + serverId);
            }

            var server = servers[0];

            return getOrAddApiClient(server, server.LastConnectionMode);
        };

        function onAuthenticated(apiClient, result, options, saveCredentials) {

            var credentials = credentialProvider.credentials();
            var servers = credentials.Servers.filter(function (s) {
                return s.Id == result.ServerId;
            });

            var server = servers.length ? servers[0] : apiClient.serverInfo();

            server.DateLastAccessed = new Date().getTime();
            server.Id = result.ServerId;

            if (saveCredentials) {
                server.UserId = result.User.Id;
                server.AccessToken = result.AccessToken;
            } else {
                server.UserId = null;
                server.AccessToken = null;
            }

            credentialProvider.addOrUpdateServer(credentials.Servers, server);
            saveUserInfoIntoCredentials(server, result.User);
            credentialProvider.credentials(credentials);

            afterConnected(apiClient, options);

            onLocalUserSignIn(result.User);
        }

        function saveUserInfoIntoCredentials(server, user) {

            //ServerUserInfo info = new ServerUserInfo();
            //info.setIsSignedInOffline(true);
            //info.setId(user.getId());

            //// Record user info here
            //server.AddOrUpdate(info);
        }

        function afterConnected(apiClient, options) {

            options = options || {};

            if (options.reportCapabilities !== false) {
                apiClient.reportCapabilities(capabilities);
            }

            if (options.enableWebSocket !== false) {
                if (!apiClient.isWebSocketOpenOrConnecting && apiClient.isWebSocketSupported()) {
                    logger.log('calling apiClient.openWebSocket');

                    apiClient.openWebSocket();
                }
            }
        }

        function onLocalUserSignIn(user) {

            Events.trigger(self, 'localusersignedin', [user]);
        }

        function ensureConnectUser(credentials) {

            var deferred = DeferredBuilder.Deferred();

            if (connectUser && connectUser.Id == credentials.ConnectUserId) {
                deferred.resolveWith(null, [[]]);
            }

            else if (credentials.ConnectUserId && credentials.ConnectAccessToken) {

                connectUser = null;

                getConnectUser(credentials.ConnectUserId, credentials.ConnectAccessToken).done(function (user) {

                    onConnectUserSignIn(user);
                    deferred.resolveWith(null, [[]]);

                }).fail(function () {
                    deferred.resolveWith(null, [[]]);
                });

            } else {
                deferred.resolveWith(null, [[]]);
            }

            return deferred.promise();
        }

        function getConnectUser(userId, accessToken) {

            if (!userId) {
                throw new Error("null userId");
            }
            if (!accessToken) {
                throw new Error("null accessToken");
            }

            var url = "https://connect.mediabrowser.tv/service/user?id=" + userId;

            return HttpClient.send({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-Application": appName + "/" + appVersion,
                    "X-Connect-UserToken": accessToken
                }

            });
        }

        function addAuthenticationInfoFromConnect(server, connectionMode, credentials) {

            if (!server.ExchangeToken) {
                throw new Error("server.ExchangeToken cannot be null");
            }
            if (!credentials.ConnectUserId) {
                throw new Error("credentials.ConnectUserId cannot be null");
            }

            var url = MediaBrowser.ServerInfo.getServerAddress(server, connectionMode);

            url += "/Connect/Exchange?format=json&ConnectUserId=" + credentials.ConnectUserId;

            return HttpClient.send({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-MediaBrowser-Token": server.ExchangeToken
                }

            }).done(function (auth) {

                server.UserId = auth.LocalUserId;
                server.AccessToken = auth.AccessToken;

            }).fail(function () {

                server.UserId = null;
                server.AccessToken = null;
            });
        }

        function validateAuthentication(server, connectionMode) {

            var deferred = DeferredBuilder.Deferred();

            var url = MediaBrowser.ServerInfo.getServerAddress(server, connectionMode);

            HttpClient.send({

                type: "GET",
                url: url + "/system/info",
                dataType: "json",
                headers: {
                    "X-MediaBrowser-Token": server.AccessToken
                }

            }).done(function (systemInfo) {

                updateServerInfo(server, systemInfo);

                if (server.UserId) {

                    HttpClient.send({

                        type: "GET",
                        url: url + "/users/" + server.UserId,
                        dataType: "json",
                        headers: {
                            "X-MediaBrowser-Token": server.AccessToken
                        }

                    }).done(function (user) {

                        onLocalUserSignIn(user);
                        deferred.resolveWith(null, [[]]);

                    }).fail(function () {

                        server.UserId = null;
                        server.AccessToken = null;
                        deferred.resolveWith(null, [[]]);
                    });
                }

            }).fail(function () {

                server.UserId = null;
                server.AccessToken = null;
                deferred.resolveWith(null, [[]]);

            });

            return deferred.promise();
        }

        function getImageUrl(localUser) {

            if (connectUser && connectUser.ImageUrl) {
                return {
                    url: connectUser.ImageUrl
                };
            }
            if (localUser && localUser.PrimaryImageTag) {

                var apiClient = self.getApiClient(localUser);

                var url = apiClient.getUserImageUrl(localUser.Id, {
                    tag: localUser.PrimaryImageTag,
                    type: "Primary"
                });

                return {
                    url: url,
                    supportsParams: true
                };
            }

            return {
                url: null,
                supportsParams: false
            };
        }

        self.user = function (apiClient) {

            var deferred = DeferredBuilder.Deferred();

            var localUser;

            function onLocalUserDone() {

                var image = getImageUrl(localUser);

                deferred.resolveWith(null, [
                {
                    localUser: localUser,
                    name: connectUser ? connectUser.Name : (localUser ? localUser.Name : null),
                    canManageServer: localUser && localUser.Policy.IsAdministrator,
                    imageUrl: image.url,
                    supportsImageParams: image.supportsParams
                }]);
            }

            function onEnsureConnectUserDone() {

                if (apiClient && apiClient.getCurrentUserId()) {
                    apiClient.getCurrentUser().done(function (u) {
                        localUser = u;
                    }).always(onLocalUserDone);
                } else {
                    onLocalUserDone();
                }
            }

            var credentials = credentialProvider.credentials();

            if (credentials.ConnectUserId && credentials.ConnectAccessToken && !(apiClient && apiClient.getCurrentUserId())) {
                ensureConnectUser(credentials).always(onEnsureConnectUserDone);
            } else {
                onEnsureConnectUserDone();
            }

            return deferred.promise();
        };

        self.isLoggedIntoConnect = function () {

            // Make sure it returns true or false
            if (!self.connectToken() || !self.connectUserId()) {
                return false;
            }
            return true;
        };

        self.logout = function () {

            Logger.log('begin connectionManager loguot');
            var promises = [];

            for (var i = 0, length = apiClients.length; i < length; i++) {

                var apiClient = apiClients[i];

                if (apiClient.accessToken()) {
                    promises.push(logoutOfServer(apiClient));
                }
            }

            return DeferredBuilder.when(promises).done(function () {

                var credentials = credentialProvider.credentials();

                var servers = credentials.Servers.filter(function (u) {
                    return u.UserLinkType != "Guest";
                });

                for (var j = 0, numServers = servers.length; j < numServers; j++) {

                    var server = servers[j];

                    server.UserId = null;
                    server.AccessToken = null;
                    server.ExchangeToken = null;

                    var serverUsers = server.Users || [];

                    for (var k = 0, numUsers = serverUsers.length; k < numUsers; k++) {

                        serverUsers[k].IsSignedInOffline = false;
                    }
                }

                credentials.Servers = servers;
                credentials.ConnectAccessToken = null;
                credentials.ConnectUserId = null;

                credentialProvider.credentials(credentials);

                if (connectUser) {
                    connectUser = null;
                    Events.trigger(self, 'connectusersignedout');
                }
            });
        };

        function logoutOfServer(apiClient) {

            var serverInfo = apiClient.serverInfo() || {};

            var logoutInfo = {
                serverId: serverInfo.Id
            };

            return apiClient.logout().always(function () {

                Events.trigger(self, 'localusersignedout', [logoutInfo]);
            });
        }

        function getConnectServers(credentials) {

            logger.log('Begin getConnectServers');

            var deferred = DeferredBuilder.Deferred();

            if (!credentials.ConnectAccessToken || !credentials.ConnectUserId) {
                deferred.resolveWith(null, [[]]);
                return deferred.promise();
            }

            var url = "https://connect.mediabrowser.tv/service/servers?userId=" + credentials.ConnectUserId;

            HttpClient.send({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-Application": appName + "/" + appVersion,
                    "X-Connect-UserToken": credentials.ConnectAccessToken
                }

            }).done(function (servers) {

                servers = servers.map(function (i) {
                    return {
                        ExchangeToken: i.AccessKey,
                        ConnectServerId: i.Id,
                        Id: i.SystemId,
                        Name: i.Name,
                        RemoteAddress: i.Url,
                        LocalAddress: i.LocalAddress,
                        UserLinkType: (i.UserType || '').toLowerCase() == "guest" ? "Guest" : "LinkedUser"
                    };
                });

                deferred.resolveWith(null, [servers]);

            }).fail(function () {
                deferred.resolveWith(null, [[]]);

            });

            return deferred.promise();
        }

        self.getAvailableServers = function () {

            logger.log('Begin getAvailableServers');

            // Clone the array
            var credentials = credentialProvider.credentials();

            var deferred = DeferredBuilder.Deferred();

            var connectServersPromise = getConnectServers(credentials);
            var findServersPromise = findServers();

            connectServersPromise.done(function (connectServers) {

                findServersPromise.done(function (foundServers) {

                    var servers = credentials.Servers.slice(0);
                    mergeServers(servers, foundServers);
                    mergeServers(servers, connectServers);

                    servers = filterServers(servers, connectServers);

                    servers.sort(function (a, b) {
                        return (b.DateLastAccessed || 0) - (a.DateLastAccessed || 0);
                    });

                    credentials.Servers = servers;

                    credentialProvider.credentials(credentials);

                    deferred.resolveWith(null, [servers]);
                });
            });

            return deferred.promise();
        };

        function filterServers(servers, connectServers) {

            return servers.filter(function (server) {

                // It's not a connect server, so assume it's still valid
                if (!server.ExchangeToken) {
                    return true;
                }

                return connectServers.filter(function (connectServer) {

                    return server.Id == connectServer.Id;

                }).length > 0;
            });
        }

        function findServers() {

            var deferred = DeferredBuilder.Deferred();

            require(['serverdiscovery'], function () {
                ServerDiscovery.findServers(2500).done(function (foundServers) {

                    var servers = foundServers.map(function (foundServer) {

                        var info = {
                            Id: foundServer.Id,
                            LocalAddress: foundServer.Address,
                            Name: foundServer.Name,
                            ManualAddress: convertEndpointAddressToManualAddress(foundServer),
                            DateLastLocalConnection: new Date().getTime()
                        };

                        info.LastConnectionMode = info.ManualAddress ? MediaBrowser.ConnectionMode.Manual : MediaBrowser.ConnectionMode.Local;

                        return info;
                    });
                    deferred.resolveWith(null, [servers]);
                });

            });
            return deferred.promise();
        }

        function convertEndpointAddressToManualAddress(info) {

            if (info.Address && info.EndpointAddress) {
                var address = info.EndpointAddress.split(":")[0];

                // Determine the port, if any
                var parts = info.Address.split(":");
                if (parts.length > 1) {
                    var portString = parts[parts.length - 1];

                    if (!isNaN(parseInt(portString))) {
                        address += ":" + portString;
                    }
                }

                return normalizeAddress(address);
            }

            return null;
        }

        self.connect = function () {

            logger.log('Begin connect');

            var deferred = DeferredBuilder.Deferred();

            self.getAvailableServers().done(function (servers) {

                self.connectToServers(servers).done(function (result) {

                    deferred.resolveWith(null, [result]);

                });
            });

            return deferred.promise();
        };

        self.getOffineResult = function () {

            // TODO: Implement
        };

        self.connectToServers = function (servers) {

            logger.log('Begin connectToServers, with ' + servers.length + ' servers');

            var deferred = DeferredBuilder.Deferred();

            if (servers.length == 1) {

                self.connectToServer(servers[0]).done(function (result) {

                    if (result.State == MediaBrowser.ConnectionState.Unavailable) {

                        result.State = result.ConnectUser == null ?
                            MediaBrowser.ConnectionState.ConnectSignIn :
                            MediaBrowser.ConnectionState.ServerSelection;
                    }

                    logger.log('resolving connectToServers with result.State: ' + result.State);
                    deferred.resolveWith(null, [result]);

                });

            } else {

                var firstServer = servers.length ? servers[0] : null;
                // See if we have any saved credentials and can auto sign in
                if (firstServer) {
                    self.connectToServer(firstServer).done(function (result) {

                        if (result.State == MediaBrowser.ConnectionState.SignedIn) {

                            deferred.resolveWith(null, [result]);

                        } else {
                            deferred.resolveWith(null, [
                            {
                                Servers: servers,
                                State: (!servers.length && !self.connectUser()) ? MediaBrowser.ConnectionState.ConnectSignIn : MediaBrowser.ConnectionState.ServerSelection,
                                ConnectUser: self.connectUser()
                            }]);
                        }

                    });
                } else {

                    deferred.resolveWith(null, [
                    {
                        Servers: servers,
                        State: (!servers.length && !self.connectUser()) ? MediaBrowser.ConnectionState.ConnectSignIn : MediaBrowser.ConnectionState.ServerSelection,
                        ConnectUser: self.connectUser()
                    }]);
                }
            }

            return deferred.promise();
        };

        function beginWakeServer(server) {

            require(['wakeonlan'], function () {
                var infos = server.WakeOnLanInfos || [];

                for (var i = 0, length = infos.length; i < length; i++) {

                    WakeOnLan.send(infos[i]);
                }
            });
        }

        self.connectToServer = function (server, options) {

            var deferred = DeferredBuilder.Deferred();

            var tests = [];

            if (server.LastConnectionMode != null) {
                tests.push(server.LastConnectionMode);
            }
            if (tests.indexOf(MediaBrowser.ConnectionMode.Manual) == -1) { tests.push(MediaBrowser.ConnectionMode.Manual); }
            if (tests.indexOf(MediaBrowser.ConnectionMode.Local) == -1) { tests.push(MediaBrowser.ConnectionMode.Local); }
            if (tests.indexOf(MediaBrowser.ConnectionMode.Remote) == -1) { tests.push(MediaBrowser.ConnectionMode.Remote); }

            beginWakeServer(server);

            var wakeOnLanSendTime = new Date().getTime();

            testNextConnectionMode(tests, 0, server, wakeOnLanSendTime, options, deferred);

            return deferred.promise();
        };

        function stringEqualsIgnoreCase(str1, str2) {

            return (str1 || '').toLowerCase() == (str2 || '').toLowerCase();
        }

        function testNextConnectionMode(tests, index, server, wakeOnLanSendTime, options, deferred) {

            if (index >= tests.length) {

                logger.log('Tested all connection modes. Failing server connection.');
                resolveWithFailure(deferred);
                return;
            }

            var mode = tests[index];
            var address = MediaBrowser.ServerInfo.getServerAddress(server, mode);
            var enableRetry = false;
            var skipTest = false;
            var timeout = defaultTimeout;

            if (mode == MediaBrowser.ConnectionMode.Local) {

                enableRetry = true;
                timeout = 5000;
            }

            else if (mode == MediaBrowser.ConnectionMode.Manual) {

                if (stringEqualsIgnoreCase(address, server.LocalAddress) ||
                        stringEqualsIgnoreCase(address, server.RemoteAddress)) {
                    skipTest = true;
                }
            }

            if (skipTest || !address) {
                testNextConnectionMode(tests, index + 1, server, wakeOnLanSendTime, options, deferred);
                return;
            }

            logger.log('testing connection mode ' + mode + ' with server ' + server.Name);

            tryConnect(address, timeout).done(function (result) {

                logger.log('calling onSuccessfulConnection with connection mode ' + mode + ' with server ' + server.Name);
                onSuccessfulConnection(server, result, mode, options, deferred);

            }).fail(function () {

                logger.log('test failed for connection mode ' + mode + ' with server ' + server.Name);

                if (enableRetry) {

                    var sleepTime = 10000 - (new Date().getTime() - wakeOnLanSendTime);

                    // TODO: Implement delay and retry

                    testNextConnectionMode(tests, index + 1, server, wakeOnLanSendTime, options, deferred);

                } else {
                    testNextConnectionMode(tests, index + 1, server, wakeOnLanSendTime, options, deferred);

                }
            });
        }

        function onSuccessfulConnection(server, systemInfo, connectionMode, options, deferred) {

            var credentials = credentialProvider.credentials();
            if (credentials.ConnectAccessToken) {

                ensureConnectUser(credentials).done(function () {

                    if (server.ExchangeToken) {
                        addAuthenticationInfoFromConnect(server, connectionMode, credentials).always(function () {

                            afterConnectValidated(server, credentials, systemInfo, connectionMode, true, options, deferred);
                        });

                    } else {

                        afterConnectValidated(server, credentials, systemInfo, connectionMode, true, options, deferred);
                    }
                });
            }
            else {
                afterConnectValidated(server, credentials, systemInfo, connectionMode, true, options, deferred);
            }
        }

        function afterConnectValidated(server, credentials, systemInfo, connectionMode, verifyLocalAuthentication, options, deferred) {

            if (verifyLocalAuthentication && server.AccessToken) {

                validateAuthentication(server, connectionMode).done(function () {

                    afterConnectValidated(server, credentials, systemInfo, connectionMode, false, options, deferred);
                });

                return;
            }

            updateServerInfo(server, systemInfo);

            server.DateLastAccessed = new Date().getTime();
            server.LastConnectionMode = connectionMode;
            credentialProvider.addOrUpdateServer(credentials.Servers, server);
            credentialProvider.credentials(credentials);

            var result = {
                Servers: []
            };

            result.ApiClient = getOrAddApiClient(server, connectionMode);
            result.State = server.AccessToken ?
                MediaBrowser.ConnectionState.SignedIn :
                MediaBrowser.ConnectionState.ServerSignIn;

            result.Servers.push(server);
            result.ApiClient.updateServerInfo(server, connectionMode);

            if (result.State == MediaBrowser.ConnectionState.SignedIn) {
                afterConnected(result.ApiClient, options);
            }

            deferred.resolveWith(null, [result]);

            Events.trigger(self, 'connected', [result]);
        }

        function normalizeAddress(address) {

            // attempt to correct bad input
            address = address.trim();

            if (address.toLowerCase().indexOf('http') != 0) {
                address = "http://" + address;
            }

            // Seeing failures in iOS when protocol isn't lowercase
            address = address.replace('Http:', 'http:');
            address = address.replace('Https:', 'https:');

            return address;
        }

        self.connectToAddress = function (address) {

            var deferred = DeferredBuilder.Deferred();

            if (!address) {
                deferred.reject();
                return deferred.promise();
            }

            address = normalizeAddress(address);

            function onFail() {
                logger.log('connectToAddress ' + address + ' failed');
                resolveWithFailure(deferred);
            }

            tryConnect(address, defaultTimeout).done(function (publicInfo) {

                logger.log('connectToAddress ' + address + ' succeeded');

                var server = {
                    ManualAddress: address,
                    LastConnectionMode: MediaBrowser.ConnectionMode.Manual
                };
                updateServerInfo(server, publicInfo);

                self.connectToServer(server).done(function (result) {

                    deferred.resolveWith(null, [result]);

                }).fail(onFail);

            }).fail(onFail);

            return deferred.promise();
        };

        self.loginToConnect = function (username, password) {

            var deferred = DeferredBuilder.Deferred();

            if (!username) {
                deferred.reject();
                return deferred.promise();
            }
            if (!password) {
                deferred.reject();
                return deferred.promise();
            }

            require(['connectservice'], function () {

                var md5 = self.getConnectPasswordHash(password);

                HttpClient.send({
                    type: "POST",
                    url: "https://connect.mediabrowser.tv/service/user/authenticate",
                    data: {
                        nameOrEmail: username,
                        password: md5
                    },
                    dataType: "json",
                    contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
                    headers: {
                        "X-Application": appName + "/" + appVersion
                    }

                }).done(function (result) {

                    var credentials = credentialProvider.credentials();

                    credentials.ConnectAccessToken = result.AccessToken;
                    credentials.ConnectUserId = result.User.Id;

                    credentialProvider.credentials(credentials);

                    onConnectUserSignIn(result.User);

                    deferred.resolveWith(null, [result]);

                }).fail(function () {
                    deferred.reject();
                });
            });

            return deferred.promise();
        };

        self.signupForConnect = function (email, username, password, passwordConfirm) {

            var deferred = DeferredBuilder.Deferred();

            if (!email) {
                deferred.rejectWith(null, [{ errorCode: 'invalidinput' }]);
                return deferred.promise();
            }
            if (!username) {
                deferred.rejectWith(null, [{ errorCode: 'invalidinput' }]);
                return deferred.promise();
            }
            if (!password) {
                deferred.rejectWith(null, [{ errorCode: 'invalidinput' }]);
                return deferred.promise();
            }
            if (!passwordConfirm) {
                deferred.rejectWith(null, [{ errorCode: 'passwordmatch' }]);
                return deferred.promise();
            }
            if (password != passwordConfirm) {
                deferred.rejectWith(null, [{ errorCode: 'passwordmatch' }]);
                return deferred.promise();
            }

            require(['connectservice'], function () {

                var md5 = self.getConnectPasswordHash(password);

                HttpClient.send({
                    type: "POST",
                    url: "https://connect.mediabrowser.tv/service/register",
                    data: {
                        email: email,
                        userName: username,
                        password: md5
                    },
                    dataType: "json",
                    contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
                    headers: {
                        "X-Application": appName + "/" + appVersion,
                        "X-CONNECT-TOKEN": "CONNECT-REGISTER"
                    }

                }).done(function (result) {

                    deferred.resolve(null, []);

                }).fail(function (e) {

                    try {

                        var result = JSON.parse(e.responseText);

                        deferred.rejectWith(null, [{ errorCode: result.Status }]);
                    } catch (err) {
                        deferred.rejectWith(null, [{}]);
                    }
                });
            });

            return deferred.promise();
        };

        self.getConnectPasswordHash = function (password) {

            password = globalScope.MediaBrowser.ConnectService.cleanPassword(password);

            return CryptoJS.MD5(password).toString();
        };

        self.getApiClient = function (item) {

            // Accept string + object
            if (item.ServerId) {
                item = item.ServerId;
            }

            return apiClients.filter(function (a) {

                var serverInfo = a.serverInfo();

                // We have to keep this hack in here because of the addApiClient method
                return !serverInfo || serverInfo.Id == item;

            })[0];
        };

        self.getUserInvitations = function () {

            var connectToken = self.connectToken();

            if (!connectToken) {
                throw new Error("null connectToken");
            }
            if (!self.connectUserId()) {
                throw new Error("null connectUserId");
            }

            var url = "https://connect.mediabrowser.tv/service/servers?userId=" + self.connectUserId() + "&status=Waiting";

            return HttpClient.send({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-Connect-UserToken": connectToken,
                    "X-Application": appName + "/" + appVersion
                }

            });
        };

        self.deleteServer = function (serverId) {

            var credentials = credentialProvider.credentials();

            var serverInfo = credentials.Servers = credentials.Servers.filter(function (s) {
                return s.ConnectServerId == serverId;
            });

            function onDone() {

                credentials = credentialProvider.credentials();

                credentials.Servers = credentials.Servers.filter(function (s) {
                    return s.ConnectServerId != serverId;
                });

                credentialProvider.credentials(credentials);
            }

            if (serverInfo.ExchangeToken) {

                var connectToken = self.connectToken();

                if (!serverId) {
                    throw new Error("null serverId");
                }
                if (!connectToken) {
                    throw new Error("null connectToken");
                }
                if (!self.connectUserId()) {
                    throw new Error("null connectUserId");
                }

                var url = "https://connect.mediabrowser.tv/service/serverAuthorizations?serverId=" + serverId + "&userId=" + self.connectUserId();

                return HttpClient.send({
                    type: "DELETE",
                    url: url,
                    headers: {
                        "X-Connect-UserToken": connectToken,
                        "X-Application": appName + "/" + appVersion
                    }

                }).done(onDone);

            } else {

                onDone();
                var deferred = DeferredBuilder.Deferred();
                deferred.resolve();
                return deferred.promise();
            }
        };

        self.rejectServer = function (serverId) {

            var connectToken = self.connectToken();

            if (!serverId) {
                throw new Error("null serverId");
            }
            if (!connectToken) {
                throw new Error("null connectToken");
            }
            if (!self.connectUserId()) {
                throw new Error("null connectUserId");
            }

            var url = "https://connect.mediabrowser.tv/service/serverAuthorizations?serverId=" + serverId + "&userId=" + self.connectUserId();

            return HttpClient.send({
                type: "DELETE",
                url: url,
                headers: {
                    "X-Connect-UserToken": connectToken,
                    "X-Application": appName + "/" + appVersion
                }

            });
        };

        self.acceptServer = function (serverId) {

            var connectToken = self.connectToken();

            if (!serverId) {
                throw new Error("null serverId");
            }
            if (!connectToken) {
                throw new Error("null connectToken");
            }
            if (!self.connectUserId()) {
                throw new Error("null connectUserId");
            }

            var url = "https://connect.mediabrowser.tv/service/ServerAuthorizations/accept?serverId=" + serverId + "&userId=" + self.connectUserId();

            return HttpClient.send({
                type: "GET",
                url: url,
                headers: {
                    "X-Connect-UserToken": connectToken,
                    "X-Application": appName + "/" + appVersion
                }

            });
        };

        self.getRegistrationInfo = function (feature, apiClient) {

            var deferred = DeferredBuilder.Deferred();

            self.getAvailableServers().done(function (servers) {

                var matchedServers = servers.filter(function (s) {
                    return stringEqualsIgnoreCase(s.Id, apiClient.serverInfo().Id);
                });

                if (!matchedServers.length) {
                    deferred.resolveWith(null, [{}]);
                    return;
                }

                var match = matchedServers[0];

                // 31 days
                if ((new Date().getTime() - (match.DateLastLocalConnection || 0)) > 2678400000) {
                    deferred.resolveWith(null, [{}]);
                    return;
                }

                apiClient.getRegistrationInfo(feature).done(function (result) {

                    deferred.resolveWith(null, [result]);
                }).fail(function () {

                    deferred.reject();
                });

            }).fail(function () {

                deferred.reject();
            });

            return deferred.promise();
        };


        return self;
    };

})(window, window.Logger);