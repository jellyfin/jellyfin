(function (globalScope) {

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.ConnectionState = {
        Unavilable: 0,
        ServerSelection: 1,
        ServerSignIn: 2,
        SignedIn: 3,
        ConnectSignIn: 4
    };

    globalScope.MediaBrowser.ConnectionMode = {
        Local: 0,
        Remote: 1
    };

    globalScope.MediaBrowser.ConnectionManager = function ($, logger, credentialProvider, appName, appVersion, deviceName, deviceId, capabilities) {

        logger.log('Begin MediaBrowser.ConnectionManager constructor');

        var self = this;
        var apiClients = [];

        function mergeServers(list1, list2) {

            for (var i = 0, length = list2.length; i < length; i++) {
                credentialProvider.addOrUpdateServer(list1, list2[i]);
            }

            return list1;
        }

        function resolveWithFailure(deferred) {

            deferred.resolveWith(null, [
            {
                state: MediaBrowser.ConnectionState.Unavilable,
                connectUser: self.connectUser()
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

            return $.ajax({

                type: "GET",
                url: url + "/system/info/public",
                dataType: "json",

                timeout: timeout || 15000

            });
        }

        var connectUser;
        self.connectUser = function () {
            return connectUser;
        };

        self.appVersion = function () {
            return appVersion;
        };

        self.deviceId = function () {
            return deviceId;
        };

        self.currentApiClient = function () {

            return apiClients[0];
        };

        self.connectUserId = function () {
            return credentialProvider.credentials().ConnectUserId;
        };

        self.connectToken = function () {

            return credentialProvider.credentials().ConnectAccessToken;
        };

        self.addApiClient = function (apiClient, enableAutomaticNetworking) {

            apiClients.push(apiClient);

            return apiClient.getPublicSystemInfo().done(function (systemInfo) {

                var server = credentialProvider.credentials().servers.filter(function (s) {

                    return s.Id == systemInfo.Id;

                })[0] || {};

                updateServerInfo(server, systemInfo);

                apiClient.serverInfo(server);
                $(self).trigger('apiclientcreated', [apiClient]);

                if (enableAutomaticNetworking) {
                    self.connectToServer(server);
                }
            });

        };

        function onConnectAuthenticated(user) {

            connectUser = user;
            $(self).trigger('connectusersignedin', [user]);
        }

        function getOrAddApiClient(server, connectionMode) {

            var apiClient = self.getApiClient(server.Id);

            if (!apiClient) {

                var url = connectionMode == MediaBrowser.ConnectionMode.Local ? server.LocalAddress : server.RemoteAddress;

                apiClient = new MediaBrowser.ApiClient($, logger, url, appName, appVersion, deviceName, deviceId, capabilities);

                apiClients.push(apiClient);

                apiClient.serverInfo(server);

                $(apiClient).on('authenticated', function (e, result) {
                    onLocalAuthenticated(this, result, true);
                });

                $(self).trigger('apiclientcreated', [apiClient]);

            }

            if (!server.AccessToken) {

                apiClient.clearAuthenticationInfo();
            }
            else {

                apiClient.setAuthenticationInfo(server.AccessToken, server.UserId);
            }

            return apiClient;
        }

        function onLocalAuthenticated(apiClient, result, saveCredentials) {

            apiClient.getSystemInfo().done(function (systemInfo) {

                var server = apiClient.serverInfo;
                updateServerInfo(server, systemInfo);

                var credentials = credentialProvider.credentials();

                server.DateLastAccessed = new Date().getTime();

                if (saveCredentials) {
                    server.UserId = result.User.Id;
                    server.AccessToken = result.AccessToken;
                } else {
                    server.UserId = null;
                    server.AccessToken = null;
                }

                credentials.addOrUpdateServer(credentials.servers, server);
                credentialProvider.credentials(credentials);

                ensureWebSocket(apiClient);

                onLocalUserSignIn(result.User);

            });
        }

        function ensureWebSocket(apiClient) {

            if (!apiClient.isWebSocketOpenOrConnecting && apiClient.isWebSocketSupported()) {
                apiClient.openWebSocket();
            }
        }

        function onLocalUserSignIn(user) {

            $(self).trigger('localusersignedin', [user]);
        }

        function ensureConnectUser(credentials) {

            var deferred = $.Deferred();

            if (connectUser && connectUser.Id == credentials.ConnectUserId) {
                deferred.resolveWith(null, [[]]);
            }

            else if (credentials.ConnectAccessToken && credentials.ConnectUserId) {

                connectUser = null;

                getConnectUser(credentials.ConnectUserId, credentials.ConnectAccessToken).done(function (user) {

                    onConnectAuthenticated(user);
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

            return $.ajax({
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

            var url = connectionMode == MediaBrowser.ConnectionMode.Local ? server.LocalAddress : server.RemoteAddress;

            url += "/Connect/Exchange?format=json&ConnectUserId=" + credentials.ConnectUserId;

            return $.ajax({
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

            var deferred = $.Deferred();

            var url = connectionMode == MediaBrowser.ConnectionMode.Local ? server.LocalAddress : server.RemoteAddress;

            $.ajax({

                type: "GET",
                url: url + "/system/info",
                dataType: "json",
                headers: {
                    "X-MediaBrowser-Token": server.AccessToken
                }

            }).done(function (systemInfo) {

                updateServerInfo(server, systemInfo);

                if (server.UserId) {

                    $.ajax({

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
            if (localUser.PrimaryImageTag) {

                var apiClient = self.getApiClient(localUser);

                var url = apiClient.getUserImageUrl(localUser.Id, {
                    tag: localUser.PrimaryImageTag,
                    type: "Primary"
                });

                return {
                    url: url,
                    supportsImageParams: true
                };
            }

            return {
                url: null,
                supportsImageParams: false
            };
        }

        self.user = function () {

            var deferred = $.Deferred();

            var localUser;

            function onLocalUserDone() {

                var image = getImageUrl(localUser);

                deferred.resolveWith(null, [
                {
                    localUser: localUser,
                    name: connectUser ? connectUser.Name : localUser.Name,
                    canManageServer: localUser && localUser.Policy.IsAdministrator,
                    imageUrl: image.url,
                    supportsImageParams: image.supportsParams
                }]);
            }

            function onEnsureConnectUserDone() {

                var apiClient = self.currentApiClient();
                if (apiClient && apiClient.getCurrentUserId()) {
                    apiClient.getUser(apiClient.getCurrentUserId()).done(function (u) {
                        localUser = u;
                    }).always(onLocalUserDone);
                } else {
                    onLocalUserDone();
                }
            }

            var credentials = credentialProvider.credentials();

            if (credentials.ConnectUserId && credentials.ConnectAccessToken && !(self.currentApiClient() && self.currentApiClient().getCurrentUserId())) {
                ensureConnectUser(credentials).always(onEnsureConnectUserDone);
            } else {
                onEnsureConnectUserDone();
            }

            return deferred.promise();
        };

        self.isLoggedIntoConnect = function () {

            return self.connectToken() && self.connectUserId();
        };

        self.logout = function () {

            var promises = [];

            for (var i = 0, length = apiClients.length; i < length; i++) {

                var apiClient = apiClients[i];

                if (apiClient.accessToken()) {
                    promises.push(apiClient.logout());
                }
            }

            return $.when(promises).done(function () {

                var credentials = credentialProvider.credentials();

                var servers = credentials.servers.filter(function (u) {
                    return u.UserLinkType != "Guest";
                });

                for (var j = 0, numServers = servers.length; j < numServers; j++) {
                    servers[j].UserId = null;
                    servers[j].AccessToken = null;
                    servers[j].ExchangeToken = null;
                }

                credentials.servers = servers;
                credentials.ConnectAccessToken = null;
                credentials.ConnectUserId = null;

                credentialProvider.credentials(credentials);

                connectUser = null;

                $(self).trigger('signedout');
            });
        };

        function getConnectServers() {

            logger.log('Begin getConnectServers');

            var deferred = $.Deferred();

            if (!self.connectToken() || !self.connectUserId()) {
                deferred.resolveWith(null, [[]]);
                return deferred.promise();
            }

            var url = "https://connect.mediabrowser.tv/service/servers?userId=" + self.connectUserId();

            $.ajax({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-Application": appName + "/" + appVersion,
                    "X-Connect-UserToken": self.connectToken()
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

        self.getServers = function () {

            logger.log('Begin getServers');

            // Clone the array
            var credentials = credentialProvider.credentials();
            var servers = credentials.servers.slice(0);

            var deferred = $.Deferred();

            getConnectServers().done(function (result) {

                var newList = mergeServers(servers, result);

                newList.sort(function (a, b) {
                    return b.DateLastAccessed - a.DateLastAccessed;
                });

                credentials.servers = newList;

                credentialProvider.credentials(credentials);

                deferred.resolveWith(null, [newList]);
            });

            return deferred.promise();
        };

        self.connect = function () {

            logger.log('Begin connect');

            var deferred = $.Deferred();

            self.getServers().done(function (servers) {

                self.connectToServers(servers).done(function (result) {

                    deferred.resolveWith(null, [result]);

                });
            });

            return deferred.promise();
        };

        self.connectToServers = function (servers) {

            var deferred = $.Deferred();

            if (servers.length == 1) {

                self.connectToServer(servers[0]).done(function (result) {

                    if (result.State == MediaBrowser.ConnectionState.Unavailable) {

                        result.State = result.ConnectUser == null ?
                            MediaBrowser.ConnectionState.ConnectSignIn :
                            MediaBrowser.ConnectionState.ServerSelection;
                    }

                    deferred.resolveWith(null, [result]);

                });

            } else {

                // Find the first server with a saved access token
                var currentServer = servers.filter(function (s) {
                    return s.AccessToken;
                })[0];

                if (currentServer) {
                    self.connectToServer(currentServer).done(function (result) {

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

        self.connectToServer = function (server) {

            var deferred = $.Deferred();

            function onLocalServerTokenValidationDone(connectionMode, credentials) {

                credentialProvider.addOrUpdateServer(credentials.servers, server);
                server.DateLastAccessed = new Date().getTime();

                credentialProvider.credentials(credentials);

                var result = {
                    Servers: []
                };

                result.ApiClient = getOrAddApiClient(server, connectionMode);
                result.State = server.AccessToken ?
                    MediaBrowser.ConnectionState.SignedIn :
                    MediaBrowser.ConnectionState.ServerSignIn;

                result.ApiClient.enableAutomaticNetworking(server, connectionMode);

                if (result.State == MediaBrowser.ConnectionState.SignedIn) {
                    ensureWebSocket(result.ApiClient);
                }

                result.Servers.push(server);

                deferred.resolveWith(null, [result]);

                $(self).trigger('connected', [result]);
            }

            function onExchangeTokenDone(connectionMode, credentials) {

                if (server.AccessToken) {
                    validateAuthentication(server, connectionMode).always(function() {
                 
                        onLocalServerTokenValidationDone(connectionMode, credentials);
                    });
                } else {
                    onLocalServerTokenValidationDone(connectionMode, credentials);
                }
            }

            function onEnsureConnectUserDone(connectionMode, credentials) {

                if (credentials.ConnectUserId && credentials.ConnectAccessToken && server.ExchangeToken) {

                    addAuthenticationInfoFromConnect(server, connectionMode, credentials).always(function() {
                        
                        onExchangeTokenDone(connectionMode, credentials);
                    });

                } else {
                    onExchangeTokenDone(connectionMode, credentials);
                }
            }

            function onRemoteTestDone(systemInfo, connectionMode) {

                if (systemInfo == null) {

                    resolveWithFailure(deferred);
                    return;
                }

                updateServerInfo(server, systemInfo);
                server.LastConnectionMode = connectionMode;
                var credentials = credentialProvider.credentials();

                if (credentials.ConnectUserId && credentials.ConnectAccessToken) {
                    ensureConnectUser(credentials).always(function() {
                        onEnsureConnectUserDone(connectionMode, credentials);
                    });
                } else {
                    onEnsureConnectUserDone(connectionMode, credentials);
                }
            }

            function onLocalTestDone(systemInfo, connectionMode) {

                if (!systemInfo && server.RemoteAddress) {

                    // Try to connect to the local address
                    tryConnect(server.RemoteAddress).done(function (result) {

                        onRemoteTestDone(result, MediaBrowser.ConnectionMode.Remote);

                    }).fail(function() {
                        onRemoteTestDone();
                    });

                } else {
                    onRemoteTestDone(systemInfo, connectionMode);
                }
            }

            if (server.LocalAddress) {

                //onLocalTestDone();
                // Try to connect to the local address
                tryConnect(server.LocalAddress, 5000).done(function (result) {
                    onLocalTestDone(result, MediaBrowser.ConnectionMode.Local);
                }).fail(function () {
                    onLocalTestDone();
                });

            } else {
                onLocalTestDone();
            }

            return deferred.promise();
        };

        self.connectToAddress = function (address) {

            if (address.toLowerCase().indexOf('http') != 0) {
                address = "http://" + address;
            }

            var deferred = $.Deferred();

            tryConnect(address).done(function (publicInfo) {

                var server = {};
                updateServerInfo(server, publicInfo);

                self.connectToServer(server).done(function (result) {

                    deferred.resolveWith(null, [result]);

                }).fail(function () {

                    resolveWithFailure(deferred);
                });

            }).fail(function () {

                resolveWithFailure(deferred);
            });

            return deferred.promise();
        };

        self.loginToConnect = function (username, password) {

            if (!username) {
                throw new Error("null username");
            }
            if (!password) {
                throw new Error("null password");
            }

            var md5 = self.getConnectPasswordHash(password);

            return $.ajax({
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

                onConnectAuthenticated(result.User);
            });
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

            if (!self.connectToken()) {
                throw new Error("null connectToken");
            }
            if (!self.connectUserId()) {
                throw new Error("null connectUserId");
            }

            var url = "https://connect.mediabrowser.tv/service/servers?userId=" + self.connectUserId() + "&status=Waiting";

            return $.ajax({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-Connect-UserToken": self.connectToken(),
                    "X-Application": appName + "/" + appVersion
                }

            });
        };

        self.deleteServer = function (serverId) {

            if (!serverId) {
                throw new Error("null serverId");
            }
            if (!self.connectToken()) {
                throw new Error("null connectToken");
            }
            if (!self.connectUserId()) {
                throw new Error("null connectUserId");
            }

            var url = "https://connect.mediabrowser.tv/service/serverAuthorizations?serverId=" + serverId + "&userId=" + self.connectUserId();

            return $.ajax({
                type: "DELETE",
                url: url,
                headers: {
                    "X-Connect-UserToken": self.connectToken(),
                    "X-Application": appName + "/" + appVersion
                }

            }).done(function () {

                var credentials = credentialProvider.credentials();

                credentials.servers = credentials.servers.filter(function (s) {
                    return s.ConnectServerId != serverId;
                });

                credentialProvider.credentials(credentials);

            });
        };

        self.rejectServer = function (serverId) {

            if (!serverId) {
                throw new Error("null serverId");
            }
            if (!self.connectToken()) {
                throw new Error("null connectToken");
            }
            if (!self.connectUserId()) {
                throw new Error("null connectUserId");
            }

            var url = "https://connect.mediabrowser.tv/service/serverAuthorizations?serverId=" + serverId + "&userId=" + self.connectUserId();

            return $.ajax({
                type: "DELETE",
                url: url,
                headers: {
                    "X-Connect-UserToken": self.connectToken(),
                    "X-Application": appName + "/" + appVersion
                }

            });
        };

        self.acceptServer = function (serverId) {

            if (!serverId) {
                throw new Error("null serverId");
            }
            if (!self.connectToken()) {
                throw new Error("null connectToken");
            }
            if (!self.connectUserId()) {
                throw new Error("null connectUserId");
            }

            var url = "https://connect.mediabrowser.tv/service/ServerAuthorizations/accept?serverId=" + serverId + "&userId=" + self.connectUserId();

            return $.ajax({
                type: "GET",
                url: url,
                headers: {
                    "X-Connect-UserToken": self.connectToken(),
                    "X-Application": appName + "/" + appVersion
                }

            });
        };

        return self;
    };

})(window, window.jQuery, window.Logger);