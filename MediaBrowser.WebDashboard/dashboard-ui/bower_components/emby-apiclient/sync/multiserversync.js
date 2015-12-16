(function (globalScope) {

    function multiServerSync(connectionManager) {

        var self = this;

        self.sync = function (options) {

            var deferred = DeferredBuilder.Deferred();

            var servers = connectionManager.getSavedServers();

            syncNext(servers, 0, options, deferred);

            return deferred.promise();
        };

        function syncNext(servers, index, options, deferred) {

            var length = servers.length;

            if (index >= length) {

                deferred.resolve();
                return;
            }

            var server = servers[index];

            Logger.log("Creating ServerSync to server: " + server.Id);

            require(['serversync'], function () {

                new MediaBrowser.ServerSync(connectionManager).sync(server, options).then(function () {

                    syncNext(servers, index + 1, options, deferred);

                }, function () {

                    syncNext(servers, index + 1, options, deferred);
                });
            });
        }
    }

    if (!globalScope.MediaBrowser) {
        globalScope.MediaBrowser = {};
    }

    globalScope.MediaBrowser.MultiServerSync = multiServerSync;

})(this);