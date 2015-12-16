(function (globalScope) {

    function multiServerSync(connectionManager) {

        var self = this;

        self.sync = function (options) {

            return new Promise(function (resolve, reject) {

                var servers = connectionManager.getSavedServers();

                syncNext(servers, 0, options, resolve, reject);
            });
        };

        function syncNext(servers, index, options, resolve, reject) {

            var length = servers.length;

            if (index >= length) {

                resolve();
                return;
            }

            var server = servers[index];

            console.log("Creating ServerSync to server: " + server.Id);

            require(['serversync'], function () {

                new MediaBrowser.ServerSync(connectionManager).sync(server, options).then(function () {

                    syncNext(servers, index + 1, options, resolve, reject);

                }, function () {

                    syncNext(servers, index + 1, options, resolve, reject);
                });
            });
        }
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.MultiServerSync = multiServerSync;

})(this);