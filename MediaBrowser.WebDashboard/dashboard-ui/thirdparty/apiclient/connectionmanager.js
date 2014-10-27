(function (globalScope, $) {

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.ConnectionState = {
        Unavilable: 0,
        ServerSelection: 1,
        ServerSignIn: 2,
        SignedIn: 3
    };

    globalScope.MediaBrowser.ConnectionMode = {
        Local: 0,
        Remote: 1
    };

    globalScope.MediaBrowser.ConnectionManager = function (credentialProvider, appName, applicationVersion, deviceName, deviceId, capabilities) {

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

        function tryConnect(url) {

            return $.ajax({

                type: "GET",
                url: url + "/mediabrowser/system/info/public",
                dataType: "json",

                error: function () {
                },

                timeout: 5000

            });
        }

        var connectUser;
        self.connectUser = function () {
            return connectUser;
        };

        self.appVersion = function () {
            return applicationVersion;
        };

        self.deviceId = function () {
            return deviceId;
        };

        self.currentApiClient = function () {

            return apiClients[0];
        };

        self.addApiClient = function (apiClient) {

            apiClients.push(apiClient);

            apiClient.getPublicSystemInfo().done(function (systemInfo) {

                var server = {};
                updateServerInfo(server, systemInfo);

                apiClient.serverInfo(server);
                $(this).trigger('apiclientcreated', [apiClient]);
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

                apiClient = new MediaBrowser.ApiClient(url, appName, applicationVersion, deviceName, deviceId, capabilities);

                apiClients.push(apiClient);

                apiClient.serverInfo(server);

                $(apiClient).on('authenticated', function (e, result) {
                    onLocalAuthenticated(this, result, true);
                });

                $(this).trigger('apiclientcreated', [apiClient]);

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

            if (self.isLoggedIntoConnect() && !connectUser) {
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

            var url = "https://connect.mediabrowser.tv/service/user?id=" + userId;

            return $.ajax({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-Connect-UserToken": accessToken
                },

                error: function () {

                }

            });
        }

        function addAuthenticationInfoFromConnect(server, connectionMode, credentials) {

            var url = connectionMode == MediaBrowser.ConnectionMode.Local ? server.LocalAddress : server.RemoteAddress;

            url += "/mediabrowser/Connect/Exchange?format=json&ConnectUserId=" + credentials.ConnectUserId;

            return $.ajax({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-MediaBrowser-Token": server.ExchangeToken
                },

                error: function () {

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
                url: url + "/mediabrowser/system/info",
                dataType: "json",
                headers: {
                    "X-MediaBrowser-Token": server.AccessToken
                },

                error: function () {

                }

            }).done(function (systemInfo) {

                updateServerInfo(server, systemInfo);

                if (server.UserId) {

                    $.ajax({

                        type: "GET",
                        url: url + "/mediabrowser/users/" + server.UserId,
                        dataType: "json",
                        headers: {
                            "X-MediaBrowser-Token": server.AccessToken
                        },

                        error: function () {

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
                    connectUser: connectUser,
                    localUser: localUser,
                    name: connectUser ? connectUser.Name : localUser.Name,
                    canManageServer: localUser && localUser.Configuration.IsAdministrator,
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

            if (credentials.ConnectUserId && credentials.ConnectAccessToken) {
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

            var i, length;
            var promises = [];

            for (i = 0, length = apiClients.length; i < length; i++) {

                var apiClient = apiClients[i];

                if (apiClient.accessToken()) {
                    promises.push(apiClient.logout());
                }
            }

            return $.when(promises).done(function () {

                var credentials = credentialProvider.credentials();

                for (i = 0, length = credentials.servers.length; i < length; i++) {
                    credentials.servers[i].UserId = null;
                    credentials.servers[i].AccessToken = null;
                    credentials.servers[i].ExchangeToken = null;
                }

                credentials.ConnectAccessToken = null;
                credentials.ConnectUserId = null;

                credentialProvider.credentials(credentials);

                connectUser = null;

                $(self).trigger('signedout');

            });
        };

        self.connectUserId = function () {
            return credentialProvider.credentials().ConnectUserId;
        };

        self.connectToken = function () {

            return credentialProvider.credentials().ConnectAccessToken;
        };

        function getConnectServers() {

            var deferred = $.Deferred();

            var url = "https://connect.mediabrowser.tv/service/servers?userId=" + self.connectUserId();

            $.ajax({
                type: "GET",
                url: url,
                dataType: "json",
                headers: {
                    "X-Connect-UserToken": self.connectToken()
                },

                error: function () {

                }

            }).done(function (servers) {

                servers = servers.map(function (i) {
                    return {
                        ExchangeToken: i.AccessKey,
                        Id: i.SystemId,
                        Name: i.Name,
                        RemoteAddress: i.Url,
                        LocalAddress: null
                    };
                });

                deferred.resolveWith(null, [servers]);

            }).fail(function () {
                deferred.resolveWith(null, [[]]);

            });

            return deferred.promise();
        }

        self.getServers = function () {

            // Clone the array
            var credentials = credentialProvider.credentials();
            var servers = credentials.servers.slice(0);

            var deferred = $.Deferred();

            getConnectServers().done(function (result) {

                var newList = mergeServers(servers, result);

                newList.sort(function (a, b) {
                    return b.DateLastAccessed - a.DateLastAccessed;
                });

                deferred.resolveWith(null, [newList]);

                credentials.servers = newList;

                credentialProvider.credentials(credentials);
            });

            return deferred.promise();
        };

        self.connect = function () {

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

                if (!servers[0].DateLastAccessed && !self.connectUserId()) {

                    deferred.resolveWith(null, [
                        {
                            Servers: servers,
                            State: MediaBrowser.ConnectionState.ServerSelection,
                            ConnectUser: self.connectUser()
                        }
                    ]);

                } else {

                    self.connectToServer(servers[0]).done(function (result) {

                        if (result.State == MediaBrowser.ConnectionState.Unavailable) {
                            result.State = MediaBrowser.ConnectionState.ServerSelection;
                        }

                        deferred.resolveWith(null, [result]);

                    }).fail(function () {

                        deferred.resolveWith(null, [
                            {
                                Servers: servers,
                                State: MediaBrowser.ConnectionState.ServerSelection,
                                ConnectUser: self.connectUser()
                            }
                        ]);

                    });
                }

            } else {

                // Find the first server with a saved access token
                var currentServer = servers.filter(function (s) {
                    return s.AccessToken;
                })[0];

                if (currentServer) {
                    self.connectToServer(currentServer).done(function (result) {

                        deferred.resolveWith(null, [result]);

                    }).fail(function () {

                        deferred.resolveWith(null, [
                            {
                                Servers: servers,
                                State: MediaBrowser.ConnectionState.ServerSelection,
                                ConnectUser: self.connectUser()
                            }
                        ]);

                    });
                } else {

                    deferred.resolveWith(null, [
                    {
                        Servers: servers,
                        State: (!servers.length && !self.connectUser()) ? MediaBrowser.ConnectionState.Unavailable : MediaBrowser.ConnectionState.ServerSelection,
                        ConnectUser: self.connectUser()
                    }]);
                }
            }

            return deferred.promise();
        };

        self.connectToServer = function (server) {

            var deferred = $.Deferred();

            var systemInfo = null;
            var connectionMode = MediaBrowser.ConnectionMode.Local;
            var credentials = credentialProvider.credentials();

            function onLocalServerTokenValidationDone() {

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

                $(this).trigger('connected', [result]);
            }

            function onExchangeTokenDone() {

                if (server.AccessToken) {
                    validateAuthentication(server, connectionMode).always(onLocalServerTokenValidationDone);
                } else {
                    onLocalServerTokenValidationDone();
                }
            }

            function onEnsureConnectUserDone() {

                if (credentials.ConnectUserId && credentials.ConnectAccessToken) {

                    addAuthenticationInfoFromConnect(server, connectionMode, credentials).always(onExchangeTokenDone);

                } else {
                    onExchangeTokenDone();
                }
            }

            function onRemoteTestDone() {

                if (systemInfo == null) {

                    resolveWithFailure(deferred);
                    return;
                }

                updateServerInfo(server, systemInfo);

                if (credentials.ConnectUserId && credentials.ConnectAccessToken) {
                    ensureConnectUser(credentials).always(onEnsureConnectUserDone);
                } else {
                    onEnsureConnectUserDone();
                }
            }

            function onLocalTestDone() {

                if (!systemInfo && server.RemoteAddress) {

                    // Try to connect to the local address
                    tryConnect(server.RemoteAddress).done(function (result) {

                        systemInfo = result;
                        connectionMode = MediaBrowser.ConnectionMode.Remote;
                        onRemoteTestDone();

                    }).fail(function () {
                        onRemoteTestDone();
                    });

                } else {
                    onRemoteTestDone();
                }
            }

            if (server.LocalAddress) {

                //onLocalTestDone();
                // Try to connect to the local address
                tryConnect(server.LocalAddress).done(function (result) {

                    systemInfo = result;
                    onLocalTestDone();

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

            var md5 = CryptoJS.MD5(password).toString();

            return $.ajax({
                type: "POST",
                url: "https://connect.mediabrowser.tv/service/user/authenticate",
                data: {
                    userName: username,
                    password: md5
                },
                dataType: "json",
                contentType: 'application/x-www-form-urlencoded; charset=UTF-8',

                error: function () {
                    // Don't show normal dashboard errors
                }


            }).done(function (result) {

                var credentials = credentialProvider.credentials();

                credentials.ConnectAccessToken = result.AccessToken;
                credentials.ConnectUserId = result.User.Id;

                credentialProvider.credentials(credentials);

                onConnectAuthenticated(result.User);
            });
        };

        self.getApiClient = function (item) {

            // Accept string + object
            if (item.ServerId) {
                item = item.ServerId;
            }

            return apiClients.filter(function (a) {

                return a.serverInfo().Id = item;

            })[0];
        };
    };

})(window, jQuery);