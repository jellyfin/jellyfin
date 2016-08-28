define(['serversync'], function (ServerSync) {

    function syncNext(connectionManager, servers, index, options, resolve, reject) {

        var length = servers.length;

        if (index >= length) {

            resolve();
            return;
        }

        var server = servers[index];

        console.log("Creating ServerSync to server: " + server.Id);

        new ServerSync(connectionManager).sync(server, options).then(function () {

            syncNext(connectionManager, servers, index + 1, options, resolve, reject);

        }, function () {

            syncNext(connectionManager, servers, index + 1, options, resolve, reject);
        });
    }

    return function (connectionManager) {

        var self = this;

        self.sync = function (options) {

            return new Promise(function (resolve, reject) {

                var servers = connectionManager.getSavedServers();

                syncNext(connectionManager, servers, 0, options, resolve, reject);
            });
        };
    };
});