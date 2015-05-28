(function (globalScope, JSON) {

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.CredentialProvider = function () {

        var self = this;
        var credentials;
        var key = 'servercredentials4';

        function ensure() {

            credentials = credentials || JSON.parse(appStorage.getItem(key) || '{}');
            credentials.Servers = credentials.Servers || [];
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

            Events.trigger(self, 'credentialsupdated');
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
                return s.Id == server.Id;
            })[0];

            if (existing) {

                // Merge the data
                existing.DateLastAccessed = Math.max(existing.DateLastAccessed || 0, server.DateLastAccessed || 0);

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
                existing.DateLastLocalConnection = Math.max(existing.DateLastLocalConnection || 0, server.DateLastLocalConnection || 0);

                return existing;
            }
            else {
                list.push(server);
                return server;
            }
        };
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

})(window, window.JSON);