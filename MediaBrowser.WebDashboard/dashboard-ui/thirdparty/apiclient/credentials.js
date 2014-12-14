(function (globalScope, store, JSON) {

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.CredentialProvider = function () {

        var self = this;
        var credentials;
        var key = 'servercredentials3';

        function ensure() {

            credentials = credentials || JSON.parse(store.getItem(key) || '{}');
            credentials.servers = credentials.servers || [];
        }

        function get() {

            ensure();
            return credentials;
        }

        function set(data) {
            
            if (data) {
                credentials = data;
                store.setItem(key, JSON.stringify(data));
            } else {
                self.clear();
            }
        }

        self.clear = function () {
            credentials = null;
            store.removeItem(key);
        };

        self.credentials = function (data) {

            if (data) {
                set(data);
            }

            return get();
        };

        self.addOrUpdateServer = function (list, server) {

            var existing = list.filter(function (s) {
                return s.Id == server.Id;
            })[0];

            if (existing) {
                // Merge the data
                existing.DateLastAccessed = Math.max(existing.DateLastAccessed || 0, server.DateLastAccessed || 0, new Date().getTime());

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
            }
            else {
                list.push(server);
            }
        };
    };

})(window, store, window.JSON);