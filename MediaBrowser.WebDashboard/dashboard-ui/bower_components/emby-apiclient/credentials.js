define(['events', 'appStorage'], function (events, appStorage) {
    'use strict';

    return function (key) {

        var self = this;
        var credentials = null;
        key = key || 'servercredentials3';

        function ensure() {

            if (!credentials) {

                var json = appStorage.getItem(key) || '{}';

                console.log('credentials initialized with: ' + json);
                credentials = JSON.parse(json);
                credentials.Servers = credentials.Servers || [];
            }
        }

        function get() {

            ensure();
            return credentials;
        }

        function set(data) {

            if (data) {
                credentials = data;
                appStorage.setItem(key, JSON.stringify(data));
            } else {
                self.clear();
            }

            events.trigger(self, 'credentialsupdated');
        }

        self.clear = function () {
            credentials = null;
            appStorage.removeItem(key);
        };

        self.credentials = function (data) {

            if (data) {
                set(data);
            }

            return get();
        };

        self.addOrUpdateServer = function (list, server) {

            if (!server.Id) {
                throw new Error('Server.Id cannot be null or empty');
            }

            var existing = list.filter(function (s) {
                return s.Id === server.Id;
            })[0];

            if (existing) {

                // Merge the data
                existing.DateLastAccessed = Math.max(existing.DateLastAccessed || 0, server.DateLastAccessed || 0);

                existing.UserLinkType = server.UserLinkType;

                if (server.AccessToken) {
                    existing.AccessToken = server.AccessToken;
                    existing.UserId = server.UserId;
                }
                if (server.ExchangeToken) {
                    existing.ExchangeToken = server.ExchangeToken;
                }
                if (server.RemoteAddress) {
                    existing.RemoteAddress = server.RemoteAddress;
                }
                if (server.ManualAddress) {
                    existing.ManualAddress = server.ManualAddress;
                }
                if (server.LocalAddress) {
                    existing.LocalAddress = server.LocalAddress;
                }
                if (server.Name) {
                    existing.Name = server.Name;
                }
                if (server.WakeOnLanInfos && server.WakeOnLanInfos.length) {
                    existing.WakeOnLanInfos = server.WakeOnLanInfos;
                }
                if (server.LastConnectionMode != null) {
                    existing.LastConnectionMode = server.LastConnectionMode;
                }
                if (server.ConnectServerId) {
                    existing.ConnectServerId = server.ConnectServerId;
                }

                return existing;
            }
            else {
                list.push(server);
                return server;
            }
        };

        self.addOrUpdateUser = function (server, user) {

            server.Users = server.Users || [];

            var existing = server.Users.filter(function (s) {
                return s.Id === user.Id;
            })[0];

            if (existing) {

                // Merge the data
                existing.IsSignedInOffline = true;
            }
            else {
                server.Users.push(user);
            }
        };
    };
});