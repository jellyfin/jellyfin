(function (globalScope) {

    function multiServerSync(connectionManager) {

        var self = this;

        self.sync = function () {

            var deferred = DeferredBuilder.Deferred();

            connectionManager.getAvailableServers().done(function (result) {
                syncNext(result, 0, deferred);
            });

            return deferred.promise();
        };

        function syncNext(servers, index, deferred) {

            var length = servers.length;

            if (index >= length) {

                deferred.resolve();
                return;
            }

            var server = servers[index];

            // get fresh info from connection manager
            server = connectionManager.getServerInfo(server.Id) || server;

            Logger.log("Creating ServerSync to server: " + server.Id);

            require(['serversync'], function () {

                new MediaBrowser.ServerSync(connectionManager).sync(server).done(function () {

                    syncNext(servers, index + 1, deferred);

                }).fail(function () {

                    syncNext(servers, index + 1, deferred);
                });
            });
        }
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.MultiServerSync = multiServerSync;

})(this);